namespace RonSijm.AnaalIJzer.Core.Findings;

public static class ArchitectureFindingCodes
{
    public const string Configuration = ArchitecturalDiagnosticIds.ConfigurationInvalid;
    public const string ConfiguredCycle = ArchitecturalDiagnosticIds.ConfigurationCycle;
    public const string UnclassifiedType = ArchitecturalDiagnosticIds.TypeRequiredMissing;
    public const string UnmatchedMatcher = ArchitecturalDiagnosticIds.ConfigurationRequiredMissing;
    public const string AmbiguousLayerMatch = ArchitecturalDiagnosticIds.TypeShapeMismatch;
    public const string UnusedAllowedEdge = ArchitecturalDiagnosticIds.DependencyReviewLifecycle;
    public const string ObservedDependencyCycle = ArchitecturalDiagnosticIds.DependencyCycle;
    public const string SolutionTopologyReferenceViolation = ArchitecturalDiagnosticIds.SolutionReferenceNotAllowed;
    public const string SolutionTopologyCycle = ArchitecturalDiagnosticIds.SolutionCycle;
    public const string AssemblyReferencePolicyViolation = ArchitecturalDiagnosticIds.AssemblyReferenceNotAllowed;
    public const string OperationContractOwnerMissing = ArchitecturalDiagnosticIds.OperationContractRequiredMissing;
    public const string OperationContractOwnerAmbiguous = ArchitecturalDiagnosticIds.OperationContractShapeMismatch;
}