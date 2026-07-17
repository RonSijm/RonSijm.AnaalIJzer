using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Graph;
using RonSijm.AnaalIJzer.Indicators;

namespace RonSijm.AnaalIJzer.Snapshots;

public sealed class ArchitectureEditorSnapshot(
    bool hasConfiguration,
    bool hasConfigurationIssues,
    ImmutableArray<ArchitectureLayerIndicator> layerIndicators,
    ImmutableArray<ArchitectureDependencySiteIndicator> siteIndicators,
    ImmutableArray<string> configurationIssueMessages,
    ArchitectureDependencyGraphSnapshot? graphSnapshot = null,
    ImmutableArray<ArchitectureLayerIndicator> unclassifiedTypeIndicators = default)
{
    public bool HasConfiguration { get; } = hasConfiguration;

    public bool HasConfigurationIssues { get; } = hasConfigurationIssues;

    public ImmutableArray<ArchitectureLayerIndicator> LayerIndicators { get; } = layerIndicators;

    public ImmutableArray<ArchitectureDependencySiteIndicator> SiteIndicators { get; } = siteIndicators;

    public ImmutableArray<ArchitectureLayerIndicator> UnclassifiedTypeIndicators { get; } = unclassifiedTypeIndicators.IsDefault ? ImmutableArray<ArchitectureLayerIndicator>.Empty : unclassifiedTypeIndicators;

    public ImmutableArray<string> ConfigurationIssueMessages { get; } = configurationIssueMessages;

    public ArchitectureDependencyGraphSnapshot GraphSnapshot { get; } = graphSnapshot ?? ArchitectureDependencyGraphSnapshot.Empty;

    public static ArchitectureEditorSnapshot Empty { get; } = new(
		false,
		false,
		ImmutableArray<ArchitectureLayerIndicator>.Empty,
		ImmutableArray<ArchitectureDependencySiteIndicator>.Empty,
		ImmutableArray<string>.Empty);
}
