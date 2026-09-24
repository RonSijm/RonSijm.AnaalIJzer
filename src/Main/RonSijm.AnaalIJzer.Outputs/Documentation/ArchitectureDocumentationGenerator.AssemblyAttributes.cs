using System.Text;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model;

namespace RonSijm.AnaalIJzer.Outputs.Documentation;

internal static partial class ArchitectureDocumentationMarkdownBuilder
{
    private static void AppendAssemblyAttributePolicies(StringBuilder sb, AnalyzerConfig config)
    {
        var policyIndices = Enumerable.Range(0, config.Documentation.Items.Length)
            .Where(index => config.Documentation.Items[index].Kind == "AssemblyAttributePolicy")
            .ToArray();
        if (policyIndices.Length == 0)
        {
            return;
        }

        sb.AppendLine("## Assembly Attribute Policies");
        sb.AppendLine();
        sb.AppendLine("These policies inspect semantic assembly attributes after source and SDK generation. They do not need to know whether an attribute originated in C# or in the project file.");
        sb.AppendLine();
        sb.AppendLine("| Policy | Mode | Attribute matcher | Required arguments | Description |");
        sb.AppendLine("|--------|------|-------------------|--------------------|-------------|");
        foreach (var policyIndex in policyIndices)
        {
            var policy = config.Documentation.Items[policyIndex];
            foreach (var containerIndex in GetDirectChildIndices(config.Documentation.Items, policyIndex))
            {
                var container = config.Documentation.Items[containerIndex];
                if (container.Kind is not ("Allowed" or "Forbidden"))
                {
                    continue;
                }

                foreach (var attributeIndex in GetDirectChildIndices(config.Documentation.Items, containerIndex))
                {
                    var attribute = config.Documentation.Items[attributeIndex];
                    if (attribute.Kind != "Attribute")
                    {
                        continue;
                    }

                    var arguments = GetDirectChildIndices(config.Documentation.Items, attributeIndex)
                        .Select(index => config.Documentation.Items[index])
                        .Where(item => item.Kind == "Argument")
                        .Select(FormatAssemblyAttributeArgument)
                        .ToArray();
                    var description = attribute.Description ?? container.Description ?? policy.Description ?? string.Empty;
                    sb.AppendLine($"| `{EscapeTable(policy.Label)}` | {EscapeTable(container.Kind)} | `{EscapeTable(attribute.Label)}` | {EscapeTable(arguments.Length == 0 ? "(any arguments)" : string.Join(" AND ", arguments))} | {EscapeTable(description)} |");
                }
            }
        }

        sb.AppendLine();
    }

    private static string FormatAssemblyAttributeArgument(ArchitectureDocumentationItem argument)
    {
        var selector = argument.GetAttribute("index") is { } index
            ? "#" + index
            : argument.GetAttribute("name") is { } name
                ? name
                : "?";
        var result = selector + ": " + argument.Label;

        return result;
    }
}