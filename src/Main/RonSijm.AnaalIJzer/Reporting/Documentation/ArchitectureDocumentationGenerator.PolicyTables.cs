using System.Text;
using RonSijm.AnaalIJzer.DependencyRules;
using RonSijm.AnaalIJzer.Model;

namespace RonSijm.AnaalIJzer.Documentation;

internal static partial class ArchitectureDocumentationGenerator
{
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

	private static string? FindLayerDescription(AnalyzerConfig config, string layerName)
    {
        return config.Documentation.Items.FirstOrDefault(item => item.Kind == "Layer" && item.LayerPath == layerName)
            .Description;
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
