using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.Graphing.Model;

public sealed class ArchitectureGraphSnapshot(
    bool hasConfiguration,
    bool hasConfigurationIssues,
    ImmutableArray<ArchitectureGraphLayer> layers,
    ImmutableArray<ArchitectureGraphRule> rules,
    ImmutableArray<string> activeLayerPaths,
    ImmutableArray<string> configurationIssueMessages,
    ArchitectureConfigurationSource? configurationSource = null,
    ArchitectureGraphEvidence? evidence = null)
{
    public bool HasConfiguration { get; } = hasConfiguration;

    public bool HasConfigurationIssues { get; } = hasConfigurationIssues;

    public ImmutableArray<ArchitectureGraphLayer> Layers { get; } = layers;

    public ImmutableArray<ArchitectureGraphRule> Rules { get; } = rules;

    public ImmutableArray<string> ActiveLayerPaths { get; } = activeLayerPaths;

    public ImmutableArray<string> ConfigurationIssueMessages { get; } = configurationIssueMessages;

    public ArchitectureConfigurationSource ConfigurationSource { get; } = configurationSource ?? ArchitectureConfigurationSource.None;

    public ArchitectureGraphEvidence Evidence { get; } = evidence ?? ArchitectureGraphEvidence.Empty;

    public static ArchitectureGraphSnapshot Empty { get; } = new(false, false, ImmutableArray<ArchitectureGraphLayer>.Empty, ImmutableArray<ArchitectureGraphRule>.Empty, ImmutableArray<string>.Empty, ImmutableArray<string>.Empty);
}
