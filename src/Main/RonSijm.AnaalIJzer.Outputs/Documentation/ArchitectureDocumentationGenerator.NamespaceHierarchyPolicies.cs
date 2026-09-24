using System.Text;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model;

namespace RonSijm.AnaalIJzer.Outputs.Documentation;

internal static partial class ArchitectureDocumentationMarkdownBuilder
{
    private static void AppendNamespaceHierarchyPolicies(StringBuilder sb, AnalyzerConfig config)
    {
        var policyIndices = Enumerable.Range(0, config.Documentation.Items.Length)
            .Where(index => config.Documentation.Items[index].Kind == "NamespaceHierarchyPolicy")
            .ToArray();
        if (policyIndices.Length == 0)
        {
            return;
        }

        sb.AppendLine("## Namespace Hierarchy Policies");
        sb.AppendLine();
        sb.AppendLine("These root-level policies protect namespace ownership independently of layer membership. A blocked relationship rejects semantic references only; `using` directives alone do not create a violation.");
        sb.AppendLine();
        sb.AppendLine("| Root namespace | Blocked relationship | Site scope | Description |");
        sb.AppendLine("|----------------|----------------------|------------|-------------|");
        foreach (var policyIndex in policyIndices)
        {
            var policy = config.Documentation.Items[policyIndex];
            var rootNamespace = policy.GetAttribute("rootNamespace") ?? "(missing root namespace)";
            var rules = GetDirectChildIndices(config.Documentation.Items, policyIndex)
                .Select(index => config.Documentation.Items[index])
                .Where(item => item.Kind == "BlockedRelation")
                .ToArray();
            foreach (var rule in rules)
            {
                var relation = rule.GetAttribute("relation") ?? "(missing relation)";
                var siteScope = FormatNamespaceHierarchySiteScope(rule);
                var description = rule.Description ?? policy.Description ?? string.Empty;
                sb.AppendLine($"| `{EscapeTable(rootNamespace)}` | `{EscapeTable(relation)}` | {EscapeTable(siteScope)} | {EscapeTable(description)} |");
            }
        }

        sb.AppendLine();
    }

    private static string FormatNamespaceHierarchySiteScope(ArchitectureDocumentationItem rule)
    {
        var allowedSites = rule.GetAttribute("allowedSites");
        if (!string.IsNullOrWhiteSpace(allowedSites))
        {
            var allowedResult = "Only " + allowedSites;

            return allowedResult;
        }

        var blockedSites = rule.GetAttribute("blockedSites");
        var result = string.IsNullOrWhiteSpace(blockedSites) ? "All sites" : "All except " + blockedSites;

        return result;
    }
}