using System.Collections.Immutable;
using System.Text;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.DependencyRules;
using RonSijm.AnaalIJzer.Model;

namespace RonSijm.AnaalIJzer.Documentation;

internal static partial class ArchitectureDocumentationGenerator
{
	private static void AppendDependencyDiagrams(StringBuilder sb, AnalyzerConfig config)
	{
		sb.AppendLine("## Dependency Flow");
		sb.AppendLine();

		var explicitEdges = config.Graph.DependencyEdges.Where(edge => edge.IsExplicit).ToImmutableArray();
		var components = GetConnectedComponents(config.Layers, explicitEdges);
		if (components.Length == 0)
		{
			sb.AppendLine("No layers are configured.");
			sb.AppendLine();
		}
		else
		{
			var chainNumber = 1;
			foreach (var component in components)
			{
				var rootNames = component.Select(node => node.Definition.Name).ToImmutableHashSet(StringComparer.Ordinal);
				var componentEdges = explicitEdges.Where(edge => rootNames.Contains(GetRootName(edge.From)) && rootNames.Contains(GetRootName(edge.To))).ToImmutableArray();
				var title = string.Join(", ", component.Select(node => GetLocalName(node.Definition.Name)));
				sb.AppendLine(components.Length == 1 ? "### Layer Flow" : $"### Dependency Chain {chainNumber++}: {title}");
				sb.AppendLine();
				sb.AppendLine(componentEdges.Length == 0
					? "This boundary has no explicit named dependency chain. It may still participate through wildcard rules."
					: "These boundaries form one connected dependency chain in the configured rules.");
				sb.AppendLine();
				AppendMermaidDiagram(sb, component, componentEdges);
				AppendLayerTable(sb, config, FlattenLayerNames(component));
				AppendEdgeTable(sb, config, componentEdges);
			}
		}

		var wildcardEdges = config.Graph.DependencyEdges.Where(edge => !edge.IsExplicit).ToImmutableArray();
		if (wildcardEdges.Length > 0)
		{
			sb.AppendLine("### Universal Rules");
			sb.AppendLine();
			sb.AppendLine("These rules expand unconditionally across every layer and sub-layer. " +
			              "The `all layers` node represents any configured layer — a rule from or to `all layers` " +
			              "is automatically satisfied for every boundary in the architecture, including nested sub-layers.");
			sb.AppendLine();
			var wildcardLayerNames = new HashSet<string>(StringComparer.Ordinal);
			foreach (var edge in wildcardEdges)
			{
				if (edge.From != "*")
				{
					wildcardLayerNames.Add(GetRootName(edge.From));
				}

				if (edge.To != "*")
				{
					wildcardLayerNames.Add(GetRootName(edge.To));
				}
			}

			var relevantLayers = config.Layers.Where(l => wildcardLayerNames.Contains(l.Definition.Name)).ToImmutableArray();
			AppendMermaidDiagram(sb, relevantLayers, wildcardEdges);
			AppendEdgeTable(sb, config, wildcardEdges);
		}
	}

	private static void AppendMermaidDiagram(StringBuilder sb, ImmutableArray<LayerNode> layers, ImmutableArray<DependencyEdge> edges)
	{
		sb.AppendLine("```mermaid");
		sb.AppendLine("flowchart LR");

		var layerIds = BuildLayerIds(layers);
		var needsWildcard = edges.Any(edge => !edge.IsExplicit);
		foreach (var layer in layers)
		{
			AppendLayerNode(sb, layer, 1);
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

	private static void AppendLayerNode(StringBuilder sb, LayerNode node, int depth)
	{
		var indent = new string(' ', depth * 4);
		var localName = GetLocalName(node.Definition.Name);
		if (node.Children.Length == 0)
		{
			sb.AppendLine($"{indent}{LayerId(node.Definition.Name)}[\"{EscapeLabel(localName)}\"]");
			return;
		}

		sb.AppendLine($"{indent}subgraph {SubgraphId(node.Definition.Name)}[\"{EscapeLabel(localName)}\"]");
		sb.AppendLine($"{indent}    direction LR");
		foreach (var child in node.Children)
		{
			AppendLayerNode(sb, child, depth + 1);
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

	private static void AppendLayerTable(StringBuilder sb, AnalyzerConfig config, ImmutableArray<string> layers)
	{
		var rows = layers.Select(layer => (Layer: layer, Description: FindLayerDescription(config, layer))).Where(row => !string.IsNullOrWhiteSpace(row.Description)).ToImmutableArray();
		if (rows.Length == 0)
		{
			return;
		}

		sb.AppendLine("| Layer | Description |");
		sb.AppendLine("|-------|-------------|");
		foreach (var (layer, description) in rows)
		{
			sb.AppendLine($"| `{EscapeTable(layer)}` | {EscapeTable(description!)} |");
		}

		sb.AppendLine();
	}

	private static void AppendEdgeTable(StringBuilder sb, AnalyzerConfig config, ImmutableArray<DependencyEdge> edges)
	{
		if (edges.Length == 0)
		{
			return;
		}

		sb.AppendLine("| Rule | Gate | Dependency | Sites | Description |");
		sb.AppendLine("|------|------|------------|-------|-------------|");
		foreach (var edge in edges)
		{
			var siteText = GetEdgeTableSiteText(edge);
			var description = FindEdgeDescription(config, edge) ?? string.Empty;
			var gate = string.IsNullOrEmpty(edge.ScopePath) ? "root" : edge.ScopePath;
			sb.AppendLine($"| {(edge.IsBlocked ? "Blocked" : "Allowed")} | `{EscapeTable(gate)}` | `{EscapeTable(edge.From)} -> {EscapeTable(edge.To)}` | {EscapeTable(siteText)} | {EscapeTable(description)} |");
		}

		sb.AppendLine();
	}

	private static string GetMermaidEdgeLabel(DependencyEdge edge, string prefix)
	{
		var parts = ImmutableArray.CreateBuilder<string>();

		if (edge.SiteFilter.HasFilter)
		{
			parts.Add(edge.SiteFilter.ToDisplayText());
		}

		if (edge.AppliesToDescendants)
		{
			parts.Add("applies to descendants");
		}

		var suffix = string.Join("; ", parts);
		var result = string.IsNullOrEmpty(prefix) || suffix.Length == 0 ? prefix + suffix : prefix + ": " + suffix;

		return result;
	}

	private static string GetEdgeTableSiteText(DependencyEdge edge)
	{
		var siteText = edge.SiteFilter.HasFilter ? edge.SiteFilter.ToDisplayText() : "all sites";
		var result = edge.AppliesToDescendants ? siteText + "; applies to descendants" : siteText;

		return result;
	}

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
}
