namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture.SolutionTopology;

public readonly struct SolutionTopologyReferenceEvaluation(
	bool isAllowed,
	string violationReason,
	SolutionModuleReferenceRule? matchedRule,
	string? sourceModule,
	string? targetModule)
{
	public bool IsAllowed { get; } = isAllowed;

	public string ViolationReason { get; } = violationReason;

	public SolutionModuleReferenceRule? MatchedRule { get; } = matchedRule;

	public string? SourceModule { get; } = sourceModule;

	public string? TargetModule { get; } = targetModule;
}
