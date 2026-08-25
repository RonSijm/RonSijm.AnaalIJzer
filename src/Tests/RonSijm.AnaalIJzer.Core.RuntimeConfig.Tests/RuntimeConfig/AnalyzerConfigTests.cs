using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RonSijm.AnaalIJzer;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.Config.Parsing;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Engine.LayerModel;
using RonSijm.AnaalIJzer.Engine.Policies;
using RonSijm.AnaalIJzer.Exceptions;
using RonSijm.AnaalIJzer.Indicators;
using RonSijm.AnaalIJzer.Inheritance;
using RonSijm.AnaalIJzer.ProjectArchitecture;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;
using OutputConfiguration = RonSijm.AnaalIJzer.Model.OutputConfig;
using ArchitectureDoc = RonSijm.AnaalIJzer.Model.ArchitectureDocumentation;
using ArchitectureDocItem = RonSijm.AnaalIJzer.Model.ArchitectureDocumentationItem;

namespace RonSijm.AnaalIJzer.Core.RuntimeConfig.Tests.RuntimeConfig;

public sealed class AnalyzerConfigTests
{
	[Fact]
	public void Empty_ExposesDisabledDefaults()
	{
		AnalyzerConfiguration.Empty.HasLayers.Should().BeFalse();
		AnalyzerConfiguration.Empty.HasProjectArchitecture.Should().BeFalse();
		AnalyzerConfiguration.Empty.EnableDocumentation.Should().BeFalse();
		AnalyzerConfiguration.Empty.ConfigurationIssues.Should().BeEmpty();
	}

	[Fact]
	public void RequiresRecognizedDependencyAt_ChecksGlobalAndLayerSpecificSites()
	{
		var applicationLayer = LayerDefinition.Normal("Application", "Application services");
		var callerMatch = new LayerMatch(applicationLayer, [applicationLayer], ImmutableArray<LayerMatcherMatch>.Empty, null, 1, 1, "/Architecture.anl");
		var config = new AnalyzerConfiguration(new CompiledArchitectureConfig(
			CompiledLayerCatalog.Empty,
			new DependencyGraph(ImmutableArray<DependencyEdge>.Empty),
			new OutputConfiguration(false, string.Empty, false, string.Empty),
			ImmutableHashSet.Create("Constructor"),
			ImmutableDictionary<string, ImmutableHashSet<string>>.Empty.Add("Application", ImmutableHashSet.Create("MethodReturn")),
			ArchitectureExceptionPolicy.Disabled,
			ImmutableArray<ArchitectureExceptionDefinition>.Empty,
			ImmutableArray<ArchitectureExceptionReview>.Empty,
			false,
			false,
			["Application"],
			ImmutableArray<(string, string?)>.Empty,
			ProjectArchitectureConfig.Empty,
			ArchitectureDoc.Empty,
			ImmutableArray<ConfigurationIssue>.Empty));

		config.RequiresRecognizedDependencyAt("Constructor").Should().BeTrue();
		config.RequiresRecognizedDependencyAt(callerMatch, "MethodReturn").Should().BeTrue();
		config.RequiresRecognizedDependencyAt(callerMatch, "Field").Should().BeFalse();
	}

	[Fact]
	public void FindLayer_UsesCompiledCatalogAndReturnsTheMatchedLayer()
	{
		var config = CreateConfig(
			[CreateLayerNode("Service", "Service", "Service layer")],
			ImmutableArray<DependencyEdge>.Empty,
			enableDocumentation: true,
			documentationPath: "docs\\architecture.md",
			forbiddenPatterns: [("LegacyHelper", "legacy helper naming")]);

		var match = config.FindLayer("OrderService", "Demo.Application");

		match.Should().NotBeNull();
		match!.Value.Layer.Name.Should().Be("Service");
		match.Value.MatchedSuffix.Should().Be("Service");
		config.EnableDocumentation.Should().BeTrue();
		config.DocumentationPath.Should().Be("docs\\architecture.md");
		config.ForbiddenPatterns.Should().ContainSingle(pattern => pattern.Name == "LegacyHelper");
	}

