using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Graphing.ViewModels;

namespace RonSijm.AnaalIJzer.Graphing.Building;

public static partial class ArchitectureGraphViewModelBuilder
{
	private static GraphDiagram BuildConcreteDiagram(ImmutableArray<ArchitectureGraphLayer> layers, ImmutableArray<ArchitectureGraphRule> rules, ArchitectureGraphEvidence evidence, ImmutableArray<ArchitectureGraphDependencyEvidence> componentEvidence)
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
				CountOutgoingViolations(componentEvidence, node.Layer.Path, false)))
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
				CountOutgoingViolations(componentEvidence, boundary.Layer.Path, true)))
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

	private static bool LayerMatches(string candidateLayerPath, string layerPath, bool includeDescendants)
	{
		var result = string.Equals(candidateLayerPath, layerPath, StringComparison.Ordinal)
		             || includeDescendants && candidateLayerPath.StartsWith(layerPath + "/", StringComparison.Ordinal);

		return result;
	}

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

	private static LayoutResult PackItems(ImmutableArray<LayoutItem> items, double startY)
	{
		var rowsByLane = new Dictionary<int, LayoutRow>();
		var placedSpans = new List<(double Left, double Right, int MinimumLevel)>();
		foreach (var item in items.OrderBy(item => item.MinimumLevel).ThenBy(item => item.PreferredLane).ThenBy(item => item.MinimumOrder))
		{
			if (!rowsByLane.TryGetValue(item.PreferredLane, out var row))
			{
				row = new LayoutRow();
				rowsByLane.Add(item.PreferredLane, row);
			}

			var shifted = ShiftPastEarlierLevels(item, placedSpans);
			var placed = row.Add(shifted);
			placedSpans.Add((placed.Left, placed.Right, placed.MinimumLevel));
		}

		var placedItems = rowsByLane.Values.SelectMany(row => row.Items).ToImmutableArray();
		var nodes = ImmutableArray.CreateBuilder<NodePlacement>();
		var boundaries = ImmutableArray.CreateBuilder<BoundaryPlacement>();
		var y = startY;
		foreach (var row in rowsByLane.OrderBy(item => item.Key).Select(item => item.Value))
		{
			foreach (var item in row.Items)
			{
				var shifted = item.Result.ShiftY(y);
				nodes.AddRange(shifted.Nodes);
				boundaries.AddRange(shifted.Boundaries);
			}

			y += row.Height + BlockRowGap;
		}

		var result = new LayoutResult(nodes.ToImmutable(), boundaries.ToImmutable(), placedItems.Min(item => item.Left), startY, placedItems.Max(item => item.Right), Math.Max(startY, y - BlockRowGap), placedItems.Min(item => item.MinimumLevel), placedItems.Min(item => item.MinimumOrder));

		return result;
	}

	private static LayoutItem ShiftPastEarlierLevels(LayoutItem item, List<(double Left, double Right, int MinimumLevel)> placedSpans)
	{
		var shift = 0d;
		while (true)
		{
			var shifted = item.ShiftX(shift);
			var overlaps = placedSpans
				.Where(span => span.MinimumLevel < item.MinimumLevel && SpansOverlap(shifted.Left, shifted.Right, span.Left, span.Right))
				.ToImmutableArray();
			if (overlaps.Length == 0)
			{
				return shifted;
			}

			shift = overlaps.Max(span => span.Right + BlockHorizontalGap - item.Left);
		}
	}

	private static bool SpansOverlap(double left, double right, double otherLeft, double otherRight)
	{
		var result = left < otherRight + BlockHorizontalGap && right + BlockHorizontalGap > otherLeft;

		return result;
	}

	private static Dictionary<string, int> BuildVerticalLanes(ImmutableArray<ArchitectureGraphLayer> layers, ImmutableArray<ArchitectureGraphRule> rules, ImmutableDictionary<string, int> levels, Dictionary<string, int> order)
	{
		var layerPaths = layers.Select(layer => layer.Path).ToImmutableHashSet(StringComparer.Ordinal);
		var outgoing = layers.ToDictionary(layer => layer.Path, _ => new List<string>(), StringComparer.Ordinal);
		var incoming = layers.ToDictionary(layer => layer.Path, _ => new List<string>(), StringComparer.Ordinal);
		foreach (var rule in rules)
		{
			if (!layerPaths.Contains(rule.From) || !layerPaths.Contains(rule.To) || rule.From == rule.To || IsContainmentRelationship(rule.From, rule.To))
			{
				continue;
			}

			if (outgoing[rule.From].Contains(rule.To, StringComparer.Ordinal))
			{
				continue;
			}

			outgoing[rule.From].Add(rule.To);
			incoming[rule.To].Add(rule.From);
		}

		var rawLanes = layers.ToDictionary(layer => layer.Path, _ => 0d, StringComparer.Ordinal);
		var forwardProposals = layers.ToDictionary(layer => layer.Path, _ => new List<double>(), StringComparer.Ordinal);
		foreach (var path in layers.OrderBy(layer => levels[layer.Path]).ThenBy(layer => order[layer.Path]).Select(layer => layer.Path))
		{
			if (forwardProposals[path].Count > 0)
			{
				rawLanes[path] = forwardProposals[path].Average();
			}

			var targets = outgoing[path]
				.OrderBy(target => levels[target])
				.ThenBy(target => order[target])
				.ToImmutableArray();
			for (var index = 0; index < targets.Length; index++)
			{
				forwardProposals[targets[index]].Add(rawLanes[path] + CalculateFanOffset(index, targets.Length));
			}
		}

		var reverseProposals = layers.ToDictionary(layer => layer.Path, _ => new List<double>(), StringComparer.Ordinal);
		foreach (var path in layers.OrderBy(layer => levels[layer.Path]).ThenBy(layer => order[layer.Path]).Select(layer => layer.Path))
		{
			var sources = incoming[path]
				.OrderBy(source => rawLanes[source])
				.ThenBy(source => order[source])
				.ToImmutableArray();
			for (var index = 0; index < sources.Length; index++)
			{
				reverseProposals[sources[index]].Add(rawLanes[path] + CalculateFanOffset(index, sources.Length));
			}
		}

		foreach (var path in layers.OrderByDescending(layer => levels[layer.Path]).ThenByDescending(layer => order[layer.Path]).Select(layer => layer.Path))
		{
			if (incoming[path].Count == 0 && reverseProposals[path].Count > 0)
			{
				rawLanes[path] = reverseProposals[path].Average();
			}
		}

		var orderedLaneValues = rawLanes.Values
			.Select(value => Math.Round(value, 3))
			.Distinct()
			.OrderBy(value => value)
			.Select((value, index) => (value, index))
			.ToDictionary(item => item.value, item => item.index);
		var result = layers.ToDictionary(layer => layer.Path, layer => orderedLaneValues[Math.Round(rawLanes[layer.Path], 3)], StringComparer.Ordinal);

		return result;
	}

	private static double CalculateFanOffset(int index, int count)
	{
		if (count <= 1)
		{
			return 0;
		}

		var result = index - (count - 1) / 2d;

		return result;
	}

	private static ImmutableDictionary<string, int> BuildNodeLevels(ImmutableArray<ArchitectureGraphLayer> layers, ImmutableArray<ArchitectureGraphRule> rules)
	{
		var order = layers.Select((layer, index) => (layer.Path, Index: index)).ToDictionary(item => item.Path, item => item.Index, StringComparer.Ordinal);
		var levels = layers.ToDictionary(layer => layer.Path, _ => 0, StringComparer.Ordinal);
		var indegree = layers.ToDictionary(layer => layer.Path, _ => 0, StringComparer.Ordinal);
		var outgoing = layers.ToDictionary(layer => layer.Path, _ => new List<string>(), StringComparer.Ordinal);
		foreach (var rule in rules)
		{
			if (!outgoing.ContainsKey(rule.From) || !indegree.ContainsKey(rule.To) || rule.From == rule.To || IsContainmentRelationship(rule.From, rule.To))
			{
				continue;
			}

			outgoing[rule.From].Add(rule.To);
			indegree[rule.To]++;
		}

		var queue = new Queue<string>(indegree.Where(item => item.Value == 0).OrderBy(item => order[item.Key]).Select(item => item.Key));
		var topologicalOrder = ImmutableArray.CreateBuilder<string>();
		var visited = 0;
		while (queue.Count > 0)
		{
			var current = queue.Dequeue();
			topologicalOrder.Add(current);
			visited++;
			foreach (var next in outgoing[current].OrderBy(path => order[path]))
			{
				levels[next] = Math.Max(levels[next], levels[current] + 1);
				indegree[next]--;
				if (indegree[next] == 0)
				{
					queue.Enqueue(next);
				}
			}
		}

		if (visited < layers.Length)
		{
			foreach (var layer in layers.Where(layer => indegree[layer.Path] > 0).OrderBy(layer => order[layer.Path]))
			{
				levels[layer.Path] = order[layer.Path];
			}
		}
		else
		{
			RelaxLevels(layers, rules, levels);
			PullSourcesTowardTargets(topologicalOrder.ToImmutable(), outgoing, levels);
			RelaxLevels(layers, rules, levels);
		}

		var result = levels.ToImmutableDictionary(StringComparer.Ordinal);

		return result;
	}

	private static void RelaxLevels(ImmutableArray<ArchitectureGraphLayer> layers, ImmutableArray<ArchitectureGraphRule> rules, Dictionary<string, int> levels)
	{
		var layerPaths = layers.Select(layer => layer.Path).ToImmutableHashSet(StringComparer.Ordinal);
		var orderedLayers = layers.OrderBy(layer => layer.Depth).ThenBy(layer => layer.Path, StringComparer.Ordinal).ToImmutableArray();
		var maxIterations = Math.Max(1, layers.Length * 3);
		for (var iteration = 0; iteration < maxIterations; iteration++)
		{
			var changed = false;
			foreach (var layer in orderedLayers)
			{
				var parentPath = GetParentPath(layer.Path);
				if (parentPath.Length > 0 && levels.TryGetValue(parentPath, out var parentLevel) && levels[layer.Path] < parentLevel)
				{
					levels[layer.Path] = parentLevel;
					changed = true;
				}
			}

			foreach (var rule in rules)
			{
				if (!layerPaths.Contains(rule.From) || !layerPaths.Contains(rule.To) || rule.From == rule.To || IsContainmentRelationship(rule.From, rule.To))
				{
					continue;
				}

				var requiredLevel = levels[rule.From] + 1;
				if (levels[rule.To] < requiredLevel)
				{
					levels[rule.To] = requiredLevel;
					changed = true;
				}
			}

			if (!changed)
			{
				return;
			}
		}
	}

	private static void PullSourcesTowardTargets(ImmutableArray<string> topologicalOrder, Dictionary<string, List<string>> outgoing, Dictionary<string, int> levels)
	{
		foreach (var path in topologicalOrder.Reverse())
		{
			var targets = outgoing[path];
			if (targets.Count == 0)
			{
				continue;
			}

			var rightMostAllowedLevel = targets.Min(target => levels[target] - 1);
			if (rightMostAllowedLevel > levels[path])
			{
				levels[path] = rightMostAllowedLevel;
			}
		}
	}
}
