namespace RonSijm.AnaalIJzer.Core.Findings.Diagnostics;

[Flags]
public enum ArchitectureDiagnosticSurface
{
    None = 0,
    Compiler = 1,
    Workspace = 2,
    All = Compiler | Workspace
}