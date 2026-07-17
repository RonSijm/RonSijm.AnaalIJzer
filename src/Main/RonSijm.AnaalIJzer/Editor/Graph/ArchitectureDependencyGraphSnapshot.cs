using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Configuration;

namespace RonSijm.AnaalIJzer.Graph;

public sealed class ArchitectureDependencyGraphSnapshot(
    bool hasConfiguration,
    bool hasConfigurationIssues,
    ImmutableArray<ArchitectureDependencyGraphLayer> layers,
    ImmutableArray<ArchitectureDependencyGraphRule> rules,
    ImmutableArray<string> activeLayerPaths,
    ImmutableArray<string> configurationIssueMessages,
    ArchitectureConfigurationSource? configurationSource = null,
    ArchitectureDependencyGraphEvidence? evidence = null)
{
    public bool HasConfiguration { get; } = hasConfiguration;

    public bool HasConfigurationIssues { get; } = hasConfigurationIssues;

    public ImmutableArray<ArchitectureDependencyGraphLayer> Layers { get; } = layers;

    public ImmutableArray<ArchitectureDependencyGraphRule> Rules { get; } = rules;

    public ImmutableArray<string> ActiveLayerPaths { get; } = activeLayerPaths;

    public ImmutableArray<string> ConfigurationIssueMessages { get; } = configurationIssueMessages;

    public ArchitectureConfigurationSource ConfigurationSource { get; } = configurationSource ?? ArchitectureConfigurationSource.None;

    public ArchitectureDependencyGraphEvidence Evidence { get; } = evidence ?? ArchitectureDependencyGraphEvidence.Empty;

    public static ArchitectureDependencyGraphSnapshot Empty { get; } = new(false, false, ImmutableArray<ArchitectureDependencyGraphLayer>.Empty, ImmutableArray<ArchitectureDependencyGraphRule>.Empty, ImmutableArray<string>.Empty, ImmutableArray<string>.Empty);
}
