using System.Text;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model;

namespace RonSijm.AnaalIJzer.Outputs.Documentation;

internal static partial class ArchitectureDocumentationMarkdownBuilder
{
	private static void AppendReturnValuePolicies(StringBuilder sb, AnalyzerConfig config)
	{
		var policies = config.Documentation.Items.Where(item => item.Kind == "ReturnValuePolicy").ToArray();
		if (policies.Length == 0)
		{
			return;
		}

		sb.AppendLine("## Return-Value Policies");
		sb.AppendLine();
		sb.AppendLine("| Scope | Forbidden direct return | Description |");
		sb.AppendLine("|-------|-------------------------|-------------|");
		for (var policyIndex = 0; policyIndex < config.Documentation.Items.Length; policyIndex++)
		{
			var policy = config.Documentation.Items[policyIndex];
			if (policy.Kind != "ReturnValuePolicy")
			{
				continue;
			}

			var rules = config.Documentation.Items
				.Skip(policyIndex + 1)
				.TakeWhile(item => item.Depth > policy.Depth)
				.Where(item => item.Depth == policy.Depth + 1 && item.Kind is "Literal" or "Invocation" or "New" or "Identifier" or "MemberAccess")
				.ToArray();
			if (rules.Length == 0)
			{
				sb.AppendLine($"| `{EscapeTable(policy.LayerPath)}` | (no return matchers) | {EscapeTable(policy.Description ?? string.Empty)} |");
				continue;
			}

			foreach (var rule in rules)
			{
				var description = rule.Description ?? policy.Description ?? string.Empty;
				sb.AppendLine($"| `{EscapeTable(policy.LayerPath)}` | {EscapeTable(FormatReturnValueRule(rule))} | {EscapeTable(description)} |");
			}
		}

		sb.AppendLine();
	}

	private static string FormatReturnValueRule(ArchitectureDocumentationItem rule)
	{
		var attributes = string.Join(" ", rule.Attributes.Select(attribute => attribute.Name + "=\"" + attribute.Value + "\""));
		var result = string.IsNullOrWhiteSpace(attributes)
			? rule.Kind
			: rule.Kind + " " + attributes;

		return result;
	}
}
