using System.Collections.Immutable;
using RonSijm.AnaalIJzer.GraphModel.Model;

namespace RonSijm.AnaalIJzer.Graphing.Building;

internal static partial class ArchitectureGraphViewModelBuilder
{
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
