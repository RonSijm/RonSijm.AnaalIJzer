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
        sb.AppendLine("| Scope | Return rule | Description |");
        sb.AppendLine("|-------|-------------|-------------|");
        for (var policyIndex = 0; policyIndex < config.Documentation.Items.Length; policyIndex++)
        {
            var policy = config.Documentation.Items[policyIndex];
            if (policy.Kind != "ReturnValuePolicy")
            {
                continue;
            }

            var scope = string.IsNullOrWhiteSpace(policy.LayerPath) ? "Global configuration" : policy.LayerPath;

            var rules = config.Documentation.Items
                .Select((item, index) => (Item: item, Index: index))
                .Skip(policyIndex + 1)
                .TakeWhile(item => item.Item.Depth > policy.Depth)
                .Where(item => item.Item.Depth == policy.Depth + 1 && IsReturnValueRule(item.Item))
                .ToArray();
            if (rules.Length == 0)
            {
                sb.AppendLine($"| `{EscapeTable(scope)}` | (no return matchers) | {EscapeTable(policy.Description ?? string.Empty)} |");
                continue;
            }

            foreach (var rule in rules)
            {
                if (rule.Item.Kind is "Literal" or "Invocation" or "New" or "Identifier" or "MemberAccess")
                {
                    var ruleDescription = rule.Item.Description ?? policy.Description ?? string.Empty;
                    sb.AppendLine($"| `{EscapeTable(scope)}` | Forbids {EscapeTable(FormatReturnValueRule(rule.Item))} | {EscapeTable(ruleDescription)} |");
                    continue;
                }

                var allowedRules = config.Documentation.Items
                    .Skip(rule.Index + 1)
                    .TakeWhile(item => item.Depth > rule.Item.Depth)
                    .Where(item => item.Depth == rule.Item.Depth + 1 && item.Kind is "Literal" or "Invocation" or "New" or "Identifier" or "MemberAccess")
                    .ToArray();
                var allowedRuleDescription = rule.Item.Description ?? policy.Description ?? string.Empty;
                var ruleText = allowedRules.Length == 0
                    ? "Allows only configured direct return expressions"
                    : "Allows only " + string.Join(" or ", allowedRules.Select(FormatReturnValueRule));
                sb.AppendLine($"| `{EscapeTable(scope)}` | {EscapeTable(ruleText)} | {EscapeTable(allowedRuleDescription)} |");
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

    private static bool IsReturnValueRule(ArchitectureDocumentationItem item)
    {
        var result = item.Kind is "Literal" or "Invocation" or "New" or "Identifier" or "MemberAccess" or "AllowedReturn";

        return result;
    }
}