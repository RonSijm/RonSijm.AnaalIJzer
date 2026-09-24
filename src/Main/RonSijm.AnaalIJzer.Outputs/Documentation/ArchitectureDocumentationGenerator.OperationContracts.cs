using System.Collections.Immutable;
using System.Text;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model;

namespace RonSijm.AnaalIJzer.Outputs.Documentation;

internal static partial class ArchitectureDocumentationMarkdownBuilder
{
    private static void AppendOperationContracts(StringBuilder sb, AnalyzerConfig config)
    {
        var containers = config.Documentation.Items.Where(item => item.Kind == "Operations").ToArray();
        if (containers.Length == 0)
        {
            return;
        }

        sb.AppendLine("## Operation Contracts");
        sb.AppendLine();
        sb.AppendLine("Each operation is explicitly named by the configuration author. The compiler checks local declaration shape and direct calls; workspace inspection checks that exactly one owner exists in the inspected scope.");
        sb.AppendLine();
        sb.AppendLine("| Operation | Owner | Entry points | Request | Response | Owner layers | Entry-point layers | Description |");
        sb.AppendLine("|-----------|-------|--------------|---------|----------|--------------|--------------------|-------------|");
        for (var containerIndex = 0; containerIndex < config.Documentation.Items.Length; containerIndex++)
        {
            if (config.Documentation.Items[containerIndex].Kind != "Operations")
            {
                continue;
            }

            var container = config.Documentation.Items[containerIndex];
            foreach (var operationIndex in GetDirectChildIndices(config.Documentation.Items, containerIndex).Where(index => config.Documentation.Items[index].Kind == "Operation"))
            {
                var operation = config.Documentation.Items[operationIndex];
                var owner = FormatOperationContractDeclaration(config.Documentation.Items, FindDirectChildIndex(config.Documentation.Items, operationIndex, "Owner"));
                var entryPoints = GetDirectChildIndices(config.Documentation.Items, operationIndex)
                    .Where(index => config.Documentation.Items[index].Kind == "EntryPoint")
                    .Select(index => FormatOperationContractDeclaration(config.Documentation.Items, index))
                    .ToArray();
                var request = FormatOperationContractType(config.Documentation.Items, FindDirectChildIndex(config.Documentation.Items, operationIndex, "Request"));
                var response = FormatOperationContractType(config.Documentation.Items, FindDirectChildIndex(config.Documentation.Items, operationIndex, "Response"));
                var description = operation.Description ?? container.Description ?? string.Empty;
                sb.AppendLine($"| `{EscapeTable(operation.GetAttribute("name") ?? operation.Label)}` | {EscapeTable(owner)} | {EscapeTable(entryPoints.Length == 0 ? "(none configured)" : string.Join(" OR ", entryPoints))} | {EscapeTable(request)} | {EscapeTable(response)} | {EscapeTable(operation.GetAttribute("allowedOwnerLayers") ?? "(any layer)")} | {EscapeTable(operation.GetAttribute("allowedEntryPointLayers") ?? "(any layer)")} | {EscapeTable(description)} |");
            }
        }

        sb.AppendLine();
    }

    private static int FindDirectChildIndex(ImmutableArray<ArchitectureDocumentationItem> items, int parentIndex, string kind)
    {
        var result = GetDirectChildIndices(items, parentIndex)
            .Where(index => items[index].Kind == kind)
            .DefaultIfEmpty(-1)
            .First();

        return result;
    }

    private static string FormatOperationContractDeclaration(ImmutableArray<ArchitectureDocumentationItem> items, int containerIndex)
    {
        if (containerIndex < 0)
        {
            return "(none configured)";
        }

        var declarationIndex = FindDirectChildIndex(items, containerIndex, "DeclarationMatcher");
        var result = declarationIndex < 0
            ? "(no declaration selector)"
            : FormatBehavioralDeclaration(items, declarationIndex);

        return result;
    }

    private static string FormatOperationContractType(ImmutableArray<ArchitectureDocumentationItem> items, int containerIndex)
    {
        if (containerIndex < 0)
        {
            return "(not constrained)";
        }

        var type = GetDirectChildIndices(items, containerIndex)
            .Select(index => items[index])
            .Where(item => item.Kind == "Class")
            .Select(item => item.Label)
            .DefaultIfEmpty("(no type selector)")
            .First();

        return type;
    }
}