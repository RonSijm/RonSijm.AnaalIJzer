namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.Compilation;

public sealed class ArchitectureAnalyzerConfigFactoryTests
{
    [Fact]
    public void Create_PopulatesDocumentationOutputAndIssues()
    {
        var documentContext = new ArchitectureConfigurationDocumentParseContext(
            [new ArchitectureConfigurationDocumentInput(
                XElement.Parse("""
                    <ArchitecturalLevels description="Restaurant architecture" />
                    """),
                @"D:\repo\Architecture.anl",
                false)],
            ImmutableArray<ArchitectureConfigurationElementInput>.Empty,
            [new ArchitectureDocumentationItem(
                "Layer",
                "Waiter",
                "Greets the customer.",
                null,
                ImmutableArray<ArchitectureDocumentationAttribute>.Empty,
                0,
                "Waiter",
                @"D:\repo\Architecture.anl",
                1)]);
        var rootSettings = new ArchitectureConfigurationRootSettings(
            ImmutableHashSet.Create("Constructor"),
            ArchitectureExceptionPolicy.Disabled,
            true,
            false,
            new OutputConfig(true, "violations.md", true, "architecture.md"));
        var materialization = new ArchitectureConfigurationMaterializationResult(
            CompiledLayerCatalog.Empty,
            ImmutableArray<DependencyEdge>.Empty,
            ImmutableDictionary<string, ImmutableHashSet<string>>.Empty,
            ImmutableArray<ArchitectureExceptionDefinition>.Empty,
            ImmutableArray<ArchitectureExceptionReview>.Empty,
            ImmutableArray<string>.Empty,
            ImmutableArray<ArchitectureForbiddenPattern>.Empty,
            ProjectArchitectureConfig.Empty);
        var issues = ImmutableArray.CreateBuilder<ConfigurationIssue>();
        issues.Add(new ConfigurationIssue(ConfigurationIssueKind.InvalidConfiguration, "Nope.", @"D:\repo\Architecture.anl", 2, 4));

        var result = ArchitectureAnalyzerConfigFactory.Create(documentContext, rootSettings, materialization, issues);

        result.Documentation.Description.Should().Be("Restaurant architecture");
        result.Documentation.Items.Should().ContainSingle(item => item.Label == "Waiter");
        result.EnableReport.Should().BeTrue();
        result.ReportPath.Should().Be("violations.md");
        result.EnableDocumentation.Should().BeTrue();
        result.DocumentationPath.Should().Be("architecture.md");
        result.ConfigurationIssues.Should().ContainSingle(issue => issue.Message == "Nope.");
    }

    [Fact]
    public void Create_PreservesLayerNamesForbiddenPatternsAndRecognizedSites()
    {
        var documentContext = new ArchitectureConfigurationDocumentParseContext(
            ImmutableArray<ArchitectureConfigurationDocumentInput>.Empty,
            ImmutableArray<ArchitectureConfigurationElementInput>.Empty,
            ImmutableArray<ArchitectureDocumentationItem>.Empty);
        var rootSettings = new ArchitectureConfigurationRootSettings(
            ImmutableHashSet.Create("Constructor", "MethodReturn"),
            ArchitectureExceptionPolicy.Disabled,
            false,
            true,
            new OutputConfig(false, string.Empty, false, string.Empty));
        var layerSites = ImmutableDictionary.CreateBuilder<string, ImmutableHashSet<string>>(StringComparer.Ordinal);
        layerSites["Kitchen"] = ImmutableHashSet.Create("Local");
        var materialization = new ArchitectureConfigurationMaterializationResult(
            CompiledLayerCatalog.Empty,
            ImmutableArray<DependencyEdge>.Empty,
            layerSites.ToImmutable(),
            ImmutableArray<ArchitectureExceptionDefinition>.Empty,
            ImmutableArray<ArchitectureExceptionReview>.Empty,
            ["Waiter", "Kitchen"],
            [new ArchitectureForbiddenPattern("Store", "Use Repository instead.")],
            ProjectArchitectureConfig.Empty);
        var issues = ImmutableArray.CreateBuilder<ConfigurationIssue>();

        var result = ArchitectureAnalyzerConfigFactory.Create(documentContext, rootSettings, materialization, issues);

        result.LayerNames.Should().BeEquivalentTo(["Waiter", "Kitchen"]);
        result.ForbiddenPatterns.Should().ContainSingle(pattern => pattern.Name == "Store" && pattern.Comment == "Use Repository instead.");
        result.RequiresRecognizedDependencyAt("Constructor").Should().BeTrue();
        result.RequiresRecognizedDependencyAt("MethodReturn").Should().BeTrue();
        result.RequiresRecognizedDependencyAt("Local").Should().BeFalse();
        result.LayerRequiredRecognizedDependencySites.Should().ContainKey("Kitchen");
        result.LayerRequiredRecognizedDependencySites["Kitchen"].Should().Contain("Local");
        result.EnforceObservedAcyclic.Should().BeTrue();
    }
}
