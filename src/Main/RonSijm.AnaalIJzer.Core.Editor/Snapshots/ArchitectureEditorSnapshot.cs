using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.GraphModel.Model;

namespace RonSijm.AnaalIJzer.Core.Editor.Snapshots;

public sealed class ArchitectureEditorSnapshot(
    bool hasConfiguration,
    bool hasConfigurationIssues,
    ImmutableArray<ArchitectureLayerIndicator> layerIndicators,
    ImmutableArray<ArchitectureDependencySiteIndicator> siteIndicators,
    ImmutableArray<string> configurationIssueMessages,
    ArchitectureGraphSnapshot? graphSnapshot = null,
    ImmutableArray<ArchitectureLayerIndicator> unclassifiedTypeIndicators = default,
    ImmutableArray<ArchitectureNameRuleIndicator> nameRuleIndicators = default,
    ImmutableArray<ArchitectureVisibilityPolicyIndicator> visibilityPolicyIndicators = default,
    ImmutableArray<ArchitectureApiSurfaceIndicator> apiSurfaceIndicators = default)
{
    public bool HasConfiguration { get; } = hasConfiguration;

    public bool HasConfigurationIssues { get; } = hasConfigurationIssues;

    public ImmutableArray<ArchitectureLayerIndicator> LayerIndicators { get; } = layerIndicators;

    public ImmutableArray<ArchitectureDependencySiteIndicator> SiteIndicators { get; } = siteIndicators;

    public ImmutableArray<ArchitectureLayerIndicator> UnclassifiedTypeIndicators { get; } = unclassifiedTypeIndicators.IsDefault ? ImmutableArray<ArchitectureLayerIndicator>.Empty : unclassifiedTypeIndicators;

    public ImmutableArray<ArchitectureNameRuleIndicator> NameRuleIndicators { get; } = nameRuleIndicators.IsDefault ? ImmutableArray<ArchitectureNameRuleIndicator>.Empty : nameRuleIndicators;

    public ImmutableArray<ArchitectureVisibilityPolicyIndicator> VisibilityPolicyIndicators { get; } = visibilityPolicyIndicators.IsDefault ? ImmutableArray<ArchitectureVisibilityPolicyIndicator>.Empty : visibilityPolicyIndicators;

    public ImmutableArray<ArchitectureApiSurfaceIndicator> ApiSurfaceIndicators { get; } = apiSurfaceIndicators.IsDefault ? ImmutableArray<ArchitectureApiSurfaceIndicator>.Empty : apiSurfaceIndicators;

    public ImmutableArray<string> ConfigurationIssueMessages { get; } = configurationIssueMessages;

    public ArchitectureGraphSnapshot GraphSnapshot { get; } = graphSnapshot ?? ArchitectureGraphSnapshot.Empty;

    public static ArchitectureEditorSnapshot Empty { get; } = new(
		false,
		false,
		ImmutableArray<ArchitectureLayerIndicator>.Empty,
		ImmutableArray<ArchitectureDependencySiteIndicator>.Empty,
		ImmutableArray<string>.Empty);
}
