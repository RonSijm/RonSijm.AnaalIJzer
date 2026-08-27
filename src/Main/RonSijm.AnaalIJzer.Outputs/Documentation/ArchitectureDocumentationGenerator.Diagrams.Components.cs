using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.LayerModel;

namespace RonSijm.AnaalIJzer.Outputs.Documentation;

internal static partial class ArchitectureDocumentationMarkdownBuilder
{
	private static ImmutableArray<ImmutableArray<LayerNode>> GetConnectedComponents(ImmutableArray<LayerNode> layers, ImmutableArray<DependencyEdge> explicitEdges)
	{
		var seen = new HashSet<string>(StringComparer.Ordinal);
		var components = ImmutableArray.CreateBuilder<ImmutableArray<LayerNode>>();
		var nodesByName = layers.ToDictionary(node => node.Definition.Name, StringComparer.Ordinal);

		foreach (var layer in layers)
		{
			var layerName = layer.Definition.Name;
			if (!seen.Add(layerName))
			{
				continue;
			}

			var queue = new Queue<string>();
			var component = ImmutableArray.CreateBuilder<LayerNode>();
			queue.Enqueue(layerName);

			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				component.Add(nodesByName[current]);

				foreach (var edge in explicitEdges)
				{
					var fromRoot = GetRootName(edge.From);
					var toRoot = GetRootName(edge.To);
					if (fromRoot != current && toRoot != current)
					{
						continue;
					}

					var next = fromRoot == current ? toRoot : fromRoot;
					if (seen.Add(next))
					{
						queue.Enqueue(next);
					}
				}
			}

			components.Add(component.ToImmutable());
		}

		return components.ToImmutable();
	}

	private static ImmutableArray<string> FlattenLayerNames(ImmutableArray<LayerNode> layers)
	{
		var names = ImmutableArray.CreateBuilder<string>();
		foreach (var layer in layers)
		{
			AddLayerNames(layer, names);
		}

		return names.ToImmutable();
	}

	private static void AddLayerNames(LayerNode node, ImmutableArray<string>.Builder names)
	{
		names.Add(node.Definition.Name);
		foreach (var child in node.Children)
		{
			AddLayerNames(child, names);
		}
	}

	private static HashSet<string> GetEntryLayerPaths(ImmutableArray<LayerNode> layers)
	{
		var entryLayers = new HashSet<string>(StringComparer.Ordinal);
		foreach (var layer in layers)
		{
			AddEntryLayerPaths(layer, entryLayers);
		}

		return entryLayers;
	}

	private static void AddEntryLayerPaths(LayerNode node, ISet<string> entryLayers)
	{
		foreach (var policy in node.EntryPointPolicies)
		{
			foreach (var rule in policy.Rules)
			{
				if (rule.Selector.LayerPath is { } layerPath)
				{
					entryLayers.Add(layerPath);
				}
			}
		}

		foreach (var child in node.Children)
		{
			AddEntryLayerPaths(child, entryLayers);
		}
	}

	private static string GetLayerLabel(string layerPath, ISet<string> entryLayers)
	{
		var label = GetLocalName(layerPath);
		var result = entryLayers.Contains(layerPath) ? label + "\\nentry" : label;

		return result;
	}
}
