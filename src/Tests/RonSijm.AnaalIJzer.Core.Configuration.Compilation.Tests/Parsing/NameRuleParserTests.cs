using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.TestSupport;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.NameRules;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.Parsing;

public sealed class NameRuleParserTests
{
	[Fact]
	public void Parser_ReadsDirectAndIntraProceduralValueTracking()
	{
		const string configText = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <NameRules>
			      <RequireMatchingNames>
			        <Name endsWith="Id" />
			      </RequireMatchingNames>
			      <RequireMatchingNames valueTracking="IntraProcedural">
			        <Name endsWith="Token" />
			      </RequireMatchingNames>
			    </NameRules>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var config = ParseConfig(configText);

		config.ConfigurationIssues.Should().BeEmpty();
		var rules = config.Layers.Should().ContainSingle().Which.NameRules;
		rules.Should().HaveCount(2);
		rules[0].ValueTracking.Should().Be(NameRuleValueTrackingMode.Direct);
		rules[1].ValueTracking.Should().Be(NameRuleValueTrackingMode.IntraProcedural);
		config.HasIntraProceduralNameRules.Should().BeTrue();
	}

	[Theory]
	[InlineData("WholeProgram")]
	[InlineData("directly")]
	public void Parser_RejectsUnknownValueTrackingMode(string valueTracking)
	{
		var config = ParseConfig($"""
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <NameRules>
			      <RequireMatchingNames valueTracking="{valueTracking}">
			        <Name endsWith="Id" />
			      </RequireMatchingNames>
			    </NameRules>
			  </Layer>
			</ArchitecturalLevels>
			""");

		config.ConfigurationIssues.Should().Contain(issue => issue.Kind == ConfigurationIssueKind.InvalidConfiguration && issue.Message.Contains("valueTracking", StringComparison.Ordinal));
		config.Layers.Should().ContainSingle().Which.NameRules.Should().BeEmpty();
	}

	[Fact]
	public void Parser_RejectsValueTrackingOnADeclarationNameRule()
	{
		var config = ParseConfig("""
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <NameRules>
			      <RequireDeclarationNameMatchesType valueTracking="IntraProcedural">
			        <Type endsWith="Id" />
			      </RequireDeclarationNameMatchesType>
			    </NameRules>
			  </Layer>
			</ArchitecturalLevels>
			""");

		config.ConfigurationIssues.Should().Contain(issue => issue.Kind == ConfigurationIssueKind.InvalidConfiguration && issue.Message.Contains("RequireMatchingNames", StringComparison.Ordinal));
		config.Layers.Should().ContainSingle().Which.NameRules.Should().BeEmpty();
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
