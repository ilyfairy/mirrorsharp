using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace MirrorSharp.Internal.Roslyn411;

[Shared]
[Export(typeof(IDiagnosticAnalyzerService))]
internal class MirrorSharpDiagnosticAnalyzerService : IDiagnosticAnalyzerService {
    DiagnosticAnalyzerInfoCache IDiagnosticAnalyzerService.AnalyzerInfoCache { get; } = new();
    IGlobalOptionService IDiagnosticAnalyzerService.GlobalOptions => throw new NotSupportedException();
    Task IDiagnosticAnalyzerService.ForceAnalyzeProjectAsync(Project project, CancellationToken cancellationToken) => throw new NotSupportedException();
    Task<ImmutableArray<DiagnosticData>> IDiagnosticAnalyzerService.GetCachedDiagnosticsAsync(Workspace workspace, ProjectId? projectId, DocumentId? documentId, bool includeSuppressedDiagnostics, bool includeLocalDocumentDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken) => throw new NotSupportedException();
    Task<ImmutableArray<DiagnosticData>> IDiagnosticAnalyzerService.GetDiagnosticsForIdsAsync(Solution solution, ProjectId? projectId, DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, Func<Project, DocumentId?, IReadOnlyList<DocumentId>>? getDocumentIds, bool includeSuppressedDiagnostics, bool includeLocalDocumentDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken) => throw new NotSupportedException();
    Task<ImmutableArray<DiagnosticData>> IDiagnosticAnalyzerService.GetDiagnosticsForSpanAsync(TextDocument document, TextSpan? range, Func<string, bool>? shouldIncludeDiagnostic, bool includeCompilerDiagnostics, bool includeSuppressedDiagnostics, ICodeActionRequestPriorityProvider priorityProvider, DiagnosticKind diagnosticKind, bool isExplicit, CancellationToken cancellationToken) => throw new NotSupportedException();
    Task<ImmutableArray<DiagnosticData>> IDiagnosticAnalyzerService.GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId? projectId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken) => throw new NotSupportedException();
    void IDiagnosticAnalyzerService.RequestDiagnosticRefresh() => throw new NotImplementedException();
}
