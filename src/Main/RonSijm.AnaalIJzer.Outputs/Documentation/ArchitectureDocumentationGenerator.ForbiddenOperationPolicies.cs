using System.Collections.Immutable;
using System.Text;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model;

namespace RonSijm.AnaalIJzer.Outputs.Documentation;

internal static partial class ArchitectureDocumentationMarkdownBuilder
{
    private static void AppendForbiddenOperationPolicies(StringBuilder sb, AnalyzerConfig config)
    {
        var policies = config.Documentation.Items.Where(item => item.Kind == "ForbiddenOperations").ToArray();
        if (policies.Length == 0)
        {
            return;
        }

        sb.AppendLine("## Forbidden Operation Policies");
        sb.AppendLine();
        sb.AppendLine("These policies reject selected resolved members without forbidding every use of their containing type.");
        sb.AppendLine();
        sb.AppendLine("| Scope | Selected operation | Sites | Description |");
        sb.AppendLine("|-------|--------------------|-------|-------------|");
        for (var policyIndex = 0; policyIndex < config.Documentation.Items.Length; policyIndex++)
        {
            var policy = config.Documentation.Items[policyIndex];
            if (policy.Kind != "ForbiddenOperations")
            {
                continue;
            }

            var rules = GetDirectChildIndices(config.Documentation.Items, policyIndex)
                .Where(index => config.Documentation.Items[index].Kind == "ForbiddenOperation")
                .ToArray();
            if (rules.Length == 0)
            {
                sb.AppendLine($"| `{EscapeTable(policy.LayerPath)}` | (no selected operations) | (all sites) | {EscapeTable(policy.Description ?? string.Empty)} |");
                continue;
            }

            foreach (var ruleIndex in rules)
            {
                var rule = config.Documentation.Items[ruleIndex];
                var matchers = GetDirectChildIndices(config.Documentation.Items, ruleIndex)
                    .Where(index => config.Documentation.Items[index].Kind == "OperationMatcher")
                    .ToArray();
                var operation = matchers.Length == 0
                    ? "(no operation matchers)"
                    : string.Join(" OR ", matchers.Select(matcherIndex => FormatOperationMatcher(
                        config.Documentation.Items[matcherIndex],
                        GetDirectChildIndices(config.Documentation.Items, matcherIndex)
                            .Select(index => config.Documentation.Items[index]))));
                var sites = FormatOperationSites(rule);
                var description = rule.Description ?? policy.Description ?? string.Empty;
                sb.AppendLine($"| `{EscapeTable(policy.LayerPath)}` | {EscapeTable(operation)} | {EscapeTable(sites)} | {EscapeTable(description)} |");
            }
        }

        sb.AppendLine();
    }

    private static IEnumerable<int> GetDirectChildIndices(ImmutableArray<ArchitectureDocumentationItem> items, int parentIndex)
    {
        var parent = items[parentIndex];
        for (var index = parentIndex + 1; index < items.Length; index++)
        {
            var candidate = items[index];
            if (candidate.Depth <= parent.Depth)
            {
                break;
            }

            if (candidate.Depth == parent.Depth + 1)
            {
                yield return index;
            }
        }
    }

    private static string FormatOperationMatcher(ArchitectureDocumentationItem matcher, IEnumerable<ArchitectureDocumentationItem> children)
    {
        var attributes = FormatOperationMatcherAttributes(matcher);
        var selectedMembers = children
            .Where(child => child.Kind is "ContainingType" or "Member")
            .Select(child => child.Kind + " " + FormatOperationMatcherAttributes(child))
            .ToArray();
        var selectorText = selectedMembers.Length == 0 ? string.Empty : " [" + string.Join("; ", selectedMembers) + "]";
        var result = (string.IsNullOrWhiteSpace(attributes) ? "OperationMatcher" : "OperationMatcher " + attributes) + selectorText;

        return result;
    }

    private static string FormatOperationMatcherAttributes(ArchitectureDocumentationItem item)
    {
        var result = string.Join(" ", item.Attributes
            .Where(attribute => attribute.Name is not ("description" or "comment"))
            .Select(attribute => attribute.Name + "=\"" + attribute.Value + "\""));

        return result;
    }

    private static string FormatOperationSites(ArchitectureDocumentationItem rule)
    {
        var allowedSites = rule.Attributes.FirstOrDefault(attribute => attribute.Name == "allowedSites").Value;
        if (!string.IsNullOrWhiteSpace(allowedSites))
        {
            return "only " + allowedSites;
        }

        var blockedSites = rule.Attributes.FirstOrDefault(attribute => attribute.Name == "blockedSites").Value;
        var result = string.IsNullOrWhiteSpace(blockedSites) ? "all sites" : "all except " + blockedSites;

        return result;
    }
}