using RonSijm.AnaalIJzer.Model;

namespace RonSijm.AnaalIJzer.Documentation;

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
