using System;
#if !NETCOREAPP
using System.Buffers;
#endif
using System.Composition.Hosting;
using System.Reflection;
#if NETCOREAPP
using System.Runtime.Loader;
#endif
using System.Threading;
using MirrorSharp.Internal.Roslyn.Internals;

namespace MirrorSharp.Internal.Roslyn {
    internal class RoslynInternals {        
        private static readonly Lazy<Assembly> _internalAssembly = new(
            LoadInternalsAssemblyWithDependenciesSlowUncached, LazyThreadSafetyMode.PublicationOnly
        );

        public RoslynInternals(
            ICodeActionInternals codeAction,
            IWorkspaceAnalyzerOptionsInternals workspaceAnalyzerOptions,
            ISignatureHelpProviderWrapperResolver singatureHelpProviderResolver
        ) {
            CodeAction = codeAction;
            WorkspaceAnalyzerOptions = workspaceAnalyzerOptions;
            SingatureHelpProviderResolver = singatureHelpProviderResolver;
        }

        public ICodeActionInternals CodeAction { get; }
        public IWorkspaceAnalyzerOptionsInternals WorkspaceAnalyzerOptions { get; }
        public ISignatureHelpProviderWrapperResolver SingatureHelpProviderResolver { get; }

        public static RoslynInternals Get(CompositionHost compositionHost) {
            Argument.NotNull(nameof(compositionHost), compositionHost);
            return new RoslynInternals(
                compositionHost.GetExport<ICodeActionInternals>(),
                compositionHost.GetExport<IWorkspaceAnalyzerOptionsInternals>(),
                compositionHost.GetExport<ISignatureHelpProviderWrapperResolver>()
            );
        }

        public static Assembly GetInternalsAssemblySlow() {
            return _internalAssembly.Value;
        }

        private static Assembly LoadInternalsAssemblyWithDependenciesSlowUncached() {
            var roslynVersion = RoslynAssemblies.MicrosoftCodeAnalysis.GetName().Version!;
            var assembly = LoadInternalsAssemblySlow(roslynVersion);
            // CI build. TODO: SharpLab only?
            if (roslynVersion.Major == 42 && roslynVersion.Minor == 42) {
                // Try previous versions, in case CI is not on newest yet
                var fallback = GetAssemblyOrNullIfTypesFailToLoad(assembly)
                            ?? GetAssemblyOrNullIfTypesFailToLoad(LoadInternalsAssemblySlow(new Version(5, 0)))
                            ?? GetAssemblyOrNullIfTypesFailToLoad(LoadInternalsAssemblySlow(new Version(4, 12)))
                            ?? GetAssemblyOrNullIfTypesFailToLoad(LoadInternalsAssemblySlow(new Version(4, 11)))
                            ?? GetAssemblyOrNullIfTypesFailToLoad(LoadInternalsAssemblySlow(new Version(4, 10)))
                            ?? GetAssemblyOrNullIfTypesFailToLoad(LoadInternalsAssemblySlow(new Version(4, 9)))
                            ?? GetAssemblyOrNullIfTypesFailToLoad(LoadInternalsAssemblySlow(new Version(4, 8)))
                            ?? GetAssemblyOrNullIfTypesFailToLoad(LoadInternalsAssemblySlow(new Version(4, 7)))
                            ?? GetAssemblyOrNullIfTypesFailToLoad(LoadInternalsAssemblySlow(new Version(4, 6)))
                            ?? GetAssemblyOrNullIfTypesFailToLoad(LoadInternalsAssemblySlow(new Version(4, 5)))
                            ?? GetAssemblyOrNullIfTypesFailToLoad(LoadInternalsAssemblySlow(new Version(4, 4)))
                            ?? GetAssemblyOrNullIfTypesFailToLoad(LoadInternalsAssemblySlow(new Version(4, 3)))
                            ?? GetAssemblyOrNullIfTypesFailToLoad(LoadInternalsAssemblySlow(new Version(4, 2)));
                assembly = fallback ?? assembly;
            }

            PreloadInternalsAssemblyDependenciesSlow(assembly);
            EnsureInternalsTypesCanLoad(assembly);
            return assembly;
        }

