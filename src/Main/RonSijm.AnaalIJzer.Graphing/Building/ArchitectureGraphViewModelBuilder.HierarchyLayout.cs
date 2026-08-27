using System.Collections.Immutable;
using RonSijm.AnaalIJzer.GraphModel.Model;

namespace RonSijm.AnaalIJzer.Graphing.Building;

internal static partial class ArchitectureGraphViewModelBuilder
{
	private static LayoutResult BuildLayout(ImmutableArray<ArchitectureGraphLayer> layers, ImmutableDictionary<string, int> levels, Dictionary<string, int> order, Dictionary<string, int> verticalLanes)
	{
		var tree = BuildLayerTree(layers);
		var items = tree
			.Select(root => LayoutItem.FromResult(LayoutLayer(root, levels, order, verticalLanes), levels[root.Layer.Path], order[root.Layer.Path], verticalLanes[root.Layer.Path]))
			.ToImmutableArray();
		var result = PackItems(items, NodeStartY);

		return result;
	}

	private static ImmutableArray<LayerTreeNode> BuildLayerTree(ImmutableArray<ArchitectureGraphLayer> layers)
	{
		var byParent = layers
			.GroupBy(layer => GetParentPath(layer.Path), StringComparer.Ordinal)
			.ToDictionary(group => group.Key, group => group.ToImmutableArray(), StringComparer.Ordinal);
		var result = BuildLayerTree(string.Empty, byParent);

		return result;
	}

	private static ImmutableArray<LayerTreeNode> BuildLayerTree(string parentPath, Dictionary<string, ImmutableArray<ArchitectureGraphLayer>> byParent)
	{
		if (!byParent.TryGetValue(parentPath, out var children))
		{
			return ImmutableArray<LayerTreeNode>.Empty;
		}

		var result = children
			.Select(child => new LayerTreeNode(child, BuildLayerTree(child.Path, byParent)))
			.ToImmutableArray();

		return result;
	}

	private static LayoutResult LayoutLayer(LayerTreeNode tree, ImmutableDictionary<string, int> levels, Dictionary<string, int> order, Dictionary<string, int> verticalLanes)
	{
		var nodeX = NodeStartX + levels[tree.Layer.Path] * NodeColumnWidth;
		if (tree.Children.Length == 0)
		{
			return LayoutResult.FromNode(tree.Layer, nodeX);
		}

		var contentItems = ImmutableArray.CreateBuilder<LayoutItem>();
		foreach (var child in tree.Children)
		{
			var childLayout = LayoutLayer(child, levels, order, verticalLanes);
			contentItems.Add(LayoutItem.FromResult(childLayout, childLayout.MinimumLevel, childLayout.MinimumOrder, verticalLanes[child.Layer.Path]));
		}

		var content = PackItems(contentItems.ToImmutable(), BoundaryPaddingTop);
		var boundaryLeft = Math.Min(content.Left, nodeX) - BoundaryPaddingX;
		var boundaryRight = Math.Max(content.Right, nodeX + NodeVisualWidth) + BoundaryPaddingX;
		var boundaryBottom = content.Bottom + BoundaryPaddingBottom;
		var boundary = new BoundaryPlacement(tree.Layer, boundaryLeft, 0, boundaryRight - boundaryLeft, boundaryBottom, content.Nodes.Any(node => node.Layer.IsActive));
		var result = content.WithBoundary(boundary, boundaryLeft, 0, boundaryRight, boundaryBottom, levels[tree.Layer.Path], order[tree.Layer.Path]);

		return result;
	}
}
