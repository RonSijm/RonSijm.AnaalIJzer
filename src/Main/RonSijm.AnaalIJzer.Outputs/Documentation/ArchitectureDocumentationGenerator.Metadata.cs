using System.Text;
using RonSijm.AnaalIJzer.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Model;

namespace RonSijm.AnaalIJzer.Documentation;

internal static partial class ArchitectureDocumentationMarkdownBuilder
{
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

	private static string? FindLayerDescription(AnalyzerConfig config, string layerName)
	{
		var result = config.Documentation.Items.FirstOrDefault(item => item.Kind == "Layer" && item.LayerPath == layerName)
			.Description;

		return result;
	}

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
}
