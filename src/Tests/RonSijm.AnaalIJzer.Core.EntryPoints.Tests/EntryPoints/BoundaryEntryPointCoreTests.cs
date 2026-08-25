using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Engine.EntryPoints;

namespace RonSijm.AnaalIJzer.Core.EntryPoints.Tests.EntryPoints;

public sealed class BoundaryEntryPointCoreTests
{
	[Fact]
	public void LayerSelector_MatchesDescendantPaths()
	{
		var selector = new BoundaryEntryPointSelector("Ordering/Contracts", ImmutableArray<BoundaryEntryPointMatcher>.Empty);

		selector.IsLayerSelector.Should().BeTrue();
		selector.Matches("Ordering/Contracts/Public", "PizzaContract", "Demo", dependencyType: null!).Should().BeTrue();
		selector.Matches("Ordering/Internal", "PizzaContract", "Demo", dependencyType: null!).Should().BeFalse();
		selector.ToDisplayText().Should().Be("Ordering/Contracts");
		BoundaryEntryPointSelector.IsContainedInBoundary("Ordering", "Ordering/Contracts").Should().BeTrue();
	}

	[Fact]
	public void MatcherSelector_UsesPatternMatchers()
	{
		var matcher = new BoundaryEntryPointMatcher(
			new PatternMatcher(MatchTarget.TypeName, MatchKind.Equals, "PizzaContract"),
			ImmutableArray<ExceptionMatcher>.Empty,
			"PizzaContract");
		var selector = new BoundaryEntryPointSelector(null, [matcher]);

		selector.IsLayerSelector.Should().BeFalse();
		selector.Matches("Ordering/Contracts", "PizzaContract", "Demo", dependencyType: null!).Should().BeTrue();
		selector.Matches("Ordering/Contracts", "CheeseRepository", "Demo", dependencyType: null!).Should().BeFalse();
		selector.ToDisplayText().Should().Be("PizzaContract");
	}

	[Fact]
	public void Rule_ToDisplayText_DelegatesToSelector()
	{
		var rule = new BoundaryEntryPointRule(
			new BoundaryEntryPointSelector("Ordering/Contracts", ImmutableArray<BoundaryEntryPointMatcher>.Empty),
			DependencySiteFilter.All,
			"Go through contracts first.",
			"Architecture.anl",
			8,
			2);

		rule.ToDisplayText().Should().Be("Ordering/Contracts");
	}

	[Fact]
	public void Denied_UsesRuleLocationWhenProvided()
	{
		var policy = new BoundaryEntryPointPolicy(
			"Ordering",
			ImmutableArray<BoundaryEntryPointRule>.Empty,
			"Outside callers must enter through contracts.",
			"Architecture.anl",
			5,
			1);
		var rule = new BoundaryEntryPointRule(
			new BoundaryEntryPointSelector("Ordering/Contracts", ImmutableArray<BoundaryEntryPointMatcher>.Empty),
			DependencySiteFilter.All,
			null,
			"Rules.anl",
			11,
			7);

		var result = BoundaryEntryPointEvaluation.Denied(policy, "the matching entry point does not allow site Method", "Ordering/Contracts", rule);

		result.IsAllowed.Should().BeFalse();
		result.BoundaryLayerName.Should().Be("Ordering");
		result.MatchedEntryPoint.Should().Be("Ordering/Contracts");
		result.XmlPath.Should().Be("Rules.anl");
		result.XmlLineNumber.Should().Be(11);
		result.XmlLinePosition.Should().Be(7);
	}
}
