using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Diagnostics;
using FSharp.Compiler.EditorServices;
using FSharp.Compiler.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using Microsoft.IO;
using MirrorSharp.FSharp.Advanced;
using MirrorSharp.Internal;
using MirrorSharp.Internal.Abstraction;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using range = FSharp.Compiler.Text.Range;
using SourceText = FSharp.Compiler.Text.SourceText;

namespace MirrorSharp.FSharp.Internal;

internal class FSharpSession : ILanguageSessionInternal, IFSharpSession {
    private static readonly Task<CompletionDescription?> NoCompletionDescriptionTask = Task.FromResult<CompletionDescription?>(null);

    private string _sourceText;
    private FSharpVirtualFile _sourceFile;
    private MemoryStream? _sourceStream;
    private LineColumnMap? _lastLineMap;
    private FSharpParseAndCheckResults? _lastParseAndCheck;
    private FSharpProjectOptions _projectOptions;
    private MemoryStream? _outputStream;
    private FSharpVirtualFile? _outputFile;
    private string[]? _compilerArgs;

    private readonly RecyclableMemoryStreamManager _memoryStreamManager;

    static FSharpSession() {
        FileSystemAutoOpens.FileSystem = CustomFileSystem.Instance;
    }

    public FSharpSession(
        string text, MirrorSharpFSharpOptions options,
        RecyclableMemoryStreamManager memoryStreamManager
    ) {
        _sourceText = text;
        _sourceFile = CustomFileSystem.Instance.RegisterVirtualFile(
            static s => s.EnsureTextStream(), this, fileName: "_.fs"
        );
        _sourceFile.LastWriteTime = DateTime.Now;
        _memoryStreamManager = memoryStreamManager;

        Checker = FSharpChecker.Create(
            null,
            keepAssemblyContents: true,
            keepAllBackgroundResolutions: true,
            legacyReferenceResolver: null,
            tryGetMetadataSnapshot: null,
            suggestNamesForErrors: true,
            keepAllBackgroundSymbolUses: false,
            enableBackgroundItemKeyStoreAndSemanticClassification: false,
            // allows for using signature files to speed up compilation, but mutually exclusive with `keepAssemblyContents`
            enablePartialTypeChecking: false,
            parallelReferenceResolution: false,
            captureIdentifiersWhenParsing: false,
            documentSource: FSharpOption<DocumentSource>.Some(DocumentSource.FileSystem),
            useSyntaxTreeCache: false,
            useTransparentCompiler: false
        );
        // Checker.ImplicitlyStartBackgroundWork = false;
        AssemblyReferencePaths = options.AssemblyReferencePaths;
        AssemblyReferencePathsAsFSharpList = ToFSharpList(options.AssemblyReferencePaths);
        _projectOptions = new FSharpProjectOptions(
            "_",
            projectId: null,
            sourceFiles: new[] { _sourceFile.Path },
            otherOptions: ConvertToOtherOptionsSlow(options),
            referencedProjects: Array.Empty<FSharpReferencedProject>(),
            isIncompleteTypeCheckEnvironment: true,
            useScriptResolutionRules: false,
            loadTime: DateTime.Now,
            unresolvedReferences: null,
            originalLoadReferences: FSharpList<Tuple<range, string, string>>.Empty,
            stamp: null
        );
    }

    private MemoryStream EnsureTextStream() {
        if (_sourceStream == null) {
            var byteCount = Encoding.UTF8.GetByteCount(_sourceText);
            _sourceStream = _memoryStreamManager.GetStream("FSharpSession.SourceFile", byteCount, asContiguousBuffer: true);
            Encoding.UTF8.GetBytes(_sourceText, 0, _sourceText.Length, _sourceStream.GetBuffer(), 0);
            _sourceStream.SetLength(byteCount);
        }

        return _sourceStream;
    }

    private FSharpList<string> ToFSharpList(ImmutableArray<string> assemblyReferencePaths) {
        var list = FSharpList<string>.Empty;
        for (var i = assemblyReferencePaths.Length - 1; i >= 0; i--) {
            list = FSharpList<string>.Cons(assemblyReferencePaths[i], list);
        }
        return list;
    }

    public FSharpChecker Checker { get; }
    public FSharpProjectOptions ProjectOptions {
        get => _projectOptions;
        set {
            if (value == _projectOptions)
                return;

            _projectOptions = Argument.NotNull(nameof(value), value);
            _lastParseAndCheck = null;
            _compilerArgs = null;
        }
    }

    public ImmutableArray<string> AssemblyReferencePaths { get; }
    public FSharpList<string> AssemblyReferencePathsAsFSharpList { get; }

