using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture;

public readonly struct ProjectArchitectureConfig(
    ImmutableArray<ProjectGroup> projectGroups,
    ImmutableArray<ProjectReferenceRule> rules,
    ImmutableArray<PackagePolicy> packagePolicies,
    ImmutableArray<AssemblyReferencePolicy> assemblyReferencePolicies,
    bool requireRecognizedProjects)
{
    public static readonly ProjectArchitectureConfig Empty = new(
        ImmutableArray<ProjectGroup>.Empty,
        ImmutableArray<ProjectReferenceRule>.Empty,
        ImmutableArray<PackagePolicy>.Empty,
        ImmutableArray<AssemblyReferencePolicy>.Empty,
        false);

    public ImmutableArray<ProjectGroup> ProjectGroups { get; } = projectGroups;

    public ImmutableArray<ProjectReferenceRule> Rules { get; } = rules;

    public ImmutableArray<PackagePolicy> PackagePolicies { get; } = packagePolicies;

    public ImmutableArray<AssemblyReferencePolicy> AssemblyReferencePolicies { get; } = assemblyReferencePolicies;

    public bool RequireRecognizedProjects { get; } = requireRecognizedProjects;

    public bool HasRules
    {
        get
        {
            var result = !ProjectGroups.IsDefaultOrEmpty || !Rules.IsDefaultOrEmpty || !PackagePolicies.IsDefaultOrEmpty || !AssemblyReferencePolicies.IsDefaultOrEmpty;

            return result;
        }
    }
}