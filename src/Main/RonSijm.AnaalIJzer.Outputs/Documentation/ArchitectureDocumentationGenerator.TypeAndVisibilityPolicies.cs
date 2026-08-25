using System.Text;
using RonSijm.AnaalIJzer.Model;

namespace RonSijm.AnaalIJzer.Documentation;

internal static partial class ArchitectureDocumentationMarkdownBuilder
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

	private static void AppendVisibilityPolicies(StringBuilder sb, AnalyzerConfig config)
	{
		var policies = config.Documentation.Items.Where(item => item.Kind == "VisibilityPolicy").ToArray();
		if (policies.Length == 0)
		{
			return;
		}

		sb.AppendLine("## Visibility Policies");
		sb.AppendLine();
		sb.AppendLine("| Scope | Targets | Mode | Accessibilities | Description |");
		sb.AppendLine("|-------|---------|------|-----------------|-------------|");
		foreach (var policy in policies)
		{
			var allowed = policy.GetAttribute("allowedAccessibilities");
			var blocked = policy.GetAttribute("blockedAccessibilities");
			var mode = allowed is not null ? "Allow only" : "Block";
			var accessibilities = allowed ?? blocked ?? string.Empty;
			sb.AppendLine($"| `{EscapeTable(policy.LayerPath)}` | {EscapeTable(policy.GetAttribute("targets") ?? string.Empty)} | {mode} | {EscapeTable(accessibilities)} | {EscapeTable(policy.Description ?? string.Empty)} |");
		}

		sb.AppendLine();
	}
}
