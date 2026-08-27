using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.GraphModel.Model;

namespace RonSijm.AnaalIJzer.GraphModel.Building;

public static class ArchitectureGraphSnapshotFactory
{
	public static ArchitectureGraphSnapshot CreateSnapshot(
		ArchitectureGraphSnapshotInput input,
		ArchitectureGraphEvidence? evidence = null,
		ImmutableArray<ArchitectureGraphExceptionReview> exceptionReviews = default)
	{
		var layers = input.Layers
			.Select(layer => new ArchitectureGraphLayer(
				layer.Path,
				layer.DisplayName,
				layer.Description,
				layer.Depth,
				layer.PaletteSlot,
				layer.IsActive,
				layer.SourcePath,
				layer.SourceKind,
				layer.XmlLineNumber))
			.ToImmutableArray();
		var rules = input.Rules
			.Select(rule => new ArchitectureGraphRule(
				rule.From,
				rule.To,
				rule.ScopePath,
				rule.Kind,
				rule.SiteText,
				rule.AppliesToDescendants,
				rule.IsWildcard,
				rule.IsActive,
				rule.ConfiguredFrom,
				rule.ConfiguredTo,
				rule.SourcePath,
				rule.SourceKind,
				rule.XmlLineNumber,
				rule.XmlLinePosition,
				rule.AllowedSites,
				rule.BlockedSites,
				rule.Description))
			.ToImmutableArray();
		var result = new ArchitectureGraphSnapshot(
			input.HasConfiguration,
			input.HasConfigurationIssues,
			layers,
			rules,
			input.ActiveLayerPaths,
			input.ConfigurationIssueMessages,
			input.ConfigurationSource,
			evidence,
			input.ConfigurationCreationTargets,
			exceptionReviews);

		return result;
	}

	public static ArchitectureGraphSnapshot CreateNoConfigurationSnapshot(ImmutableArray<ArchitectureConfigurationCreationTarget> configurationCreationTargets)
	{
		var input = new ArchitectureGraphSnapshotInput(
			hasConfiguration: false,
			hasConfigurationIssues: false,
			layers: ImmutableArray<ArchitectureGraphLayerInput>.Empty,
			rules: ImmutableArray<ArchitectureGraphRuleInput>.Empty,
			activeLayerPaths: ImmutableArray<string>.Empty,
			configurationIssueMessages: ImmutableArray<string>.Empty,
			configurationCreationTargets: configurationCreationTargets);
		var result = CreateSnapshot(input, ArchitectureGraphEvidence.Empty, ImmutableArray<ArchitectureGraphExceptionReview>.Empty);

		return result;
	}

	public static ArchitectureGraphSnapshot AttachEvidence(
		ArchitectureGraphSnapshot configSnapshot,
		ArchitectureGraphEvidence evidence,
		ImmutableArray<ArchitectureGraphExceptionReview> exceptionReviews = default)
	{
		var result = new ArchitectureGraphSnapshot(
			configSnapshot.HasConfiguration,
			configSnapshot.HasConfigurationIssues,
			configSnapshot.Layers,
			configSnapshot.Rules,
			configSnapshot.ActiveLayerPaths,
			configSnapshot.ConfigurationIssueMessages,
			configSnapshot.ConfigurationSource,
			evidence,
			configSnapshot.ConfigurationCreationTargets,
			exceptionReviews);

		return result;
	}
}
