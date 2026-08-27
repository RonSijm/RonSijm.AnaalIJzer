using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.GraphModel.Building;

public sealed class ArchitectureGraphSnapshotInput(
	bool hasConfiguration,
	bool hasConfigurationIssues,
	ImmutableArray<ArchitectureGraphLayerInput> layers,
	ImmutableArray<ArchitectureGraphRuleInput> rules,
	ImmutableArray<string> activeLayerPaths,
	ImmutableArray<string> configurationIssueMessages,
	ArchitectureConfigurationSource? configurationSource = null,
	ImmutableArray<ArchitectureConfigurationCreationTarget> configurationCreationTargets = default)
{
	public bool HasConfiguration { get; } = hasConfiguration;

	public bool HasConfigurationIssues { get; } = hasConfigurationIssues;

	public ImmutableArray<ArchitectureGraphLayerInput> Layers { get; } = layers.IsDefault ? ImmutableArray<ArchitectureGraphLayerInput>.Empty : layers;

	public ImmutableArray<ArchitectureGraphRuleInput> Rules { get; } = rules.IsDefault ? ImmutableArray<ArchitectureGraphRuleInput>.Empty : rules;

	public ImmutableArray<string> ActiveLayerPaths { get; } = activeLayerPaths.IsDefault ? ImmutableArray<string>.Empty : activeLayerPaths;

	public ImmutableArray<string> ConfigurationIssueMessages { get; } = configurationIssueMessages.IsDefault ? ImmutableArray<string>.Empty : configurationIssueMessages;

	public ArchitectureConfigurationSource ConfigurationSource { get; } = configurationSource ?? ArchitectureConfigurationSource.None;

	public ImmutableArray<ArchitectureConfigurationCreationTarget> ConfigurationCreationTargets { get; } = configurationCreationTargets.IsDefault ? ImmutableArray<ArchitectureConfigurationCreationTarget>.Empty : configurationCreationTargets;
}
