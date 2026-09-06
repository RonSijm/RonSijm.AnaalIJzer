namespace RonSijm.AnaalIJzer.Core.Findings;

public static class ArchitectureFindingCodes
{
	public const string Configuration = "Configuration";
	public const string ConfiguredCycle = "Configured cycle";
	public const string UnclassifiedType = "Unclassified type";
	public const string UnmatchedMatcher = "Unmatched matcher";
	public const string AmbiguousLayerMatch = "Ambiguous layer match";
	public const string UnusedAllowedEdge = "Unused allowed edge";
	public const string ObservedDependencyCycle = "Observed dependency cycle";
	public const string SolutionTopologyReferenceViolation = "TOPO001";
	public const string SolutionTopologyCycle = "TOPO002";
	public const string AssemblyReferencePolicyViolation = "Assembly reference policy";
	public const string OperationContractOwnerMissing = "Operation contract owner missing";
	public const string OperationContractOwnerAmbiguous = "Operation contract owner ambiguous";
}
