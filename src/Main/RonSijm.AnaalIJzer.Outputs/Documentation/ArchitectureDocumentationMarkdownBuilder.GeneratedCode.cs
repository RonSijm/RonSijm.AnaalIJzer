using System.Text;
using RonSijm.AnaalIJzer.Core.Observations;
using RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model;

namespace RonSijm.AnaalIJzer.Outputs.Documentation;

internal static partial class ArchitectureDocumentationMarkdownBuilder
{
	private static void AppendGeneratedCodeScope(StringBuilder sb, AnalyzerConfig config)
	{
		var scopeIndex = -1;
		for (var index = 0; index < config.Documentation.Items.Length; index++)
		{
			if (config.Documentation.Items[index].Kind == "GeneratedCode")
			{
				scopeIndex = index;
				break;
			}
		}

		if (scopeIndex < 0)
		{
			return;
		}

		var scope = config.Documentation.Items[scopeIndex];
		var mode = scope.GetAttribute("mode") ?? "Exclude";
		var maximumDocumentLength = scope.GetAttribute("maximumDocumentLength") ?? GeneratedCodeAnalysisScope.DefaultMaximumDocumentLength.ToString(System.Globalization.CultureInfo.InvariantCulture);
		var paths = GetDirectChildIndices(config.Documentation.Items, scopeIndex)
			.Where(index => config.Documentation.Items[index].Kind == "Path")
			.Select(index => FormatAttributes(config.Documentation.Items[index].Attributes))
			.ToArray();

		sb.AppendLine("## Generated Code Analysis");
		sb.AppendLine();
		sb.AppendLine("Generated source remains excluded unless this explicit scope opts into it. Ordinary source files are always analyzed.");
		sb.AppendLine();
		sb.AppendLine($"- `mode`: `{EscapeMarkdown(mode)}`");
		sb.AppendLine($"- `maximumDocumentLength`: `{EscapeMarkdown(maximumDocumentLength)}`");
		if (paths.Length == 0)
		{
			sb.AppendLine("- configured paths: none");
		}
		else
		{
			foreach (var path in paths)
			{
				sb.AppendLine($"- configured path: `{EscapeMarkdown(path)}`");
			}
		}

		if (!string.IsNullOrWhiteSpace(scope.Description))
		{
			sb.AppendLine($"- description: {EscapeMarkdown(scope.Description!)}");
		}

		sb.AppendLine();
	}
}
