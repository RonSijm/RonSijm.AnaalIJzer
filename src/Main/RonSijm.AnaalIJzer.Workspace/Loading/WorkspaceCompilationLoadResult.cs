namespace RonSijm.AnaalIJzer.Workspace.Loading;

public sealed class WorkspaceCompilationLoadResult(string inputPath, IReadOnlyList<WorkspaceCompilationProject> projects, IReadOnlyList<string> workspaceFailures)
{
    public string InputPath { get; } = inputPath;
    public IReadOnlyList<WorkspaceCompilationProject> Projects { get; } = projects;
    public IReadOnlyList<string> WorkspaceFailures { get; } = workspaceFailures;
}