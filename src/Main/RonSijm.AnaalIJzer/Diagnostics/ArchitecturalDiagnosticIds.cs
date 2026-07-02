namespace RonSijm.AnaalIJzer.Diagnostics;

public static class ArchitecturalDiagnosticIds
{
    public const string IllegalLevelDependency = "ARCH001";
    public const string UnrecognizedDependency = "ARCH002";
    public const string ForbiddenDependency = "ARCH003";
    public const string WrongDirectionDependency = "ARCH004";
    public const string SameLayerDependency = "ARCH005";
    public const string InvalidConfiguration = "ARCH006";
    public const string CyclicDependencyGraph = "ARCH007";
}
