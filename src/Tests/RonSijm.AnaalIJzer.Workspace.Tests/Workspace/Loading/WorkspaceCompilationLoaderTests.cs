using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Workspace.Loading;

namespace RonSijm.AnaalIJzer.Workspace.Tests.Workspace.Loading;

public sealed class WorkspaceCompilationLoaderTests
{
    [Fact]
    public void IsReportableWorkspaceFailure_IgnoresWarnings()
    {
        var diagnostic = new WorkspaceDiagnostic(WorkspaceDiagnosticKind.Warning, "Found project reference without a matching metadata reference.");

        var result = WorkspaceCompilationLoader.IsReportableWorkspaceFailure(diagnostic);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsReportableWorkspaceFailure_ReportsFailures()
    {
        var diagnostic = new WorkspaceDiagnostic(WorkspaceDiagnosticKind.Failure, "MSBuild could not load the project.");

        var result = WorkspaceCompilationLoader.IsReportableWorkspaceFailure(diagnostic);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsReportableWorkspaceFailure_IgnoresKnownNuGetAuditFailures()
    {
        var diagnostic = new WorkspaceDiagnostic(WorkspaceDiagnosticKind.Failure, "Audit source 'nuget.org' did not provide any vulnerability data.");

        var result = WorkspaceCompilationLoader.IsReportableWorkspaceFailure(diagnostic);

        result.Should().BeFalse();
    }
}