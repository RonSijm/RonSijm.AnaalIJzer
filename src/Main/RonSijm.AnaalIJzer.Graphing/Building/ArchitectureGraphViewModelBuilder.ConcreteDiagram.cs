using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
using RonSijm.AnaalIJzer.GraphModel.Model;

namespace RonSijm.AnaalIJzer.Graphing.Building;

internal static partial class ArchitectureGraphViewModelBuilder
{
	private static GraphDiagram BuildConcreteDiagram(ImmutableArray<ArchitectureGraphLayer> layers, ImmutableArray<ArchitectureGraphRule> rules, ImmutableArray<ArchitectureGraphExceptionReview> exceptionReviews, ArchitectureGraphEvidence evidence, ImmutableArray<ArchitectureGraphDependencyEvidence> componentEvidence)
	{
		var levels = BuildNodeLevels(layers, rules);
		var order = layers.Select((layer, index) => (layer.Path, Index: index)).ToDictionary(item => item.Path, item => item.Index, StringComparer.Ordinal);
		var verticalLanes = BuildVerticalLanes(layers, rules, levels, order);
		var layout = BuildLayout(layers, levels, order, verticalLanes);
		var boundaryPaths = layout.Boundaries.Select(boundary => boundary.Layer.Path).ToImmutableHashSet(StringComparer.Ordinal);
		var nodes = layout.Nodes
			.Where(node => !boundaryPaths.Contains(node.Layer.Path))
			.Select(node => new ArchitectureGraphNodeViewModel(
				node.Layer.Path,
				node.Layer.DisplayName,
				node.Layer.Description,
				node.Layer.Depth,
				node.Layer.PaletteSlot,
				node.Layer.IsActive,
				node.X,
				node.Y,
				node.Layer.EditHandle,
				GetTypeEvidence(evidence, node.Layer.Path, false),
				CountIncoming(componentEvidence, node.Layer.Path, false),
				CountOutgoing(componentEvidence, node.Layer.Path, false),
				CountIncomingViolations(componentEvidence, node.Layer.Path, false),
				CountOutgoingViolations(componentEvidence, node.Layer.Path, false),
				CountExceptionReviews(exceptionReviews, node.Layer.Path, false),
				GetExceptionReviewSummaries(exceptionReviews, node.Layer.Path, false)))
			.ToImmutableArray();
		var ruleEdges = rules
			.Select(CreateEdge)
			.ToImmutableArray();
		var evidenceEdges = CreateEvidenceEdges(componentEvidence);
		var edges = ruleEdges.AddRange(evidenceEdges);
		var boundaries = layout.Boundaries
			.Select(boundary => new ArchitectureGraphBoundaryViewModel(
				boundary.Layer.Path,
				boundary.Layer.DisplayName,
				boundary.Layer.Description,
				boundary.Layer.Depth,
				boundary.Layer.PaletteSlot,
				boundary.IsActive,
				boundary.X,
				boundary.Y,
				boundary.Width,
				boundary.Height,
				boundary.Layer.EditHandle,
				GetTypeEvidence(evidence, boundary.Layer.Path, true),
				CountIncoming(componentEvidence, boundary.Layer.Path, true),
				CountOutgoing(componentEvidence, boundary.Layer.Path, true),
				CountIncomingViolations(componentEvidence, boundary.Layer.Path, true),
				CountOutgoingViolations(componentEvidence, boundary.Layer.Path, true),
				CountExceptionReviews(exceptionReviews, boundary.Layer.Path, true),
				GetExceptionReviewSummaries(exceptionReviews, boundary.Layer.Path, true)))
			.ToImmutableArray();
		var result = new GraphDiagram(nodes, edges, boundaries);

		return result;
	}

	private static ImmutableArray<ArchitectureGraphTypeEvidence> GetTypeEvidence(ArchitectureGraphEvidence evidence, string layerPath, bool includeDescendants)
	{
		var result = evidence.Types
			.Where(type => LayerMatches(type.LayerPath, layerPath, includeDescendants))
			.OrderBy(type => type.FullTypeName, StringComparer.Ordinal)
			.ToImmutableArray();

		return result;
	}

	private static int CountIncoming(ImmutableArray<ArchitectureGraphDependencyEvidence> dependencies, string layerPath, bool includeDescendants)
	{
		var result = dependencies.Count(dependency => LayerMatches(dependency.DependencyLayerPath, layerPath, includeDescendants));

		return result;
	}

	private static int CountOutgoing(ImmutableArray<ArchitectureGraphDependencyEvidence> dependencies, string layerPath, bool includeDescendants)
	{
		var result = dependencies.Count(dependency => LayerMatches(dependency.CallerLayerPath, layerPath, includeDescendants));

		return result;
	}

	private static int CountIncomingViolations(ImmutableArray<ArchitectureGraphDependencyEvidence> dependencies, string layerPath, bool includeDescendants)
	{
		var result = dependencies.Count(dependency => dependency.IsViolation && LayerMatches(dependency.DependencyLayerPath, layerPath, includeDescendants));

		return result;
	}

	private static int CountOutgoingViolations(ImmutableArray<ArchitectureGraphDependencyEvidence> dependencies, string layerPath, bool includeDescendants)
	{
		var result = dependencies.Count(dependency => dependency.IsViolation && LayerMatches(dependency.CallerLayerPath, layerPath, includeDescendants));

		return result;
	}

	private static int CountExceptionReviews(ImmutableArray<ArchitectureGraphExceptionReview> reviews, string layerPath, bool includeDescendants)
	{
		var result = reviews.Count(review => LayerMatches(review.OwnerLayerPath, layerPath, includeDescendants));

		return result;
	}

	private static ImmutableArray<string> GetExceptionReviewSummaries(ImmutableArray<ArchitectureGraphExceptionReview> reviews, string layerPath, bool includeDescendants)
	{
		var result = reviews
			.Where(review => LayerMatches(review.OwnerLayerPath, layerPath, includeDescendants))
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

	private static bool LayerMatches(string candidateLayerPath, string layerPath, bool includeDescendants)
	{
		var result = string.Equals(candidateLayerPath, layerPath, StringComparison.Ordinal)
		             || includeDescendants && candidateLayerPath.StartsWith(layerPath + "/", StringComparison.Ordinal);

		return result;
	}
}
