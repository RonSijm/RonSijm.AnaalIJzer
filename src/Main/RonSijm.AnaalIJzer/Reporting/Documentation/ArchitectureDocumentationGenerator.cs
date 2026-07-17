using System.Text;
using RonSijm.AnaalIJzer.Model;

namespace RonSijm.AnaalIJzer.Documentation;

/// <summary>
///     Renders the configured architecture as documentation: Mermaid diagrams plus the
///     descriptive XML story in authored order.
/// </summary>
internal static partial class ArchitectureDocumentationGenerator
{
	private const string WildcardNodeId = "Any";

	internal static string GenerateMarkdown(AnalyzerConfig config, string? assemblyName)
	{
		var sb = new StringBuilder();

		sb.AppendLine("# Architecture Documentation");
		sb.AppendLine();
		if (assemblyName is not null)
		{
			sb.AppendLine($"**Assembly**: `{assemblyName}`  ");
		}

		sb.AppendLine($"**Generated**: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
		sb.AppendLine();

		var description = config.Documentation.Description;
		if (!string.IsNullOrWhiteSpace(description))
		{
			sb.AppendLine(EscapeMarkdown(description!));
			sb.AppendLine();
		}

		AppendDependencyDiagrams(sb, config);
		AppendTypePolicies(sb, config);
		AppendConfigurationOrder(sb, config);
		return sb.ToString();
	}

}
