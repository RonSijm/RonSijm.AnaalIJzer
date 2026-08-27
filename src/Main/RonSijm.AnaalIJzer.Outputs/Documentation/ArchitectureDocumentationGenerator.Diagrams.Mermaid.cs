using System.Collections.Immutable;
using System.Text;
using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.LayerModel;

namespace RonSijm.AnaalIJzer.Outputs.Documentation;

internal static partial class ArchitectureDocumentationMarkdownBuilder
{
	private static void AppendMermaidDiagram(StringBuilder sb, ImmutableArray<LayerNode> layers, ImmutableArray<DependencyEdge> edges)
	{
		sb.AppendLine("```mermaid");
		sb.AppendLine("flowchart LR");

		var layerIds = BuildLayerIds(layers);
		var entryLayers = GetEntryLayerPaths(layers);
		var needsWildcard = edges.Any(edge => !edge.IsExplicit);
		foreach (var layer in layers)
		{
			AppendLayerNode(sb, layer, entryLayers, 1);
		}

		if (needsWildcard)
		{
			sb.AppendLine($"    {WildcardNodeId}([\"all layers\"])");
		}

		foreach (var edge in edges)
		{
			var fromId = edge.From == "*" ? WildcardNodeId : GetLayerId(edge.From, layerIds);
			var toId = edge.To == "*" ? WildcardNodeId : GetLayerId(edge.To, layerIds);
			if (edge.IsBlocked)
			{
				var blockText = GetMermaidEdgeLabel(edge, "blocked");
				sb.AppendLine($"    {fromId} -. \"{EscapeLabel(blockText)}\" .-> {toId}");
			}
			else
			{
				var label = GetMermaidEdgeLabel(edge, string.Empty);
				var arrow = label.Length > 0 ? $"-->|\"{EscapeLabel(label)}\"| " : "--> ";
				sb.AppendLine($"    {fromId} {arrow}{toId}");
			}
		}

		foreach (var layer in layers)
		{
			AppendLayerStyle(sb, layer);
		}

		if (needsWildcard)
		{
			sb.AppendLine($"    style {WildcardNodeId} fill:#fff4cc,stroke:#cc9900,color:#000");
		}

		sb.AppendLine("```");
		sb.AppendLine();
	}

	private static IReadOnlyDictionary<string, string> BuildLayerIds(ImmutableArray<LayerNode> layers)
	{
		var ids = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var layer in layers)
		{
			AddLayerIds(ids, layer);
		}

		return ids;
	}

	private static void AddLayerIds(Dictionary<string, string> ids, LayerNode node)
	{
		ids[node.Definition.Name] = node.Children.Length == 0 ? LayerId(node.Definition.Name) : SubgraphId(node.Definition.Name);
		foreach (var child in node.Children)
		{
			AddLayerIds(ids, child);
		}
	}

	private static string GetLayerId(string layerName, IReadOnlyDictionary<string, string> layerIds)
	{
		var result = layerIds.TryGetValue(layerName, out var layerId) ? layerId : LayerId(layerName);

		return result;
	}

	private static void AppendLayerNode(StringBuilder sb, LayerNode node, ISet<string> entryLayers, int depth)
	{
		var indent = new string(' ', depth * 4);
		var localName = GetLayerLabel(node.Definition.Name, entryLayers);
		if (node.Children.Length == 0)
		{
			sb.AppendLine($"{indent}{LayerId(node.Definition.Name)}[\"{EscapeLabel(localName)}\"]");
			return;
		}

		sb.AppendLine($"{indent}subgraph {SubgraphId(node.Definition.Name)}[\"{EscapeLabel(localName)}\"]");
		sb.AppendLine($"{indent}    direction LR");
		foreach (var child in node.Children)
		{
			AppendLayerNode(sb, child, entryLayers, depth + 1);
		}
		sb.AppendLine($"{indent}end");
	}

	private static void AppendLayerStyle(StringBuilder sb, LayerNode node)
	{
		if (node.Children.Length == 0)
		{
			sb.AppendLine($"    style {LayerId(node.Definition.Name)} fill:#cce5ff,stroke:#0066cc,color:#000");
			return;
		}

		sb.AppendLine($"    style {SubgraphId(node.Definition.Name)} fill:#e8f4ff,stroke:#0066cc,color:#000");
		foreach (var child in node.Children)
		{
			AppendLayerStyle(sb, child);
		}
	}
}
