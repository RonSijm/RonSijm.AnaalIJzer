using System.Collections.Immutable;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;

namespace RonSijm.AnaalIJzer.EditorRuntime.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static ImmutableArray<string> GetLinearCallChain(ProjectAnalyzerConfig config, string layerPath)
	{
		var edges = config.Graph.DependencyEdges
			.Where(edge => edge.IsAllowed && IsDirectLayerEndpoint(edge.From) && IsDirectLayerEndpoint(edge.To))
			.Select(edge => (edge.From, edge.To))
			.Distinct()
			.ToArray();
		if (edges.Length == 0)
		{
			return ImmutableArray<string>.Empty;
		}

		var nodes = edges.Select(edge => edge.From).Concat(edges.Select(edge => edge.To)).Distinct(StringComparer.Ordinal).ToImmutableHashSet(StringComparer.Ordinal);
		if (!nodes.Contains(layerPath))
		{
			return ImmutableArray<string>.Empty;
		}

		var outgoing = BuildEdgeLookup(edges, edge => edge.From, edge => edge.To);
		var incoming = BuildEdgeLookup(edges, edge => edge.To, edge => edge.From);
		var component = GetConnectedComponent(layerPath, nodes, outgoing, incoming);
		if (component.Any(node => GetLookupValues(outgoing, node).Length > 1 || GetLookupValues(incoming, node).Length > 1))
		{
			return ImmutableArray<string>.Empty;
		}

		var starts = component.Where(node => GetLookupValues(incoming, node).Length == 0).ToArray();
		if (starts.Length != 1)
		{
			return ImmutableArray<string>.Empty;
		}

		var chain = ImmutableArray.CreateBuilder<string>();
		var seen = new HashSet<string>(StringComparer.Ordinal);
		var current = starts[0];
		while (seen.Add(current))
		{
			chain.Add(current);
			var next = GetLookupValues(outgoing, current);
			if (next.Length == 0)
			{
				break;
			}

			current = next[0];
		}

		var result = chain.Count == component.Count && chain.Count > 1 && chain.Contains(layerPath, StringComparer.Ordinal)
			? chain.ToImmutable()
			: ImmutableArray<string>.Empty;

		return result;
	}

	private static bool IsDirectLayerEndpoint(string endpoint)
	{
		var result = !string.IsNullOrWhiteSpace(endpoint) && endpoint != "*";

		return result;
	}

	private static ImmutableDictionary<string, ImmutableArray<string>> BuildEdgeLookup(
		IEnumerable<(string From, string To)> edges,
		Func<(string From, string To), string> keySelector,
		Func<(string From, string To), string> valueSelector)
	{
		var result = edges
			.GroupBy(keySelector, StringComparer.Ordinal)
			.ToImmutableDictionary(
				group => group.Key,
				group => group.Select(valueSelector).Distinct(StringComparer.Ordinal).ToImmutableArray(),
				StringComparer.Ordinal);

		return result;
	}

	private static ImmutableArray<string> GetLookupValues(ImmutableDictionary<string, ImmutableArray<string>> lookup, string key)
	{
		var result = lookup.TryGetValue(key, out var values) ? values : ImmutableArray<string>.Empty;

		return result;
	}

	private static ImmutableHashSet<string> GetConnectedComponent(
		string layerPath,
		ImmutableHashSet<string> nodes,
		ImmutableDictionary<string, ImmutableArray<string>> outgoing,
		ImmutableDictionary<string, ImmutableArray<string>> incoming)
	{
		var component = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
		var stack = new Stack<string>();
		stack.Push(layerPath);
		while (stack.Count > 0)
		{
			var current = stack.Pop();
			if (!nodes.Contains(current) || !component.Add(current))
			{
				continue;
			}

			foreach (var next in GetLookupValues(outgoing, current).Concat(GetLookupValues(incoming, current)))
			{
				stack.Push(next);
			}
		}

		var result = component.ToImmutable();

		return result;
	}
}
