using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.ProjectArchitecture;

public readonly struct ProjectArchitectureConfig(
	ImmutableArray<ProjectGroup> projectGroups,
	ImmutableArray<ProjectReferenceRule> rules,
	ImmutableArray<PackagePolicy> packagePolicies,
	bool requireRecognizedProjects)
{
	public static readonly ProjectArchitectureConfig Empty = new(
		ImmutableArray<ProjectGroup>.Empty,
		ImmutableArray<ProjectReferenceRule>.Empty,
		ImmutableArray<PackagePolicy>.Empty,
		false);

	public ImmutableArray<ProjectGroup> ProjectGroups { get; } = projectGroups;

	public ImmutableArray<ProjectReferenceRule> Rules { get; } = rules;

	public ImmutableArray<PackagePolicy> PackagePolicies { get; } = packagePolicies;

	public bool RequireRecognizedProjects { get; } = requireRecognizedProjects;

	public bool HasRules
	{
		get
		{
			var result = !ProjectGroups.IsDefaultOrEmpty || !Rules.IsDefaultOrEmpty || !PackagePolicies.IsDefaultOrEmpty;

			return result;
		}
	}
}
