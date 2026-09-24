using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture.SolutionTopology;

public readonly struct SolutionTopologyConfig(
    ImmutableArray<SolutionModule> modules,
    ImmutableArray<SolutionModuleReferenceRule> rules,
    bool requireRecognizedProjects,
    bool enforceAcyclic)
{
    public static readonly SolutionTopologyConfig Empty = new(
        ImmutableArray<SolutionModule>.Empty,
        ImmutableArray<SolutionModuleReferenceRule>.Empty,
        false,
        false);

    public ImmutableArray<SolutionModule> Modules { get; } = modules;

    public ImmutableArray<SolutionModuleReferenceRule> Rules { get; } = rules;

    public bool RequireRecognizedProjects { get; } = requireRecognizedProjects;

    public bool EnforceAcyclic { get; } = enforceAcyclic;

    public bool HasRules
    {
        get
        {
            var result = !Modules.IsDefaultOrEmpty || !Rules.IsDefaultOrEmpty;

            return result;
        }
    }
}