    private string[] ConvertToOtherOptionsSlow(MirrorSharpFSharpOptions options) {
        var results = new List<string> {
            "--noframework",
            // There are some challenges with supporting a win32manifest,
            // so it is disabled for now
            "--nowin32manifest",
            // There are some challenges with supporting PDB symbols with
            // VirtualFileSystem, so it is always disabled for now.
            "--debug-"
        };
        if (options.Optimize != null)
            results.Add("--optimize" + (options.Optimize.Value ? "+" : "-"));
        if (options.Target != null)
            results.Add("--target:" + options.Target);
        if (options.LangVersion != null)
            results.Add("--langversion:" + options.LangVersion);
        if (options.TargetProfile != null)
            results.Add("--targetprofile:" + options.TargetProfile);

        foreach (var path in options.AssemblyReferencePaths) {
            // ReSharper disable once HeapView.ObjectAllocation (Not worth fixing for now)
            results.Add("-r:" + path);
        }
        return results.ToArray();
    }

    public async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(CancellationToken cancellationToken) {
        var result = await ParseAndCheckAsync(cancellationToken).ConfigureAwait(false);
        var success = result.CheckAnswer as FSharpCheckFileAnswer.Succeeded;
        var diagnosticCount = result.ParseResults.Diagnostics.Length + (success?.Item.Diagnostics.Length ?? 0);
        if (diagnosticCount == 0)
            return ImmutableArray<Diagnostic>.Empty;

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>(diagnosticCount);
        ConvertAndAddTo(diagnostics, result.ParseResults.Diagnostics);

        if (success != null)
            ConvertAndAddTo(diagnostics, success.Item.Diagnostics);

        return diagnostics.MoveToImmutable();
    }

    public async ValueTask<FSharpParseAndCheckResults> ParseAndCheckAsync(CancellationToken cancellationToken) {
        if (_lastParseAndCheck != null)
            return _lastParseAndCheck;
        var sourceText = SourceText.ofString(_sourceText);
        var tuple = await FSharpAsync.StartAsTask(
            Checker.ParseAndCheckFileInProject(_sourceFile.Path, 0, sourceText, ProjectOptions, Microsoft.FSharp.Core.FSharpOption<string>.None), null, cancellationToken
        ).ConfigureAwait(false);

        _lastParseAndCheck = new FSharpParseAndCheckResults(tuple.Item1, tuple.Item2);
        return _lastParseAndCheck;
    }

    public FSharpParseFileResults? GetLastParseResults() {
        return _lastParseAndCheck?.ParseResults;
    }

    public FSharpCheckFileAnswer? GetLastCheckAnswer() {
        return _lastParseAndCheck?.CheckAnswer;
    }

    public async ValueTask<Tuple<FSharpDiagnostic[], int>> CompileAsync(MemoryStream assemblyStream, CancellationToken cancellationToken) {
        Argument.NotNull(nameof(assemblyStream), assemblyStream);
        if (_outputStream != null)
            throw new InvalidOperationException("Attempted to call CompileAsync when output stream was already set.");

        var args = GetCompilerArgs();
        try {
            _outputStream = assemblyStream;
            var compiledAsync = Checker.Compile(args, userOpName: null);
            return await FSharpAsync.StartAsTask(compiledAsync, null, cancellationToken)
                .ConfigureAwait(false);
        }
        finally {
            _outputStream = null;
        }
    }

    private string[] GetCompilerArgs() {
        if (_compilerArgs != null)
            return _compilerArgs;

        var args = new string[2 + _projectOptions.OtherOptions.Length + _projectOptions.SourceFiles.Length];
        args[0] = "fsc.exe";
        args[1] = "--out:" + GetOutputFile().Path;
        for (var i = 0; i < _projectOptions.OtherOptions.Length; i++) {
            args[2 + i] = ValidateOptionForCompilerArgs(_projectOptions.OtherOptions[i]);
        }
        _projectOptions.SourceFiles.CopyTo(args, 2 + _projectOptions.OtherOptions.Length);

        _compilerArgs = args;
        return args;
    }

    private string ValidateOptionForCompilerArgs(string option) {
        Exception OptionNotSupported() => new NotSupportedException(
            $"Option {option} is not currently supported for {nameof(CompileAsync)}."
        );

        if (option.StartsWith("--debug") && option.Length > "--debug".Length && option["--debug".Length] != '-')
            throw OptionNotSupported();

        if (option.StartsWith("-g"))
            throw OptionNotSupported();

        return option;
    }

    private FSharpVirtualFile GetOutputFile() {
        if (_outputFile != null)
            return _outputFile;

        _outputFile = CustomFileSystem.Instance.RegisterVirtualFile(
            static s => s._outputStream ?? throw new InvalidOperationException(
                "Attempted to access session output stream when it was not set."
            ),
            this, fileName: "_.dll"
        );
        return _outputFile;
    }

