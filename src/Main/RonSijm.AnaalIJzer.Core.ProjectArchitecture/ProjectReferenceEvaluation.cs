namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture;

public readonly struct ProjectReferenceEvaluation(
	bool isAllowed,
	string violationReason,
	ProjectReferenceRule? matchedRule,
	string? sourceProjectGroup,
	string? targetProjectGroup)
{
	public bool IsAllowed { get; } = isAllowed;

	public string ViolationReason { get; } = violationReason;

	public ProjectReferenceRule? MatchedRule { get; } = matchedRule;

	public string? SourceProjectGroup { get; } = sourceProjectGroup;

	public string? TargetProjectGroup { get; } = targetProjectGroup;
}
