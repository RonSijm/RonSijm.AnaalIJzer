using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.TestSupport;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Model;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.Parsing;

public sealed class ForbiddenOperationPolicyParserTests
{
    [Fact]
    public void Parser_ReadsLayerScopedOperationMatchers()
    {
        const string configText = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <ForbiddenOperations description="Application code receives time through an adapter.">
			      <ForbiddenOperation allowedSites="StaticMember" description="Do not read the system clock directly.">
			        <OperationMatcher kind="PropertyRead" staticAccess="true">
			          <ContainingType exactFullName="System.DateTime" />
			          <Member exactName="UtcNow" memberKind="Property" />
			        </OperationMatcher>
			      </ForbiddenOperation>
			    </ForbiddenOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

        var config = ParseConfig(configText);

        config.HasForbiddenOperationPolicies.Should().BeTrue();
        var policy = config.Layers.Should().ContainSingle().Which.ForbiddenOperationPolicies.Should().ContainSingle().Subject;
        policy.Description.Should().Be("Application code receives time through an adapter.");
        var rule = policy.Rules.Should().ContainSingle().Subject;
        rule.SiteFilter.Allows("StaticMember").Should().BeTrue();
        rule.SiteFilter.Allows("Method").Should().BeFalse();
        var matcher = rule.Matchers.Should().ContainSingle().Subject;
        matcher.Kinds.Should().ContainSingle().Which.Should().Be(SemanticOperationKind.PropertyRead);
        matcher.RequireStaticAccess.Should().BeTrue();
        matcher.ContainingTypeConditions.Should().ContainSingle().Which.Value.Should().Be("System.DateTime");
        matcher.MemberConditions.Should().ContainSingle().Which.Value.Should().Be("UtcNow");
        matcher.MemberKinds.Should().ContainSingle().Which.Should().Be(SemanticOperationMemberKind.Property);
    }

    [Fact]
    public void Parser_ReadsTypeNameAndTypeKindOnOperationMatchers()
    {
        const string configText = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Kitchen" />
			    <ForbiddenOperations>
			      <ForbiddenOperation>
			        <OperationMatcher kind="PropertyRead">
			          <ContainingType typeName="Task" typeKind="Class" />
			          <Member typeName="String" typeKind="Class" memberKind="Property" />
			        </OperationMatcher>
			      </ForbiddenOperation>
			    </ForbiddenOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

        var config = ParseConfig(configText);

        config.ConfigurationIssues.Should().BeEmpty();
        var matcher = config.Layers.Should().ContainSingle().Which.ForbiddenOperationPolicies.Should().ContainSingle().Which.Rules.Should().ContainSingle().Which.Matchers.Should().ContainSingle().Subject;
        matcher.ContainingTypeConditions.Should().HaveCount(2);
        matcher.MemberConditions.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("""<ForbiddenOperations />""")]
    [InlineData("""<ForbiddenOperations><ForbiddenOperation /></ForbiddenOperations>""")]
    [InlineData("""<ForbiddenOperations><ForbiddenOperation><OperationMatcher kind="PropertyRead"><Member exactName="UtcNow" memberKind="Method" /></OperationMatcher></ForbiddenOperation></ForbiddenOperations>""")]
    [InlineData("""<ForbiddenOperations><ForbiddenOperation><OperationMatcher kind="Return"><Member exactName="UtcNow" /></OperationMatcher></ForbiddenOperation></ForbiddenOperations>""")]
    public void Parser_RejectsInvalidForbiddenOperationPolicy(string policyXml)
    {
        var configText = $"""
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Kitchen" />
			    {policyXml}
			  </Layer>
			</ArchitecturalLevels>
			""";

        var config = ParseConfig(configText);

        config.ConfigurationIssues.Should().Contain(issue => issue.Kind == ConfigurationIssueKind.InvalidConfiguration);
        config.Layers.Should().ContainSingle().Which.ForbiddenOperationPolicies.Should().BeEmpty();
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