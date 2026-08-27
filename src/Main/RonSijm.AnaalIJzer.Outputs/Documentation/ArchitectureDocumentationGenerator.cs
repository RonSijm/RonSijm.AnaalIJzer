using RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model;

namespace RonSijm.AnaalIJzer.Outputs.Documentation;

/// <summary>
///     Facade for architecture documentation generation.
/// </summary>
internal static class ArchitectureDocumentationGenerator
{
	internal static string GenerateMarkdown(AnalyzerConfig config, string? assemblyName)
	{
		var result = ArchitectureDocumentationMarkdownBuilder.Generate(config, assemblyName);

		return result;
	}
}
