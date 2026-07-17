using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.DependencyRules;

internal static class DependencyCycleDetector
{
	public static ImmutableArray<ImmutableArray<string>> FindConfiguredCycles(IEnumerable<string> layerNames, IEnumerable<DependencyEdge> edges)
	{
		var edgeArray = edges.ToArray();
		var blockedRules = edgeArray.Where(edge => edge.IsBlocked && !edge.SiteFilter.HasFilter).ToArray();
		var allowedPairs = edgeArray
			.Where(edge => edge.IsAllowed && edge.IsExplicit && edge.From != edge.To && !blockedRules.Any(blocked => Matches(blocked, edge.From, edge.To)))
			.Select(edge => (edge.From, edge.To));
		return FindCycles(layerNames, allowedPairs);
	}

	public static ImmutableArray<ImmutableArray<string>> FindCycles(IEnumerable<string> layerNames, IEnumerable<(string From, string To)> edges)
	{
		var layers = new HashSet<string>(layerNames, StringComparer.Ordinal);
		var adjacency = layers.ToDictionary(layer => layer, _ => new List<string>(), StringComparer.Ordinal);
		foreach (var edge in edges)
		{
			if (edge.From != edge.To && layers.Contains(edge.From) && layers.Contains(edge.To) && !adjacency[edge.From].Contains(edge.To, StringComparer.Ordinal))
			{
				adjacency[edge.From].Add(edge.To);
			}
		}

		foreach (var dependencies in adjacency.Values)
		{
			dependencies.Sort(StringComparer.Ordinal);
		}

		var cycles = new Dictionary<string, ImmutableArray<string>>(StringComparer.Ordinal);
		foreach (var start in layers.OrderBy(name => name, StringComparer.Ordinal))
		{
			FindFrom(start, start, adjacency, new List<string>(), new HashSet<string>(StringComparer.Ordinal), cycles);
		}
		return [..cycles.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value)];
	}

	private static void FindFrom(string start, string current, IReadOnlyDictionary<string, List<string>> adjacency, List<string> path, HashSet<string> pathSet, Dictionary<string, ImmutableArray<string>> cycles)
	{
		path.Add(current);
		pathSet.Add(current);

		foreach (var next in adjacency[current])
		{
			if (next == start && path.Count > 1)
			{
				var cycle = path.ToImmutableArray();
				var key = GetCanonicalKey(cycle);
				if (!cycles.ContainsKey(key))
				{
					cycles.Add(key, cycle);
				}
				continue;
			}

			if (!pathSet.Contains(next))
			{
				FindFrom(start, next, adjacency, path, pathSet, cycles);
			}
		}

		pathSet.Remove(current);
		path.RemoveAt(path.Count - 1);
	}

	private static string GetCanonicalKey(ImmutableArray<string> cycle)
	{
		var rotations = new string[cycle.Length];
		for (var offset = 0; offset < cycle.Length; offset++)
		{
			rotations[offset] = string.Join("\u001f", Enumerable.Range(0, cycle.Length).Select(index => cycle[(offset + index) % cycle.Length]));
		}
		return rotations.OrderBy(value => value, StringComparer.Ordinal).First();
	}

	private static bool Matches(DependencyEdge edge, string from, string to)
	{
		var result = (edge.From == "*" || edge.From == from) && (edge.To == "*" || edge.To == to);

		return result;
	}
}
