using System.Collections.Immutable;
using System.Text;
using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model;

namespace RonSijm.AnaalIJzer.Outputs.Documentation;

internal static partial class ArchitectureDocumentationMarkdownBuilder
{
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
}
