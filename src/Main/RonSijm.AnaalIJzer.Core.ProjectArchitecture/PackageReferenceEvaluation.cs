using RonSijm.AnaalIJzer.Core.Matchers.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture;

public readonly struct PackageReferenceEvaluation(
	bool isAllowed,
	string violationReason,
	PackagePolicy? matchedPolicy,
	PackageMatcher? matchedMatcher,
	string? sourceProjectGroup)
{
	public bool IsAllowed { get; } = isAllowed;

	public string ViolationReason { get; } = violationReason;

	public PackagePolicy? MatchedPolicy { get; } = matchedPolicy;

	public PackageMatcher? MatchedMatcher { get; } = matchedMatcher;

	public string? SourceProjectGroup { get; } = sourceProjectGroup;
}
