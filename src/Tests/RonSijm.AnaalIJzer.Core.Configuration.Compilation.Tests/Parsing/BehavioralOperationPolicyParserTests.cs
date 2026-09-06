using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.TestSupport;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.OperationPolicies.Behavioral;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Model;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.Parsing;

public sealed class BehavioralOperationPolicyParserTests
{
	[Fact]
	public void Parser_ReadsEveryBehavioralOperationRuleFamily()
	{
		const string configText = """
			<ArchitecturalLevels>
			  <Layer name="Kitchen">
			    <Class endsWith="Kitchen" />
			    <BehavioralOperations description="The kitchen validates an order before saving it.">
			      <RequiredOperation ordering="Lexical" allowedSites="Method">
			        <DeclarationMatcher>
			          <Member exactName="Prepare" memberKind="Method" />
			        </DeclarationMatcher>
			        <OperationMatcher kind="Invocation">
			          <ContainingType typeName="PizzaValidator" />
			          <Member exactName="Validate" memberKind="Method" />
			        </OperationMatcher>
			      </RequiredOperation>
			      <RequiredOperationBefore>
			        <DeclarationMatcher>
			          <Member exactName="Submit" memberKind="Method" />
			        </DeclarationMatcher>
			        <OperationMatcher kind="Invocation">
			          <Member exactName="Validate" memberKind="Method" />
			        </OperationMatcher>
			        <BeforeOperation>
			          <OperationMatcher kind="Invocation">
			            <Member exactName="Save" memberKind="Method" />
			          </OperationMatcher>
			        </BeforeOperation>
			      </RequiredOperationBefore>
			      <ForbiddenOperationAfter ordering="Lexical">
			        <DeclarationMatcher>
			          <Member exactName="Submit" memberKind="Method" />
			        </DeclarationMatcher>
			        <OperationMatcher kind="Invocation">
			          <Member exactName="Audit" memberKind="Method" />
			        </OperationMatcher>
			        <AfterOperation>
			          <OperationMatcher kind="Invocation">
			            <Member exactName="Commit" memberKind="Method" />
			          </OperationMatcher>
			        </AfterOperation>
			      </ForbiddenOperationAfter>
			      <MaximumOperationCount maximum="1">
			        <DeclarationMatcher>
			          <Member exactName="Submit" memberKind="Method" />
			        </DeclarationMatcher>
			        <OperationMatcher kind="Invocation">
			          <Member exactName="Publish" memberKind="Method" />
			        </OperationMatcher>
			      </MaximumOperationCount>
			    </BehavioralOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var config = ParseConfig(configText);

		config.ConfigurationIssues.Should().BeEmpty();
		config.HasBehavioralOperationPolicies.Should().BeTrue();
		var policy = config.Layers.Should().ContainSingle().Which.BehavioralOperationPolicies.Should().ContainSingle().Subject;
		policy.Description.Should().Be("The kitchen validates an order before saving it.");
		policy.Rules.Should().HaveCount(4);
		policy.Rules[0].Kind.Should().Be(BehavioralOperationRuleKind.RequiredOperation);
		policy.Rules[0].Ordering.Should().Be(BehavioralOperationOrdering.Lexical);
		policy.Rules[0].SiteFilter.Allows("Method").Should().BeTrue();
		policy.Rules[0].SiteFilter.Allows("Local").Should().BeFalse();
		policy.Rules[0].DeclarationMatcher.MemberKinds.Should().ContainSingle().Which.Should().Be(SemanticOperationMemberKind.Method);
		policy.Rules[1].Kind.Should().Be(BehavioralOperationRuleKind.RequiredOperationBefore);
		policy.Rules[1].Ordering.Should().Be(BehavioralOperationOrdering.Dominance);
		policy.Rules[1].RelatedOperationMatchers.Should().ContainSingle();
		policy.Rules[2].Kind.Should().Be(BehavioralOperationRuleKind.ForbiddenOperationAfter);
		policy.Rules[2].RelatedOperationMatchers.Should().ContainSingle();
		policy.Rules[3].Kind.Should().Be(BehavioralOperationRuleKind.MaximumOperationCount);
		policy.Rules[3].MaximumCount.Should().Be(1);
	}

	[Theory]
	[InlineData("""<BehavioralOperations />""")]
	[InlineData("""<BehavioralOperations><RequiredOperation><DeclarationMatcher><Member exactName="Submit" memberKind="Method" /></DeclarationMatcher></RequiredOperation></BehavioralOperations>""")]
	[InlineData("""<BehavioralOperations><RequiredOperation ordering="Sideways"><DeclarationMatcher><Member exactName="Submit" memberKind="Method" /></DeclarationMatcher><OperationMatcher kind="Invocation" /></RequiredOperation></BehavioralOperations>""")]
	[InlineData("""<BehavioralOperations><RequiredOperation><DeclarationMatcher /><OperationMatcher kind="Invocation" /></RequiredOperation></BehavioralOperations>""")]
	[InlineData("""<BehavioralOperations><RequiredOperationBefore><DeclarationMatcher><Member exactName="Submit" memberKind="Method" /></DeclarationMatcher><OperationMatcher kind="Invocation" /></RequiredOperationBefore></BehavioralOperations>""")]
	[InlineData("""<BehavioralOperations><ForbiddenOperationAfter><DeclarationMatcher><Member exactName="Submit" memberKind="Method" /></DeclarationMatcher><OperationMatcher kind="Invocation" /><AfterOperation /></ForbiddenOperationAfter></BehavioralOperations>""")]
	[InlineData("""<BehavioralOperations><MaximumOperationCount maximum="0"><DeclarationMatcher><Member exactName="Submit" memberKind="Method" /></DeclarationMatcher><OperationMatcher kind="Invocation" /></MaximumOperationCount></BehavioralOperations>""")]
	public void Parser_RejectsInvalidBehavioralOperationPolicies(string policyXml)
	{
		var configText = $"""
			<ArchitecturalLevels>
			  <Layer name="Kitchen">
			    <Class endsWith="Kitchen" />
			    {policyXml}
			  </Layer>
			</ArchitecturalLevels>
			""";

		var config = ParseConfig(configText);

		config.ConfigurationIssues.Should().Contain(issue => issue.Kind == ConfigurationIssueKind.InvalidConfiguration);
		config.Layers.Should().ContainSingle().Which.BehavioralOperationPolicies.Should().BeEmpty();
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
