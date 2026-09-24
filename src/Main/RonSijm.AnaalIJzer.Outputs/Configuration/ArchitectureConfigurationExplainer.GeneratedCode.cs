using System.Text;
using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.Outputs.Configuration;

internal static partial class ArchitectureConfigurationExplainer
{
    private static void AppendGeneratedCodeScope(StringBuilder sb, XElement element, int depth)
    {
        var mode = element.Attribute("mode")?.Value ?? "(missing mode)";
        var maximumDocumentLength = element.Attribute("maximumDocumentLength")?.Value ?? "262144";
        AppendLine(sb, depth, "- Generated-code analysis uses `" + Escape(mode) + "` mode with a maximum document length of `" + Escape(maximumDocumentLength) + "` characters.");
        AppendDescription(sb, element, depth + 1);
        foreach (var path in element.Elements("Path"))
        {
            AppendLine(sb, depth + 1, "- Includes generated paths matching " + FormatMatcher(path) + ".");
            AppendDescription(sb, path, depth + 2);
        }
    }
}