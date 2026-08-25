using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.Contracts;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.Engine.ApiSurface;
using RonSijm.AnaalIJzer.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Engine.EntryPoints;
using RonSijm.AnaalIJzer.Engine.LayerModel;
using RonSijm.AnaalIJzer.Engine.NameRules;
using RonSijm.AnaalIJzer.Engine.Policies;
using RonSijm.AnaalIJzer.Engine.PolicyEvaluation;
using RonSijm.AnaalIJzer.Engine.Visibility;
using RonSijm.AnaalIJzer.Inheritance;
using RonSijm.AnaalIJzer.SourceLocations;

namespace RonSijm.AnaalIJzer.Core.PolicyEvaluation.Tests.PolicyEvaluation;

public sealed class PolicyEvaluationCoreTests
{
	[Fact]
	public void ArchitecturePolicyEngine_FindsLayerAndAppliesGlobalAllowedTypePolicies()
	{
		var layer = LayerDefinition.Normal("Service", null);
		var matcherRule = new MatcherRule(layer, ImmutableArray<ExceptionMatcher>.Empty, 1, 1, "Architecture.anl");
		var serviceNode = new LayerNode(
			layer,
			ImmutableArray.Create((new PatternMatcher(MatchTarget.TypeName, MatchKind.EndsWith, "Service"), matcherRule)),
			ImmutableArray<LayerNode>.Empty,
			ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)>.Empty,
			ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)>.Empty,
			ImmutableArray<NameMatchingRule>.Empty,
			ImmutableArray<ContractPolicy>.Empty,
			ImmutableArray<InheritancePolicy>.Empty,
			ImmutableArray<VisibilityPolicy>.Empty,
			ImmutableArray<ApiSurfacePolicy>.Empty,
			ImmutableArray<BoundaryEntryPointPolicy>.Empty,
			ImmutableArray<SourceLocationPolicy>.Empty);
		var globalAllowedRule = new MatcherRule(LayerDefinition.Normal("AllowedType", null), ImmutableArray<ExceptionMatcher>.Empty, 2, 1, "Architecture.anl");
		var catalog = new CompiledLayerCatalog(
			ImmutableArray.Create(serviceNode),
			ImmutableDictionary<string, LayerNode>.Empty.Add(layer.Name, serviceNode),
			ImmutableDictionary<string, MatcherRule>.Empty,
			ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)>.Empty,
			ImmutableArray.Create((new PatternMatcher(MatchTarget.TypeName, MatchKind.EndsWith, "Pizza"), globalAllowedRule)));
		var engine = new ArchitecturePolicyEngine(catalog);

		var layerMatch = engine.FindLayer("OrderService", "Shop.Application");
		var violation = engine.EvaluateTypePolicy(layerMatch!.Value, "Fork", "Shop.Tools");

		layerMatch!.Value.Layer.Name.Should().Be("Service");
		violation.Should().NotBeNull();
		violation!.Value.Reason.Should().Contain("global <Allowed> list");
	}

	[Fact]
	public void DependencyGraph_EvaluateDependency_AllowsCascadingRootEdges()
	{
		var graph = new DependencyGraph(
			ImmutableArray.Create(new DependencyEdge(
				string.Empty,
				"*",
				"Framework",
				"*",
				"Framework",
				DependencySiteFilter.All,
				true,
				DependencyRuleKind.Allowed,
				"Architecture.anl",
				1,
				1)));

		var evaluation = graph.EvaluateDependency("Application/Contracts", "Framework", "Constructor");

		evaluation.IsAllowed.Should().BeTrue();
	}

	[Fact]
	public void DependencyGraph_EvaluateDependency_ReportsSiteFilterReason()
	{
		var graph = new DependencyGraph(
			ImmutableArray.Create(new DependencyEdge(
				string.Empty,
				"*",
				"Framework",
				"*",
				"Framework",
				new DependencySiteFilter(ImmutableHashSet.Create<string>(StringComparer.Ordinal, "Constructor"), ImmutableHashSet<string>.Empty),
				true,
				DependencyRuleKind.Allowed,
				"Architecture.anl",
				1,
				1)));

		var evaluation = graph.EvaluateDependency("Application/Contracts", "Framework", "Method");

		evaluation.IsAllowed.Should().BeFalse();
		evaluation.DenialReason.Should().Contain("allowedSites does not include Method");
	}

	[Fact]
	public void DependencyCycleDetector_DeduplicatesRotations()
	{
		var cycles = DependencyCycleDetector.FindCycles(
			["Application", "Domain", "Infrastructure"],
			[
				("Application", "Domain"),
				("Domain", "Infrastructure"),
				("Infrastructure", "Application")
			]);

		cycles.Should().ContainSingle();
		cycles[0].Should().Equal(["Application", "Domain", "Infrastructure"]);
	}
}