        private static Assembly? GetAssemblyOrNullIfTypesFailToLoad(Assembly assembly) {
            try {
                _ = assembly.DefinedTypes;
            }
            catch (ReflectionTypeLoadException) {
                return null;
            }

            return assembly;
        }

        private static void EnsureInternalsTypesCanLoad(Assembly assembly) {
            try {
                _ = assembly.DefinedTypes;
            }
            catch (ReflectionTypeLoadException ex) {
                throw new Exception(
                    $"Failed to load MirrorSharp Roslyn internals.{Environment.NewLine}" +
                    $"  * Microsoft.CodeAnalysis {RoslynAssemblies.MicrosoftCodeAnalysis.GetName().Version}.{Environment.NewLine}" +
                    $"  * {assembly.GetName().Name}",
                    ex
                );
            }
        }

        private static Assembly LoadInternalsAssemblySlow(Version roslynVersion) {
            var assemblyName = roslynVersion switch {
                { Major: > 4 } or { Major: 5 } => "MirrorSharp.Internal.Roslyn50.dll",
                { Major: 4, Minor: 14 } => "MirrorSharp.Internal.Roslyn412.dll",
                { Major: 4, Minor: 12 } => "MirrorSharp.Internal.Roslyn412.dll",
                { Major: 4, Minor: 11 } => "MirrorSharp.Internal.Roslyn411.dll",
                { Major: 4, Minor: 10 } => "MirrorSharp.Internal.Roslyn410.dll",
                { Major: 4, Minor: 9 } => "MirrorSharp.Internal.Roslyn49.dll",
                { Major: 4, Minor: 8 } => "MirrorSharp.Internal.Roslyn48.dll",
                { Major: 4, Minor: 7 } => "MirrorSharp.Internal.Roslyn47.dll",
                { Major: 4, Minor: 6 } => "MirrorSharp.Internal.Roslyn46.dll",
                { Major: 4, Minor: 5 } => "MirrorSharp.Internal.Roslyn45.dll",
                { Major: 4, Minor: 4 } => "MirrorSharp.Internal.Roslyn44.dll",
                { Major: 4, Minor: 3 } => "MirrorSharp.Internal.Roslyn43.dll",
                { Major: 4, Minor: 2 } => "MirrorSharp.Internal.Roslyn42.dll",
                { Major: 4, Minor: 1 } => "MirrorSharp.Internal.Roslyn41.dll",
                { Major: 4 } => "MirrorSharp.Internal.Roslyn36.dll",
                { Major: 3, Minor: >= 6 } => "MirrorSharp.Internal.Roslyn36.dll",
                { Major: 3, Minor: >= 3 } => "MirrorSharp.Internal.Roslyn33.dll",
                _ => throw new NotSupportedException()
            };

            using var assemblyStream = typeof(RoslynInternals).Assembly.GetManifestResourceStream(assemblyName)!;
            #if NETCOREAPP
            return AssemblyLoadContext.Default.LoadFromStream(assemblyStream);
            #else
            byte[]? assemblyBytes = null;
            try {
                assemblyBytes = ArrayPool<byte>.Shared.Rent((int)assemblyStream.Length);
                assemblyStream.Read(assemblyBytes, 0, assemblyBytes.Length);
                return Assembly.Load(assemblyBytes);
            }
            finally {
                if (assemblyBytes != null)
                    ArrayPool<byte>.Shared.Return(assemblyBytes);
            }
            #endif
        }

        private static void PreloadInternalsAssemblyDependenciesSlow(Assembly assembly) {
            foreach (var reference in assembly.GetReferencedAssemblies()) {
                Assembly.Load(reference);
            }
        }
    }
}
