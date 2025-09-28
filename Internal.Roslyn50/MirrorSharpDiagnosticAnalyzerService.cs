using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace MirrorSharp.Internal.Roslyn414;

[Shared]
[Export(typeof(IDiagnosticAnalyzerService))]
internal class MirrorSharpDiagnosticAnalyzerService : IDiagnosticAnalyzerService {
    DiagnosticAnalyzerInfoCache IDiagnosticAnalyzerService.AnalyzerInfoCache { get; } = new();

    Task<ImmutableArray<DiagnosticData>> IDiagnosticAnalyzerService.GetDiagnosticsForSpanAsync(TextDocument document, TextSpan? range, Func<string, bool>? shouldIncludeDiagnostic, ICodeActionRequestPriorityProvider priorityProvider, DiagnosticKind diagnosticKind, CancellationToken cancellationToken) => throw new NotSupportedException();
    Task<ImmutableArray<DiagnosticData>> IDiagnosticAnalyzerService.ForceAnalyzeProjectAsync(Project project, CancellationToken cancellationToken) => throw new NotSupportedException();
    Task<ImmutableArray<DiagnosticData>> IDiagnosticAnalyzerService.GetDiagnosticsForIdsAsync(Project project, DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeLocalDocumentDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken) => throw new NotSupportedException();
    Task<ImmutableArray<DiagnosticData>> IDiagnosticAnalyzerService.GetProjectDiagnosticsForIdsAsync(Project project, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken) => throw new NotSupportedException();
    void IDiagnosticAnalyzerService.RequestDiagnosticRefresh() => throw new NotImplementedException();
}
