using System.Collections.Immutable;
using System.Text;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model;

namespace RonSijm.AnaalIJzer.Outputs.Documentation;

internal static partial class ArchitectureDocumentationMarkdownBuilder
{
    private static void AppendSolutionTopology(StringBuilder sb, AnalyzerConfig config)
    {
        var items = config.Documentation.Items;
        var topologyIndex = FindSolutionTopologyIndex(items);
        if (topologyIndex < 0)
        {
            return;
        }

        var topology = items[topologyIndex];
        sb.AppendLine("## Solution Topology");
        sb.AppendLine();
        sb.AppendLine($"`requireRecognizedProjects`: `{EscapeTable(topology.GetAttribute("requireRecognizedProjects") ?? "false")}`  ");
        sb.AppendLine($"`enforceAcyclic`: `{EscapeTable(topology.GetAttribute("enforceAcyclic") ?? "false")}`");
        if (!string.IsNullOrWhiteSpace(topology.Description))
        {
            sb.AppendLine();
            sb.AppendLine(EscapeMarkdown(topology.Description!));
        }

        var modules = GetDirectChildItems(items, topologyIndex, "Module");
        if (modules.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Modules");
            sb.AppendLine();
            sb.AppendLine("| Module | Project matchers | Description |");
            sb.AppendLine("|--------|------------------|-------------|");
            foreach (var moduleIndex in modules)
            {
                var module = items[moduleIndex];
                var projectMatchers = GetDirectChildItems(items, moduleIndex, "Project")
                    .Select(index => items[index].Label);
                sb.AppendLine($"| `{EscapeTable(module.Label)}` | {EscapeTable(string.Join(", ", projectMatchers))} | {EscapeTable(module.Description ?? string.Empty)} |");
            }

            sb.AppendLine();
        }

        var rules = GetDirectChildItems(items, topologyIndex, "AllowedModuleReference", "BlockedModuleReference");
        if (rules.Length == 0)
        {
            return;
        }

        sb.AppendLine("### Module Reference Rules");
        sb.AppendLine();
        sb.AppendLine("| Rule | Edge | Description |");
        sb.AppendLine("|------|------|-------------|");
        foreach (var ruleIndex in rules)
        {
            var rule = items[ruleIndex];
            var mode = rule.Kind == "AllowedModuleReference" ? "Allowed" : "Blocked";
            sb.AppendLine($"| {mode} | `{EscapeTable(rule.Label)}` | {EscapeTable(rule.Description ?? string.Empty)} |");
        }

        sb.AppendLine();
    }

    private static int FindSolutionTopologyIndex(IReadOnlyList<ArchitectureDocumentationItem> items)
    {
        for (var index = 0; index < items.Count; index++)
        {
            if (items[index].Kind == "SolutionTopology")
            {
                return index;
            }
        }

        return -1;
    }

    private static ImmutableArray<int> GetDirectChildItems(IReadOnlyList<ArchitectureDocumentationItem> items, int parentIndex, params string[] kinds)
    {
        var parent = items[parentIndex];
        var matches = ImmutableArray.CreateBuilder<int>();
        for (var index = parentIndex + 1; index < items.Count && items[index].Depth > parent.Depth; index++)
        {
            var item = items[index];
            if (item.Depth == parent.Depth + 1 && kinds.Contains(item.Kind, StringComparer.Ordinal))
            {
                matches.Add(index);
            }
        }

        var result = matches.ToImmutable();

        return result;
    }
}