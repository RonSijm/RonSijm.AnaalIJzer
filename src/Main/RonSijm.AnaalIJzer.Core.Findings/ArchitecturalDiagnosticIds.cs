namespace RonSijm.AnaalIJzer.Core.Findings;

public static class ArchitecturalDiagnosticIds
{
	public const string IllegalLevelDependency = "ARCH001";
	public const string UnrecognizedDependency = "ARCH002";
	public const string ForbiddenDependency = "ARCH003";
	public const string WrongDirectionDependency = "ARCH004";
	public const string SameLayerDependency = "ARCH005";
	public const string InvalidConfiguration = "ARCH006";
	public const string CyclicDependencyGraph = "ARCH007";
	public const string NameRuleViolation = "ARCH008";
	public const string ApiSurfaceLeakage = "ARCH009";
	public const string ProjectReferenceViolation = "ARCH010";
	public const string PackageReferenceViolation = "ARCH011";
	public const string VisibilityPolicyViolation = "ARCH012";
	public const string ContractPurityViolation = "ARCH013";
	public const string ForbiddenTransitiveExposure = "ARCH014";
	public const string SourceLocationViolation = "ARCH015";
	public const string BoundaryEntryPointViolation = "ARCH016";
	public const string ExceptionReview = "ARCH017";
	public const string ObservedDependencyCycle = "ARCH018";
	public const string InheritancePolicyViolation = "ARCH019";
}
