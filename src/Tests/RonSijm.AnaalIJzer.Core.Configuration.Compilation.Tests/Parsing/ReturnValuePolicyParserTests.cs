using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.TestSupport;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Matchers.Observations;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.Parsing;

public sealed class ReturnValuePolicyParserTests
{
	[Fact]
	public void Parser_ReadsGenericReturnValueMatchers()
	{
		const string configText = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <ReturnValuePolicy description="Application methods return a handled pizza.">
			      <Literal value="null" description="No invisible empty plate." />
			      <Literal value="" />
			      <Invocation withAttribute="CanBeNull" description="Nullable lookups get a fallback." />
			    </ReturnValuePolicy>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var config = ParseConfig(configText);

		config.HasReturnValuePolicies.Should().BeTrue();
		var policy = config.Layers.Should().ContainSingle().Which.ReturnValuePolicies.Should().ContainSingle().Subject;
		policy.Description.Should().Be("Application methods return a handled pizza.");
		policy.Rules.Should().HaveCount(3);
		policy.Rules[0].Matcher.Target.Should().Be(CodeObservationMatchTarget.Literal);
		policy.Rules[0].DisplayName.Should().Be("literal value=\"null\"");
		policy.Rules[1].Matcher.Conditions.Should().ContainSingle().Which.Value.Should().BeEmpty();
		policy.Rules[2].Matcher.Target.Should().Be(CodeObservationMatchTarget.Invocation);
		policy.Rules[2].Matcher.Conditions.Should().ContainSingle().Which.Value.Should().Be("CanBeNull");
	}

	[Theory]
	[InlineData("""<ReturnValuePolicy />""")]
	[InlineData("""<ReturnValuePolicy disallowExplicitNull="true"><Literal value="null" /></ReturnValuePolicy>""")]
	[InlineData("""<ReturnValuePolicy><Throw /></ReturnValuePolicy>""")]
	[InlineData("""<ReturnValuePolicy><Literal value="null" unexpected="pizza" /></ReturnValuePolicy>""")]
	public void Parser_RejectsInvalidReturnValuePolicy(string policyXml)
	{
		var configText = $"""
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    {policyXml}
			  </Layer>
			</ArchitecturalLevels>
			""";

		var config = ParseConfig(configText);

		config.ConfigurationIssues.Should().Contain(issue => issue.Kind == ConfigurationIssueKind.InvalidConfiguration);
		config.Layers.Should().ContainSingle().Which.ReturnValuePolicies.Should().BeEmpty();
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