	[Fact]
	public void DependencyRuleEvaluator_Evaluate_ReturnsAllowedForConfiguredEdge()
	{
		var callerLayer = LayerDefinition.Normal("Controller", null);
		var dependencyLayer = LayerDefinition.Normal("Service", null);
		var config = CreateConfig(
			[CreateLayerNode("Controller", "Controller"), CreateLayerNode("Service", "Service")],
			[
				new DependencyEdge(
					string.Empty,
					"Controller",
					"Service",
					"Controller",
					"Service",
					DependencySiteFilter.All,
					false,
					DependencyRuleKind.Allowed,
					"/Architecture.anl",
					1,
					1)
			]);
		var callerMatch = CreateLayerMatch(callerLayer);
		var dependencyMatch = CreateLayerMatch(dependencyLayer);
		var dependencyType = CreateTypeSymbol("OrderService");

		var decision = DependencyRuleEvaluator.Evaluate(config, callerMatch, dependencyMatch, dependencyType, "Constructor");

		decision.IsAllowed.Should().BeTrue();
		decision.Status.Should().Be(ArchitectureDependencySiteStatus.Allowed);
		decision.Reason.Should().Be("allowed by configured dependency rules");
	}

	[Fact]
	public void DependencyRuleEvaluator_Evaluate_ReturnsWrongDirectionForReverseEdge()
	{
		var callerLayer = LayerDefinition.Normal("Controller", null);
		var dependencyLayer = LayerDefinition.Normal("Service", null);
		var config = CreateConfig(
			[CreateLayerNode("Controller", "Controller"), CreateLayerNode("Service", "Service")],
			[
				new DependencyEdge(
					string.Empty,
					"Service",
					"Controller",
					"Service",
					"Controller",
					DependencySiteFilter.All,
					false,
					DependencyRuleKind.Allowed,
					"/Architecture.anl",
					1,
					1)
			]);
		var decision = DependencyRuleEvaluator.Evaluate(config, CreateLayerMatch(callerLayer), CreateLayerMatch(dependencyLayer), CreateTypeSymbol("OrderService"), "Constructor");

		decision.IsAllowed.Should().BeFalse();
		decision.Status.Should().Be(ArchitectureDependencySiteStatus.WrongDirection);
		decision.Reason.Should().Contain("wrong direction");
		decision.DiagnosticId.Should().Be(ArchitecturalDiagnosticIds.WrongDirectionDependency);
	}

	[Fact]
	public void DependencyRuleEvaluator_Evaluate_ReturnsSiteFilteredWhenAllowedSitesDoNotIncludeCurrentSite()
	{
		var callerLayer = LayerDefinition.Normal("Controller", null);
		var dependencyLayer = LayerDefinition.Normal("Service", null);
		var config = CreateConfig(
			[CreateLayerNode("Controller", "Controller"), CreateLayerNode("Service", "Service")],
			[
				new DependencyEdge(
					string.Empty,
					"Controller",
					"Service",
					"Controller",
					"Service",
					new DependencySiteFilter(ImmutableHashSet.Create("Constructor"), ImmutableHashSet<string>.Empty),
					false,
					DependencyRuleKind.Allowed,
					"/Architecture.anl",
					1,
					1)
			]);
		var decision = DependencyRuleEvaluator.Evaluate(config, CreateLayerMatch(callerLayer), CreateLayerMatch(dependencyLayer), CreateTypeSymbol("OrderService"), "Method");

		decision.IsAllowed.Should().BeFalse();
		decision.Status.Should().Be(ArchitectureDependencySiteStatus.SiteFiltered);
		decision.Reason.Should().Contain("allowedSites does not include Method");
	}

