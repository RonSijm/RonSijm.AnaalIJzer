using System.Text;
using RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model;

namespace RonSijm.AnaalIJzer.Outputs.Documentation;

/// <summary>
///     Renders the configured architecture as documentation: Mermaid diagrams plus the
///     descriptive XML story in authored order.
/// </summary>
internal static partial class ArchitectureDocumentationMarkdownBuilder
{
	private const string WildcardNodeId = "Any";

	internal static string Generate(AnalyzerConfig config, string? assemblyName)
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

		AppendRootSettings(sb, config);
		AppendExceptionPolicy(sb, config);
		AppendDependencyDiagrams(sb, config);
		AppendTypePolicies(sb, config);
		AppendInheritancePolicies(sb, config);
		AppendVisibilityPolicies(sb, config);
		AppendContractPolicies(sb, config);
		AppendApiSurfacePolicies(sb, config);
		AppendSourceLocationPolicies(sb, config);
		AppendBoundaryEntryPointPolicies(sb, config);
		AppendProjectArchitecture(sb, config);
		AppendConfigurationOrder(sb, config);

		var result = sb.ToString();

		return result;
	}
}
