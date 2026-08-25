using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Graphing.Model;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;

namespace RonSijm.AnaalIJzer.EditorRuntime.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static ImmutableArray<string> GetLayersThatCanCall(ProjectAnalyzerConfig config, string layerPath)
	{
		var result = config.Graph.DependencyEdges
			.Where(edge => edge.IsAllowed && EndpointTouchesLayer(edge.To, layerPath))
			.Select(edge => FormatLayerEndpoint(edge.From))
			.Distinct(StringComparer.Ordinal)
			.ToImmutableArray();

		return result;
	}

	private static ImmutableArray<string> GetLayersThisLayerCanCall(ProjectAnalyzerConfig config, string layerPath)
	{
		var result = config.Graph.DependencyEdges
			.Where(edge => edge.IsAllowed && EndpointTouchesLayer(edge.From, layerPath))
			.Select(edge => FormatLayerEndpoint(edge.To))
			.Distinct(StringComparer.Ordinal)
			.ToImmutableArray();

		return result;
	}

	private static string FormatLayerEndpoint(string endpoint)
	{
		var result = endpoint == "*" ? "* (any layer)" : endpoint;

		return result;
	}

	private static string? FindLayerDescription(ProjectAnalyzerConfig config, string layerPath)
	{
		var result = config.Documentation.Items.FirstOrDefault(item => item.Kind == "Layer" && item.LayerPath == layerPath).Description;

		return result;
	}

	private static string? FindDependencyRuleDescription(ProjectAnalyzerConfig config, DependencyEdge edge)
	{
		var kind = edge.IsBlocked ? "BlockedDependency" : "AllowedDependency";
		var result = config.Documentation.Items
			.FirstOrDefault(item => item.Kind == kind
			                        && string.Equals(item.SourcePath, edge.XmlPath, StringComparison.OrdinalIgnoreCase)
			                        && item.XmlLineNumber == edge.XmlLineNumber)
			.Description;

		return result;
	}

	private static ImmutableArray<string> GetLayerExceptionReviewSummaries(ImmutableArray<ArchitectureGraphExceptionReview> exceptionReviews, string layerPath)
	{
		var result = exceptionReviews
			.Where(review => string.Equals(review.OwnerLayerPath, layerPath, StringComparison.Ordinal)
			                 || review.OwnerLayerPath.StartsWith(layerPath + "/", StringComparison.Ordinal))
			.OrderBy(review => GetExceptionStatusSortOrder(review.Status))
			.ThenBy(review => review.MatcherLabel, StringComparer.Ordinal)
			.Select(review => "[" + review.Status + "] " + review.MatcherKind + " " + review.MatcherLabel)
			.Take(6)
			.ToImmutableArray();

		return result;
	}

	private static int GetExceptionStatusSortOrder(string status)
	{
		var result = status switch
		{
			"Invalid" => 0,
			"Expired" => 1,
			"ExpiringSoon" => 2,
			"Stale" => 3,
			"Active" => 4,
			_ => 5
		};

		return result;
	}
}