	private static AnalyzerConfiguration CreateConfig(
		ImmutableArray<LayerNode> layerNodes,
		ImmutableArray<DependencyEdge> dependencyEdges,
		bool enableDocumentation = false,
		string documentationPath = "",
		ImmutableArray<(string Name, string? Comment)> forbiddenPatterns = default)
	{
		var nodesByPath = layerNodes.ToImmutableDictionary(node => node.Definition.Name, StringComparer.Ordinal);
		var catalog = new CompiledLayerCatalog(
			layerNodes,
			nodesByPath,
			ImmutableDictionary<string, MatcherRule>.Empty,
			ImmutableArray<(PatternMatcher, MatcherRule)>.Empty,
			ImmutableArray<(PatternMatcher, MatcherRule)>.Empty);
		var config = new AnalyzerConfiguration(new CompiledArchitectureConfig(
			catalog,
			new DependencyGraph(dependencyEdges),
			new OutputConfiguration(false, string.Empty, enableDocumentation, documentationPath),
			ImmutableHashSet<string>.Empty,
			ImmutableDictionary<string, ImmutableHashSet<string>>.Empty,
			ArchitectureExceptionPolicy.Disabled,
			ImmutableArray<ArchitectureExceptionDefinition>.Empty,
			ImmutableArray<ArchitectureExceptionReview>.Empty,
			false,
			false,
			layerNodes.Select(node => node.Definition.Name).ToImmutableArray(),
			forbiddenPatterns.IsDefault ? ImmutableArray<(string, string?)>.Empty : forbiddenPatterns,
			ProjectArchitectureConfig.Empty,
			new ArchitectureDoc("Shared docs", ImmutableArray<ArchitectureDocItem>.Empty),
			ImmutableArray<ConfigurationIssue>.Empty));

		return config;
	}

	private static LayerNode CreateLayerNode(string layerName, string suffix, string? comment = null)
	{
		var layer = LayerDefinition.Normal(layerName, comment);
		var matcherRule = new MatcherRule(layer, ImmutableArray<ExceptionMatcher>.Empty, 12, 3, "/Architecture.anl");
		var matcher = new PatternMatcher(MatchTarget.TypeName, MatchKind.EndsWith, suffix);
		var node = new LayerNode(
			layer,
			[(matcher, matcherRule)],
			ImmutableArray<LayerNode>.Empty,
			ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)>.Empty,
			ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)>.Empty,
			ImmutableArray<RonSijm.AnaalIJzer.Engine.NameRules.NameMatchingRule>.Empty,
			ImmutableArray<RonSijm.AnaalIJzer.Contracts.ContractPolicy>.Empty,
			ImmutableArray<InheritancePolicy>.Empty,
			ImmutableArray<RonSijm.AnaalIJzer.Engine.Visibility.VisibilityPolicy>.Empty,
			ImmutableArray<RonSijm.AnaalIJzer.Engine.ApiSurface.ApiSurfacePolicy>.Empty,
			ImmutableArray<RonSijm.AnaalIJzer.Engine.EntryPoints.BoundaryEntryPointPolicy>.Empty,
			ImmutableArray<RonSijm.AnaalIJzer.SourceLocations.SourceLocationPolicy>.Empty);

		return node;
	}

	private static LayerMatch CreateLayerMatch(LayerDefinition layer)
	{
		var result = new LayerMatch(layer, [layer], ImmutableArray<LayerMatcherMatch>.Empty, null, 1, 1, "/Architecture.anl");

		return result;
	}

	private static INamedTypeSymbol CreateTypeSymbol(string typeName)
	{
		var source = $$"""
namespace Demo;

public class {{typeName}}
{
}
""";
		var tree = CSharpSyntaxTree.ParseText(source);
		var compilation = CSharpCompilation.Create(
			"RuntimeConfigTests",
			[tree],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
		var symbol = compilation.GetTypeByMetadataName("Demo." + typeName);

		symbol.Should().NotBeNull();

		return symbol!;
	}
}
