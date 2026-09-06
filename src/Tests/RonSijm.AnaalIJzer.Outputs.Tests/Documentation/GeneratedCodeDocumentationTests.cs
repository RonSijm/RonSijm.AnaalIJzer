using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Outputs.Documentation;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Outputs.Tests.Documentation;

public sealed class GeneratedCodeDocumentationTests
{
	[Fact]
	public void DocumentationGenerator_ExplainsConfiguredGeneratedCodeScope()
	{
		var config = ParseConfig("""
			<ArchitecturalLevels>
			  <GeneratedCode mode="IncludeConfigured" maximumDocumentLength="8192" description="Only owned generated output is checked.">
			    <Path endsWith="Generated_Clock_Kitchen.g.cs" />
			  </GeneratedCode>
			</ArchitecturalLevels>
			""");

		var markdown = ArchitectureDocumentationGenerator.GenerateMarkdown(config, null);

		markdown.Should().Contain("## Generated Code Analysis");
		markdown.Should().Contain("`mode`: `IncludeConfigured`");
		markdown.Should().Contain("`maximumDocumentLength`: `8192`");
		markdown.Should().Contain("configured path: `endsWith=\"Generated_Clock_Kitchen.g.cs\"`");
		markdown.Should().Contain("Only owned generated output is checked.");
		markdown.Should().Contain("- **GeneratedCode** `Generated source analysis (IncludeConfigured)`");
	}

	private static AnalyzerConfiguration ParseConfig(string config)
	{
		var additionalText = new TestAdditionalText("Architecture.anl", config);
		var result = ArchitecturalConfigParser.Parse([additionalText], CancellationToken.None);

		return result;
	}

	private sealed class TestAdditionalText(string path, string content) : AdditionalText
	{
		private readonly SourceText _text = SourceText.From(content);

		public override string Path { get; } = path;

		public override SourceText GetText(CancellationToken cancellationToken = default)
		{
			var result = _text;

			return result;
		}
	}
}
