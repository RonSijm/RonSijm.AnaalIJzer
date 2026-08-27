using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.DependencyRules.Tests.DependencyRules;

public sealed class DependencyEdgeTests
{
	[Fact]
	public void Edge_ComputesKindAndWildcardFlags()
	{
		var edge = CreateEdge("*", "Repository", DependencyRuleKind.Allowed, appliesToDescendants: true);

		edge.IsAllowed.Should().BeTrue();
		edge.IsBlocked.Should().BeFalse();
		edge.IsExplicit.Should().BeFalse();
		edge.IsWildcardTarget.Should().BeTrue();
		edge.IsWildcardSource.Should().BeFalse();
		edge.IsAllowAny.Should().BeFalse();
	}

	[Fact]
	public void Edge_UsesSiteFilterWhenCheckingAllowedSites()
	{
		var edge = new DependencyEdge(
			"/Ordering",
			"Application",
			"Repository",
			"Application",
			"Repository",
			new DependencySiteFilter(ImmutableHashSet.Create(StringComparer.Ordinal, "Constructor"), ImmutableHashSet<string>.Empty),
			false,
			DependencyRuleKind.Allowed,
			"Architecture.anl",
			14,
			7);

		edge.AllowsSite("Constructor").Should().BeTrue();
		edge.AllowsSite("Field").Should().BeFalse();
	}

	[Fact]
	public void ToXmlText_RendersRuleKindAndAppliesToDescendants()
	{
		var allowed = CreateEdge("Application", "Repository", DependencyRuleKind.Allowed, appliesToDescendants: true);
		var blocked = CreateEdge("Repository", "Controller", DependencyRuleKind.Blocked, appliesToDescendants: false);

		allowed.ToXmlText().Should().Be("<AllowedDependency from=\"Application\" to=\"Repository\" appliesToDescendants=\"true\"/>");
		blocked.ToXmlText().Should().Be("<BlockedDependency from=\"Repository\" to=\"Controller\"/>");
	}

	private static DependencyEdge CreateEdge(string from, string to, DependencyRuleKind kind, bool appliesToDescendants)
	{
		var result = new DependencyEdge(
			"/Ordering",
			from,
			to,
			from,
			to,
			DependencySiteFilter.All,
			appliesToDescendants,
			kind,
			"Architecture.anl",
			10,
			2);

		return result;
	}
}
