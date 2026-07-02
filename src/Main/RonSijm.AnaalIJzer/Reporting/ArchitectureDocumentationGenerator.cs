using System.Collections.Immutable;
using System.Text;
using RonSijm.AnaalIJzer.Config;
using RonSijm.AnaalIJzer.Diagnostics;
using RonSijm.AnaalIJzer.Matching;

namespace RonSijm.AnaalIJzer.Reporting;

/// <summary>
///     Renders the configured architecture as documentation: Mermaid diagrams plus the
///     descriptive XML story in authored order.
/// </summary>
internal static class ArchitectureDocumentationGenerator
{
	private const string WildcardNodeId = "Any";

	internal static string GenerateMarkdown(AnalyzerConfig config, string? assemblyName)
	{
		var sb = new StringBuilder();

		sb.AppendLine("# Architecture Documentation");
		sb.AppendLine();
		if (assemblyName is not null)
		{
			sb.AppendLine($"**Assembly**: `{assemblyName}`  ");
		}

		sb.AppendLine($"**Generated**: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
		sb.AppendLine();

		var description = config.Documentation.Description;
		if (!string.IsNullOrWhiteSpace(description))
		{
			sb.AppendLine(EscapeMarkdown(description!));
			sb.AppendLine();
		}

		AppendDependencyDiagrams(sb, config);
		AppendTypePolicies(sb, config);
		AppendConfigurationOrder(sb, config);
		return sb.ToString();
	}

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
			sb.AppendLine("### Wildcard Rules");
			sb.AppendLine();
			sb.AppendLine("Wildcard rules apply across layer boundaries that are not fully named by the edge.");
			sb.AppendLine();
			AppendMermaidDiagram(sb, config.Layers, wildcardEdges);
			AppendEdgeTable(sb, config, wildcardEdges);
		}
	}

	private static void AppendMermaidDiagram(StringBuilder sb, ImmutableArray<LayerNode> layers, ImmutableArray<DependencyEdge> edges)
	{
		sb.AppendLine("```mermaid");
		sb.AppendLine("flowchart LR");

		var needsWildcard = edges.Any(edge => !edge.IsExplicit);
		foreach (var layer in layers)
		{
			AppendLayerNode(sb, layer, 1);
		}

		if (needsWildcard)
		{
			sb.AppendLine($"    {WildcardNodeId}([\"* (any layer)\"])");
		}

		foreach (var edge in edges)
		{
			var fromId = edge.From == "*" ? WildcardNodeId : LayerId(edge.From);
			var toId = edge.To == "*" ? WildcardNodeId : LayerId(edge.To);
			if (edge.IsBlocked)
			{
				var blockText = edge.SiteFilter.HasFilter ? $"blocked: {edge.SiteFilter.ToDisplayText()}" : "blocked";
				sb.AppendLine($"    {fromId} -. \"{EscapeLabel(blockText)}\" .-> {toId}");
			}
			else
			{
				var arrow = edge.SiteFilter.HasFilter ? $"-->|\"{EscapeLabel(edge.SiteFilter.ToDisplayText())}\"| " : "--> ";
				sb.AppendLine($"    {fromId} {arrow}{toId}");
			}
		}

		foreach (var name in FlattenLayerNames(layers))
		{
			sb.AppendLine($"    style {LayerId(name)} fill:#cce5ff,stroke:#0066cc,color:#000");
		}

		if (needsWildcard)
		{
			sb.AppendLine($"    style {WildcardNodeId} fill:#fff4cc,stroke:#cc9900,color:#000");
		}

		sb.AppendLine("```");
		sb.AppendLine();
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

		sb.AppendLine($"{indent}subgraph SG_{Sanitize(node.Definition.Name)}[\"{EscapeLabel(localName)}\"]");
		sb.AppendLine($"{indent}    direction LR");
		sb.AppendLine($"{indent}    {LayerId(node.Definition.Name)}[\"{EscapeLabel(localName)} (boundary)\"]");
		foreach (var child in node.Children)
		{
			AppendLayerNode(sb, child, depth + 1);
		}
		sb.AppendLine($"{indent}end");
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
			var siteText = edge.SiteFilter.HasFilter ? edge.SiteFilter.ToDisplayText() : "all sites";
			var description = FindEdgeDescription(config, edge) ?? string.Empty;
			var gate = string.IsNullOrEmpty(edge.ScopePath) ? "root" : edge.ScopePath;
			sb.AppendLine($"| {(edge.IsBlocked ? "Blocked" : "Allowed")} | `{EscapeTable(gate)}` | `{EscapeTable(edge.From)} -> {EscapeTable(edge.To)}` | {EscapeTable(siteText)} | {EscapeTable(description)} |");
		}

		sb.AppendLine();
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

	private static void AppendTypePolicies(StringBuilder sb, AnalyzerConfig config)
	{
		if (!config.Documentation.Items.Any(item => item.Kind is "Allowed" or "Forbidden"))
		{
			return;
		}

		sb.AppendLine("## Type Policies");
		sb.AppendLine();
		sb.AppendLine("| Policy | Scope | Matcher | Description |");
		sb.AppendLine("|--------|-------|---------|-------------|");
		for (var policyIndex = 0; policyIndex < config.Documentation.Items.Length; policyIndex++)
		{
			var policy = config.Documentation.Items[policyIndex];
			if (policy.Kind is not ("Allowed" or "Forbidden"))
			{
				continue;
			}

			var scope = string.IsNullOrEmpty(policy.LayerPath) ? "global" : policy.LayerPath;
			foreach (var matcher in config.Documentation.Items.Skip(policyIndex + 1).TakeWhile(item => item.Depth > policy.Depth).Where(item => item.Depth == policy.Depth + 1 && item.Kind is "Class" or "Namespace"))
			{
				var description = matcher.Comment ?? matcher.Description ?? policy.Description ?? string.Empty;
				sb.AppendLine($"| {EscapeTable(policy.Kind)} | `{EscapeTable(scope)}` | `{EscapeTable(matcher.Label)}` | {EscapeTable(description)} |");
			}
		}

		sb.AppendLine();
	}

	private static void AppendConfigurationOrder(StringBuilder sb, AnalyzerConfig config)
	{
		if (config.Documentation.Items.Length == 0)
		{
			return;
		}

		sb.AppendLine("## Rules In Configuration Order");
		sb.AppendLine();

		foreach (var item in config.Documentation.Items)
		{
			var indent = new string(' ', item.Depth * 2);
			sb.AppendLine($"{indent}- **{EscapeMarkdown(item.Kind)}** `{EscapeMarkdown(item.Label)}`");

			var description = item.Description;
			if (!string.IsNullOrWhiteSpace(description))
			{
				sb.AppendLine($"{indent}  {EscapeMarkdown(description!)}");
			}

			var comment = item.Comment;
			if (!string.IsNullOrWhiteSpace(comment))
			{
				sb.AppendLine($"{indent}  Diagnostic note: {EscapeMarkdown(comment!)}");
			}

			var details = FormatAttributes(item.Attributes);
			if (!string.IsNullOrWhiteSpace(details))
			{
				sb.AppendLine($"{indent}  `{details}`");
			}
		}

		sb.AppendLine();
	}

	private static string? FindLayerDescription(AnalyzerConfig config, string layerName) =>
		config.Documentation.Items.FirstOrDefault(item => item.Kind == "Layer" && item.LayerPath == layerName).Description;

	private static string? FindEdgeDescription(AnalyzerConfig config, DependencyEdge edge)
	{
		foreach (var item in config.Documentation.Items)
		{
			if (item.Kind == (edge.IsBlocked ? "BlockedDependency" : "AllowedDependency")
			    && item.LayerPath == edge.ScopePath
			    && item.GetAttribute("from") == edge.ConfiguredFrom
			    && item.GetAttribute("to") == edge.ConfiguredTo
			    && SiteAttributesMatch(item, edge))
			{
				return item.Description;
			}
		}

		return null;
	}

	private static bool SiteAttributesMatch(ArchitectureDocumentationItem item, DependencyEdge edge)
	{
		var allowedSites = item.GetAttribute("allowedSites");
		var blockedSites = item.GetAttribute("blockedSites");
		if (!edge.SiteFilter.HasFilter)
		{
			return allowedSites is null && blockedSites is null;
		}

		return allowedSites is not null && SitesMatch(allowedSites, edge.SiteFilter.AllowedSites)
		       || blockedSites is not null && SitesMatch(blockedSites, edge.SiteFilter.BlockedSites);
	}

	private static bool SitesMatch(string text, ImmutableHashSet<string> sites)
	{
		var parsedSites = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
		foreach (var rawToken in text.Split(','))
		{
			var token = rawToken.Trim();
			if (token.Length == 0)
			{
				continue;
			}

			if (!DependencySites.TryNormalize(token, out var normalized))
			{
				return false;
			}

			parsedSites.Add(normalized);
		}

		return parsedSites.SetEquals(sites);
	}

	private static string FormatAttributes(ImmutableArray<ArchitectureDocumentationAttribute> attributes)
	{
		if (attributes.Length == 0)
		{
			return string.Empty;
		}

		return string.Join(" ", attributes.Select(attribute => $"{attribute.Name}=\"{attribute.Value}\""));
	}

	private static string LayerId(string name) => "L_" + Sanitize(name);

	private static string GetRootName(string path)
	{
		var separator = path.IndexOf('/');
		return separator < 0 ? path : path.Substring(0, separator);
	}

	private static string GetLocalName(string path)
	{
		var separator = path.LastIndexOf('/');
		return separator < 0 ? path : path.Substring(separator + 1);
	}

	private static string Sanitize(string name)
	{
		var sb = new StringBuilder(name.Length);
		foreach (var c in name)
		{
			sb.Append(IsIdChar(c) ? c : '_');
		}

		return sb.ToString();
	}

	private static bool IsIdChar(char c) =>
		c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_';

	private static string EscapeLabel(string text) =>
		text.Replace("\"", "&quot;").Replace("|", "&#124;");

	private static string EscapeTable(string text) =>
		text.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

	private static string EscapeMarkdown(string text) =>
		text.Replace("\r", " ").Replace("\n", " ");
}
