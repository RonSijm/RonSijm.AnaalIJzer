using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture.SolutionTopology;

public readonly struct SolutionTopologyCycle(
    ImmutableArray<string> modules,
    ImmutableArray<SolutionModuleReferenceRule> rules)
{
    public ImmutableArray<string> Modules { get; } = modules;

    public ImmutableArray<SolutionModuleReferenceRule> Rules { get; } = rules;

    public string GetDisplayPath()
    {
        var result = Modules.IsDefaultOrEmpty ? string.Empty : string.Join(" -> ", Modules) + " -> " + Modules[0];

        return result;
    }
}