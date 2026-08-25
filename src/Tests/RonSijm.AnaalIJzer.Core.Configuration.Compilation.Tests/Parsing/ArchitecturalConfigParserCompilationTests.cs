using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.TestSupport;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.Parsing;

public sealed class ArchitecturalConfigParserCompilationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<ArchitecturalLevels>")]
    public void Parser_ReturnsEmptyConfig_WhenAdditionalFileHasNoUsableXml(string configText)
    {
        var config = ParseConfig(configText);

        config.HasLayers.Should().BeFalse();
        config.LayerNames.Should().BeEmpty();
        config.AllowedEdges.Should().BeEmpty();
    }

    [Fact]
    public void Parser_SkipsUnnamedLayersAndIncompleteAllowedDependencies()
    {
        const string configText = """
                                  <ArchitecturalLevels>
                                      <Layer>
                                          <Class endsWith="Ghost" />
                                      </Layer>
                                      <Layer name="Application">
                                          <Class exactName="PizzaKitchen" />
                                          <Class />
                                      </Layer>
                                      <Layer name="Repository">
                                          <Class endsWith="Repository" />
                                      </Layer>
                                      <AllowedDependency from="Application" />
                                      <AllowedDependency to="Repository" />
                                      <AllowedDependency from="Application" to="Repository" />
                                  </ArchitecturalLevels>
                                  """;

        var config = ParseConfig(configText);

        config.LayerNames.Should().Equal("Application", "Repository");
        config.AllowedEdges.Should().ContainSingle()
            .Which.Should().Be(("Application", "Repository"));
        config.FindLayer("Ghost", string.Empty).Should().BeNull();

        var applicationMatch = config.FindLayer("PizzaKitchen", string.Empty);
        applicationMatch.Should().NotBeNull();
        applicationMatch!.Value.Layer.Name.Should().Be("Application");
    }

    [Fact]
    public void Parser_ReadsAllowedSites()
    {
        const string configText = """
                                  <ArchitecturalLevels>
                                      <Layer name="Controller"><Class typeName="PizzaController" /></Layer>
                                      <Layer name="Repository"><Class typeName="CheeseRepository" /></Layer>
                                      <AllowedDependency from="Controller" to="Repository" allowedSites=" local , METHODRETURN " />
                                  </ArchitecturalLevels>
                                  """;

        var config = ParseConfig(configText);

        var edge = config.Graph.DependencyEdges.Should().ContainSingle().Which;
        edge.SiteFilter.AllowedSites.Should().BeEquivalentTo(["Local", "MethodReturn"]);
        edge.SiteFilter.BlockedSites.Should().BeEmpty();
    }

    [Fact]
    public void Parser_ReadsAppliesToDescendants()
    {
        const string configText = """
                                  <ArchitecturalLevels>
                                      <Layer name="Controller"><Class typeName="PizzaController" /></Layer>
                                      <Layer name="Repository"><Class typeName="CheeseRepository" /></Layer>
                                      <AllowedDependency from="Controller" to="Repository" appliesToDescendants="1" />
                                  </ArchitecturalLevels>
                                  """;

        var config = ParseConfig(configText);

        config.Graph.DependencyEdges.Should().ContainSingle()
            .Which.AppliesToDescendants.Should().BeTrue();
    }

    [Fact]
    public void Parser_SkipsAllowedDependency_WhenSiteFilterIsInvalid()
    {
        const string configText = """
                                  <ArchitecturalLevels>
                                      <Layer name="Controller"><Class typeName="PizzaController" /></Layer>
                                      <Layer name="Repository"><Class typeName="CheeseRepository" /></Layer>
                                      <AllowedDependency from="Controller" to="Repository" allowedSites="Constructor" blockedSites="Field" />
                                      <AllowedDependency from="Controller" to="Repository" allowedSites="BananaPeel" />
                                  </ArchitecturalLevels>
                                  """;

        var config = ParseConfig(configText);

        config.Graph.DependencyEdges.Should().BeEmpty();
        config.AllowedEdges.Should().BeEmpty();
    }

    [Fact]
    public void Parser_SkipsDependency_WhenAppliesToDescendantsIsInvalid()
    {
        const string configText = """
                                  <ArchitecturalLevels>
                                      <Layer name="Controller"><Class typeName="PizzaController" /></Layer>
                                      <Layer name="Repository"><Class typeName="CheeseRepository" /></Layer>
                                      <AllowedDependency from="Controller" to="Repository" appliesToDescendants="sometimes" />
                                  </ArchitecturalLevels>
                                  """;

        var config = ParseConfig(configText);

        config.Graph.DependencyEdges.Should().BeEmpty();
        config.AllowedEdges.Should().BeEmpty();
        config.ConfigurationIssues.Should().Contain(item => item.Message.Contains("appliesToDescendants"));
    }

    [Fact]
    public void Matching_UsesAncestorScopeAndFirstMatchingSibling()
    {
        const string configText = """
                                  <ArchitecturalLevels>
                                    <Layer name="Ordering">
                                      <Namespace startsWith="Shop.Ordering" />
                                      <Layer name="First"><Class endsWith="Service" /></Layer>
                                      <Layer name="Second"><Class typeName="OrderService" /></Layer>
                                    </Layer>
                                  </ArchitecturalLevels>
                                  """;

        var config = ParseConfig(configText);

        config.FindLayer("OrderService", "Shop.Ordering.Application")!.Value.Layer.Name.Should().Be("Ordering/First");
        config.FindLayer("OrderService", "Shop.Billing.Application").Should().BeNull();
    }

    [Fact]
    public void Parser_AllowsRepeatedChildNamesUnderDifferentParents()
    {
        var config = ParseConfig(CreateCrossBoundaryConfig());

        config.ConfigurationIssues.Should().BeEmpty();
        config.LayerNames.Should().ContainInOrder("Ordering", "Ordering/Application", "Ordering/Repository", "Billing", "Billing/Application", "Billing/Contracts");
    }

    [Fact]
    public void Parser_RejectsDuplicateSiblingNamesAndUnrootedPaths()
    {
        const string configText = """
                                  <ArchitecturalLevels>
                                    <Layer name="Ordering">
                                      <Namespace startsWith="Shop.Ordering" />
                                      <Layer name="Application"><Class endsWith="Service" /></Layer>
                                      <Layer name="Application"><Class endsWith="Manager" /></Layer>
                                      <AllowedDependency from="Application" to="Billing/Contracts" />
                                    </Layer>
                                  </ArchitecturalLevels>
                                  """;

        var config = ParseConfig(configText);

        config.ConfigurationIssues.Should().Contain(item => item.Message.Contains("declared more than once"));
        config.ConfigurationIssues.Should().Contain(item => item.Message.Contains("must start with '/'"));
    }

    [Fact]
    public void CycleDetection_UsesCanonicalNestedPaths()
    {
        const string configText = """
                                  <ArchitecturalLevels enforceAcyclic="true">
                                    <Layer name="Ordering">
                                      <Namespace startsWith="Shop.Ordering" />
                                      <Layer name="Application"><Class endsWith="Service" /></Layer>
                                      <Layer name="Repository"><Class endsWith="Repository" /></Layer>
                                      <AllowedDependency from="Application" to="Repository" />
                                      <AllowedDependency from="Repository" to="Application" />
                                    </Layer>
                                  </ArchitecturalLevels>
                                  """;

        var config = ParseConfig(configText);

        config.ConfigurationIssues.Should().ContainSingle(item => item.Kind == ConfigurationIssueKind.CyclicDependencyGraph);
        config.ConfigurationIssues[0].Message.Should().Contain("Ordering/Application -> Ordering/Repository");
    }

    [Fact]
    public void Parser_ReadsProjectGroups_AndProjectReferenceRules()
    {
        const string configText = """
                                  <ArchitecturalLevels>
                                    <ProjectArchitecture requireRecognizedProjects="true">
                                      <ProjectGroup name="Presentation">
                                        <Project endsWith=".Web" />
                                      </ProjectGroup>
                                      <ProjectGroup name="Application">
                                        <Project endsWith=".Application" />
                                      </ProjectGroup>
                                      <AllowedProjectReference from="Presentation" to="Application" />
                                    </ProjectArchitecture>
                                  </ArchitecturalLevels>
                                  """;

        var config = ParseConfig(configText);

        config.HasProjectArchitecture.Should().BeTrue();
        config.ProjectArchitecture.RequireRecognizedProjects.Should().BeTrue();
        config.ProjectArchitecture.ProjectGroups.Select(group => group.Name).Should().Equal("Presentation", "Application");
        config.ProjectArchitecture.Rules.Should().ContainSingle();
        config.ProjectArchitecture.Rules[0].Kind.Should().Be(ProjectReferenceRuleKind.Allowed);
    }

    [Fact]
    public void Parser_ReadsPackagePolicies()
    {
        const string configText = """
                                  <ArchitecturalLevels>
                                    <ProjectArchitecture requireRecognizedProjects="true">
                                      <ProjectGroup name="Domain">
                                        <Project endsWith=".Domain" />
                                      </ProjectGroup>
                                      <PackagePolicy projectGroup="Domain" includeTransitive="true">
                                        <Allowed>
                                          <Package startsWith="System." />
                                        </Allowed>
                                        <Forbidden>
                                          <Package exactName="Microsoft.EntityFrameworkCore" />
                                        </Forbidden>
                                      </PackagePolicy>
                                    </ProjectArchitecture>
                                  </ArchitecturalLevels>
                                  """;

        var config = ParseConfig(configText);

        config.ProjectArchitecture.PackagePolicies.Should().ContainSingle();
        config.ProjectArchitecture.PackagePolicies[0].ProjectGroup.Should().Be("Domain");
        config.ProjectArchitecture.PackagePolicies[0].IncludeTransitive.Should().BeTrue();
        config.ProjectArchitecture.PackagePolicies[0].AllowedMatchers.Should().ContainSingle();
        config.ProjectArchitecture.PackagePolicies[0].ForbiddenMatchers.Should().ContainSingle();
    }

    private static AnalyzerConfiguration ParseConfig(string configText)
    {
        var result = ArchitecturalConfigParser.Parse(
            ImmutableArray.Create<AdditionalText>(
                new TestAdditionalText("Architecture.anl", configText)),
            CancellationToken.None);

        return result;
    }

    private static string CreateCrossBoundaryConfig()
    {
        var result = """
                     <ArchitecturalLevels>
                       <Layer name="Ordering">
                         <Namespace startsWith="Shop.Ordering" />
                         <Layer name="Application"><Namespace startsWith="Shop.Ordering.Application" /></Layer>
                         <Layer name="Repository"><Namespace startsWith="Shop.Ordering.Repository" /></Layer>
                         <AllowedDependency from="Application" to="/Billing/Contracts" />
                         <AllowedDependency from="Application" to="Repository" />
                       </Layer>
                       <Layer name="Billing">
                         <Namespace startsWith="Shop.Billing" />
                         <Layer name="Application"><Namespace startsWith="Shop.Billing.Application" /></Layer>
                         <Layer name="Contracts"><Namespace startsWith="Shop.Billing.Contracts" /></Layer>
                         <AllowedDependency from="/Ordering/Application" to="Contracts" />
                       </Layer>
                       <AllowedDependency from="Ordering" to="Billing" />
                     </ArchitecturalLevels>
                     """;

        return result;
    }
}