    private void ConvertAndAddTo(ImmutableArray<Diagnostic>.Builder diagnostics, FSharpDiagnostic[] fsharpDiagnostics) {
        foreach (var fsharpDiagnostic in fsharpDiagnostics) {
            diagnostics.Add(ConvertToDiagnostic(fsharpDiagnostic));
        }
    }

    public Diagnostic ConvertToDiagnostic(FSharpDiagnostic diagnostic) {
        Argument.NotNull(nameof(diagnostic), diagnostic);

        var lineMap = GetLineMap();
        var severity = diagnostic.Severity.Tag == FSharpDiagnosticSeverity.Tags.Error ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning;

        var startOffset = lineMap.GetOffset(diagnostic.Range.StartLine, diagnostic.Range.StartColumn);
        var location = Location.Create(
            "",
            new TextSpan(
                startOffset,
                lineMap.GetOffset(diagnostic.Range.EndLine, diagnostic.Range.EndColumn) - startOffset
            ),
            new LinePositionSpan(
                new LinePosition(diagnostic.Range.StartLine, diagnostic.Range.StartColumn),
                new LinePosition(diagnostic.Range.EndLine, diagnostic.Range.EndColumn)
            )
        );

        return Diagnostic.Create(
            "FS" + diagnostic.ErrorNumber.ToString("0000"),
            "Compiler",
            diagnostic.Message,
            severity, severity,
            isEnabledByDefault: false,
            warningLevel: severity == DiagnosticSeverity.Warning ? 1 : 0,
            location: location
        );
    }

    public string GetText() {
        return _sourceText;
    }

    public void ReplaceText(string? newText, int start = 0, int? length = null) {
        if (length > 0)
            _sourceText = _sourceText.Remove(start, length.Value);
        if (newText?.Length > 0)
            _sourceText = _sourceText.Insert(start, newText);

        _lastParseAndCheck = null;
        _lastLineMap = null;
        _sourceStream?.Dispose();
        _sourceStream = null;
        _sourceFile.LastWriteTime = DateTime.Now;
    }

    public bool ShouldTriggerCompletion(int cursorPosition, CompletionTrigger trigger) {
        return (trigger.Kind == CompletionTriggerKind.Insertion && trigger.Character == '.')
            || trigger.Kind == CompletionTriggerKind.Invoke;
    }

    public async Task<CompletionList?> GetCompletionsAsync(int cursorPosition, CompletionTrigger trigger, CancellationToken cancellationToken) {
        var result = await ParseAndCheckAsync(cancellationToken);
        if (!(result.CheckAnswer is FSharpCheckFileAnswer.Succeeded success))
            return null;

        var info = GetLineMap().GetLineAndColumn(cursorPosition);
        var symbols = success.Item.GetDeclarationListSymbols(
            result.ParseResults, info.line.Number,
            _sourceText.Substring(info.line.Start, info.line.Length),
            QuickParse.GetPartialLongNameEx(_sourceText.Substring(info.line.Start, info.line.Length), info.column - 1),
            Microsoft.FSharp.Core.FSharpOption<Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, FSharpList<AssemblySymbol>>>.None
        );
        if (symbols.IsEmpty)
            return null;

        return CompletionList.Create(
            new TextSpan(cursorPosition, 0),
            ConvertToCompletionItems(symbols)
        );
    }

    private ImmutableArray<CompletionItem> ConvertToCompletionItems(FSharpList<FSharpList<FSharpSymbolUse>> symbols) {
        var items = ImmutableArray.CreateBuilder<CompletionItem>(symbols.Length);
        foreach (var list in symbols) {
            var use = list.Head;
            items.Add(CompletionItem.Create(
                use.Symbol.DisplayName,
                tags: SymbolTags.From(use.Symbol)
            ));
        }
        return items.MoveToImmutable();
    }

    public Task<CompletionDescription?> GetCompletionDescriptionAsync(CompletionItem item, CancellationToken cancellationToken) {
        return NoCompletionDescriptionTask;
    }

    public Task<CompletionChange> GetCompletionChangeAsync(TextSpan completionSpan, CompletionItem item, CancellationToken cancellationToken) {
        return Task.FromResult(CompletionChange.Create(new TextChange(completionSpan, item.DisplayText)));
    }

    public int ConvertToOffset(int line, int column) {
        Argument.PositiveOrZero(nameof(line), line);
        Argument.PositiveOrZero(nameof(column), column);
        return GetLineMap().GetOffset(line, column);
    }

    private LineColumnMap GetLineMap() {
        if (_lastLineMap == null)
            _lastLineMap = LineColumnMap.BuildFor(_sourceText);
        return _lastLineMap;
    }

    public void Dispose() {
        _sourceFile.Dispose();
        _sourceStream?.Dispose();
        _outputFile?.Dispose();
    }
}