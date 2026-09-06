namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture.SolutionTopology;

public readonly struct SolutionTopologyReferenceViolation(
	SolutionProjectReference reference,
	string? sourceModule,
	string? targetModule,
	string violationReason,
	SolutionModuleReferenceRule? matchedRule)
{
	public SolutionProjectReference Reference { get; } = reference;

	public string? SourceModule { get; } = sourceModule;

	public string? TargetModule { get; } = targetModule;

	public string ViolationReason { get; } = violationReason;

	public SolutionModuleReferenceRule? MatchedRule { get; } = matchedRule;
}
