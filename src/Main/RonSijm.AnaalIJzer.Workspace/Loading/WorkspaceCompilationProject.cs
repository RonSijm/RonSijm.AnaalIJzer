using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Workspace.Loading;

public sealed class WorkspaceCompilationProject(string projectPath, string projectName, string? assemblyName, string? targetFramework, Compilation? compilation, IReadOnlyList<Diagnostic> compilerDiagnostics, string? failure)
{
    public string ProjectPath { get; } = projectPath;
    public string ProjectName { get; } = projectName;
    public string? AssemblyName { get; } = assemblyName;
    public string? TargetFramework { get; } = targetFramework;
    public Compilation? Compilation { get; } = compilation;
    public IReadOnlyList<Diagnostic> CompilerDiagnostics { get; } = compilerDiagnostics;
    public string? Failure { get; } = failure;
    public bool IsLoaded => Compilation is not null && string.IsNullOrWhiteSpace(Failure);
}