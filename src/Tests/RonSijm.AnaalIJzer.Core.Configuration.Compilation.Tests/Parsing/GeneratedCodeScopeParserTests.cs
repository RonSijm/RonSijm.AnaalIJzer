using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.TestSupport;
using RonSijm.AnaalIJzer.Core.Observations;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.Parsing;

public sealed class GeneratedCodeScopeParserTests
{
	[Fact]
	public void Parser_ReadsCaseInsensitiveConfiguredGeneratedCodeScope()
	{
		const string configText = """
			<ArchitecturalLevels>
			  <GeneratedCode mode="includeconfigured" maximumDocumentLength="4096" description="Check selected generated kitchen code.">
			    <Path endsWith="Generated_Clock_Kitchen.g.cs" />
			  </GeneratedCode>
			</ArchitecturalLevels>
			""";

		var config = ParseConfig(configText);

		config.ConfigurationIssues.Should().BeEmpty();
		config.GeneratedCodeScope.Mode.Should().Be(GeneratedCodeAnalysisMode.IncludeConfigured);
		config.GeneratedCodeScope.MaximumDocumentLength.Should().Be(4096);
		config.GeneratedCodeScope.ConfiguredPaths.Should().ContainSingle()
			.Which.Matches(@"D:\repo\Generated_Clock_Kitchen.g.cs").Should().BeTrue();
	}

	[Theory]
	[InlineData("<GeneratedCode mode=\"Unknown\" />")]
	[InlineData("<GeneratedCode mode=\"IncludeConfigured\" />")]
	[InlineData("<GeneratedCode mode=\"IncludeAll\"><Path endsWith=\".g.cs\" /></GeneratedCode>")]
	[InlineData("<GeneratedCode mode=\"Exclude\" maximumDocumentLength=\"0\" />")]
	[InlineData("<GeneratedCode mode=\"IncludeConfigured\" comment=\"legacy\"><Path endsWith=\".g.cs\" /></GeneratedCode>")]
	public void Parser_RejectsInvalidGeneratedCodeScopes(string scopeXml)
	{
		var config = ParseConfig($"<ArchitecturalLevels>{scopeXml}</ArchitecturalLevels>");

		config.ConfigurationIssues.Should().NotBeEmpty();
		config.GeneratedCodeScope.Mode.Should().Be(GeneratedCodeAnalysisMode.Exclude);
	}

	private static AnalyzerConfiguration ParseConfig(string configText)
	{
		var result = ArchitecturalConfigParser.Parse(
			[
				new TestAdditionalText(@"D:\repo\Architecture.anl", configText)
			],
			CancellationToken.None);

		return result;
	}
}
