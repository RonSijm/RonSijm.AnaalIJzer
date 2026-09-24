using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.TestSupport;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.NamespaceHierarchy.Policies;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.Parsing;

public sealed class NamespaceHierarchyPolicyParserTests
{
    [Fact]
    public void Parser_ReadsGlobalNamespaceHierarchyPolicyWithoutLayers()
    {
        const string configText = """
			<ArchitecturalLevels>
			  <NamespaceHierarchyPolicy rootNamespace="Shop" description="Feature namespaces own their implementation details.">
			    <BlockedRelation relation="DescendantToAncestor" allowedSites="Constructor, Field" description="Requests do not reach back into the root." />
			  </NamespaceHierarchyPolicy>
			</ArchitecturalLevels>
			""";

        var config = ParseConfig(configText);

        config.Layers.Should().BeEmpty();
        config.HasNamespaceHierarchyPolicies.Should().BeTrue();
        config.HasConfiguredRules.Should().BeTrue();
        var policy = config.NamespaceHierarchyPolicies.Should().ContainSingle().Subject;
        policy.RootNamespace.Should().Be("Shop");
        policy.Description.Should().Be("Feature namespaces own their implementation details.");
        var rule = policy.Rules.Should().ContainSingle().Subject;
        rule.Relation.Should().Be(NamespaceHierarchyRelation.DescendantToAncestor);
        rule.SiteFilter.AllowedSites.Should().BeEquivalentTo(["Constructor", "Field"]);
    }

    [Theory]
    [InlineData("<NamespaceHierarchyPolicy><BlockedRelation relation=\"DescendantToAncestor\" /></NamespaceHierarchyPolicy>")]
    [InlineData("<NamespaceHierarchyPolicy rootNamespace=\"Shop\" />")]
    [InlineData("<NamespaceHierarchyPolicy rootNamespace=\"Shop..Requests\"><BlockedRelation relation=\"DescendantToAncestor\" /></NamespaceHierarchyPolicy>")]
    [InlineData("<NamespaceHierarchyPolicy rootNamespace=\"Shop\"><BlockedRelation relation=\"Downhill\" /></NamespaceHierarchyPolicy>")]
    [InlineData("<NamespaceHierarchyPolicy rootNamespace=\"Shop\"><BlockedRelation relation=\"DescendantToAncestor\" allowedSites=\"Mystery\" /></NamespaceHierarchyPolicy>")]
    [InlineData("<NamespaceHierarchyPolicy rootNamespace=\"Shop\"><BlockedRelation relation=\"DescendantToAncestor\" allowedSites=\"Constructor\" blockedSites=\"Field\" /></NamespaceHierarchyPolicy>")]
    public void Parser_RejectsInvalidNamespaceHierarchyPolicies(string policyXml)
    {
        var configText = "<ArchitecturalLevels>" + policyXml + "</ArchitecturalLevels>";

        var config = ParseConfig(configText);

        config.ConfigurationIssues.Should().Contain(issue => issue.Kind == ConfigurationIssueKind.InvalidConfiguration);
        config.NamespaceHierarchyPolicies.Should().BeEmpty();
    }

    [Fact]
    public void Parser_RejectsNamespaceHierarchyPolicyNestedInALayer()
    {
        const string configText = """
			<ArchitecturalLevels>
			  <Layer name="Requests">
			    <Namespace exactName="Shop.Requests" />
			    <NamespaceHierarchyPolicy rootNamespace="Shop">
			      <BlockedRelation relation="DescendantToAncestor" />
			    </NamespaceHierarchyPolicy>
			  </Layer>
			</ArchitecturalLevels>
			""";

        var config = ParseConfig(configText);

        config.ConfigurationIssues.Should().Contain(issue => issue.Kind == ConfigurationIssueKind.InvalidConfiguration && issue.Message.Contains("root-level", StringComparison.Ordinal));
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