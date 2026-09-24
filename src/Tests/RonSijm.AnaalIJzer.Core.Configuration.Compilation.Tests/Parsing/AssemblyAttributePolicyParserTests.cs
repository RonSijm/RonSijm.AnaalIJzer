using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.TestSupport;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.Parsing;

public sealed class AssemblyAttributePolicyParserTests
{
    [Fact]
    public void Parser_ReadsAssemblyAttributePoliciesWithoutLayers()
    {
        const string configText = """
			<ArchitecturalLevels>
			  <AssemblyAttributePolicy description="Only approved projects receive internal access.">
			    <Allowed>
			      <Attribute exactFullName="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
			        <Argument index="0" exactName="AllowedExample" />
			      </Attribute>
			    </Allowed>
			  </AssemblyAttributePolicy>
			</ArchitecturalLevels>
			""";

        var config = ParseConfig(configText);

        config.ConfigurationIssues.Should().BeEmpty();
        config.HasAssemblyAttributePolicies.Should().BeTrue();
        var policy = config.AssemblyAttributePolicies.Policies.Should().ContainSingle().Subject;
        var rule = policy.AllowedRules.Should().ContainSingle().Subject;
        rule.AttributeMatcher.Conditions.Should().ContainSingle(condition => condition.Kind == RonSijm.AnaalIJzer.Core.Matchers.Conditions.MatchKind.EqualsFullName);
        var argument = rule.ArgumentMatchers.Should().ContainSingle().Subject;
        argument.Index.Should().Be(0);
        argument.Name.Should().BeNull();
    }

    [Fact]
    public void Parser_PreservesAnAttributeRuleCommentAsItsDescription()
    {
        const string configText = """
			<ArchitecturalLevels>
			  <AssemblyAttributePolicy>
			    <Forbidden>
			      <Attribute exactFullName="System.Runtime.CompilerServices.InternalsVisibleToAttribute"
			                 comment="This legacy note remains visible in reports.">
			        <Argument index="0" exactName="NotAllowedExample" />
			      </Attribute>
			    </Forbidden>
			  </AssemblyAttributePolicy>
			</ArchitecturalLevels>
			""";

        var config = ParseConfig(configText);

        config.ConfigurationIssues.Should().BeEmpty();
        var rule = config.AssemblyAttributePolicies.Policies.Single().ForbiddenRules.Single();
        rule.Description.Should().Be("This legacy note remains visible in reports.");
    }

    [Theory]
    [InlineData("""<Argument index="0" name="Purpose" exactName="Tests" />""")]
    [InlineData("""<Argument exactName="Tests" />""")]
    [InlineData("""<Argument index="0" exactFullName="Tests" />""")]
    [InlineData("""<Attribute exactFullName="System.Runtime.CompilerServices.InternalsVisibleToAttribute" typeKind="NotAType"><Argument index="0" exactName="AllowedExample" /></Attribute>""")]
    public void Parser_RejectsInvalidAssemblyAttributePolicyRules(string attributeOrArgumentXml)
    {
        var attribute = attributeOrArgumentXml.StartsWith("<Argument", StringComparison.Ordinal)
            ? "<Attribute exactFullName=\"System.Runtime.CompilerServices.InternalsVisibleToAttribute\">" + attributeOrArgumentXml + "</Attribute>"
            : attributeOrArgumentXml;
        var config = ParseConfig("<ArchitecturalLevels><AssemblyAttributePolicy><Forbidden>" + attribute + "</Forbidden></AssemblyAttributePolicy></ArchitecturalLevels>");

        config.ConfigurationIssues.Should().Contain(issue => issue.Kind == ConfigurationIssueKind.InvalidConfiguration);
        config.AssemblyAttributePolicies.Policies.Should().BeEmpty();
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