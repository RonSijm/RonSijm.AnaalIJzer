using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.GraphModel.Building;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Indicators;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;

namespace RonSijm.AnaalIJzer.EditorRuntime.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static ArchitectureGraphSnapshot CreateEmptyGraphSnapshot(Document document, ArchitectureConfigurationSource configurationSource, bool hasConfiguration)
	{
		var creationTargets = hasConfiguration
			? ImmutableArray<ArchitectureConfigurationCreationTarget>.Empty
			: CreateConfigurationCreationTargets(document);
		var input = new ArchitectureGraphSnapshotInput(
			hasConfiguration,
			hasConfigurationIssues: false,
			layers: ImmutableArray<ArchitectureGraphLayerInput>.Empty,
			rules: ImmutableArray<ArchitectureGraphRuleInput>.Empty,
			activeLayerPaths: ImmutableArray<string>.Empty,
			configurationIssueMessages: ImmutableArray<string>.Empty,
			configurationSource,
			creationTargets);
		var result = ArchitectureGraphSnapshotFactory.CreateSnapshot(input, ArchitectureGraphEvidence.Empty, ImmutableArray<ArchitectureGraphExceptionReview>.Empty);

		return result;
	}

	private static ArchitectureGraphSnapshot CreateGraphSnapshot(ProjectAnalyzerConfig config, ImmutableDictionary<string, int> paletteSlots, ImmutableArray<ArchitectureLayerIndicator> layerIndicators, ArchitectureConfigurationSource configurationSource, string inlineConfigPath, ArchitectureGraphEvidence evidence, ImmutableArray<ArchitectureGraphExceptionReview> exceptionReviews)
	{
		var activeLayerPaths = layerIndicators.Select(indicator => indicator.LayerPath).Distinct(StringComparer.Ordinal).ToImmutableArray();
		var layers = ImmutableArray.CreateBuilder<ArchitectureGraphLayerInput>();
		foreach (var layer in config.Layers)
		{
			AddGraphLayer(config, layer, paletteSlots, activeLayerPaths, configurationSource, inlineConfigPath, layers);
		}

		var rules = config.Graph.DependencyEdges
			.Select(edge => new ArchitectureGraphRuleInput(
				edge.From,
				edge.To,
				edge.ScopePath,
				edge.IsBlocked ? "BlockedDependency" : "AllowedDependency",
				string.IsNullOrWhiteSpace(edge.SiteFilter.ToDisplayText()) ? "all sites" : edge.SiteFilter.ToDisplayText(),
				edge.AppliesToDescendants,
				edge.From == "*" || edge.To == "*",
				RuleTouchesActiveLayer(edge, activeLayerPaths),
				edge.ConfiguredFrom,
				edge.ConfiguredTo,
				GetEditableRulePath(edge, configurationSource, inlineConfigPath),
				GetEditableRuleSourceKind(edge, configurationSource, inlineConfigPath),
				edge.XmlLineNumber,
				edge.XmlLinePosition,
				ArchitectureDependencySites.All.Where(edge.SiteFilter.AllowedSites.Contains).ToImmutableArray(),
				ArchitectureDependencySites.All.Where(edge.SiteFilter.BlockedSites.Contains).ToImmutableArray(),
				FindDependencyRuleDescription(config, edge)))
			.ToImmutableArray();
		var input = new ArchitectureGraphSnapshotInput(
			true,
			config.HasConfigurationIssues,
			layers.ToImmutable(),
			rules,
			activeLayerPaths,
			config.ConfigurationIssues.Select(issue => issue.Message).ToImmutableArray(),
			configurationSource);
		var result = ArchitectureGraphSnapshotFactory.CreateSnapshot(input, evidence, exceptionReviews);

		return result;
	}
}
