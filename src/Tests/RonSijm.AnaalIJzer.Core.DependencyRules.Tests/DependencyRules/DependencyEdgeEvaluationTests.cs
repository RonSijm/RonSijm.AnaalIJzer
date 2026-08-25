using RonSijm.AnaalIJzer.Engine.DependencyRules;

namespace RonSijm.AnaalIJzer.Core.DependencyRules.Tests.DependencyRules;

public sealed class DependencyEdgeEvaluationTests
{
	[Fact]
	public void Allowed_IsStableShortcut()
	{
		DependencyEdgeEvaluation.Allowed.IsAllowed.Should().BeTrue();
		DependencyEdgeEvaluation.Allowed.DenialKind.Should().Be(DependencyDenialKind.None);
		DependencyEdgeEvaluation.Allowed.IsDeniedByBlockedEdge.Should().BeFalse();
		DependencyEdgeEvaluation.Allowed.IsDeniedBySiteFilter.Should().BeFalse();
	}

	[Fact]
	public void Denied_CapturesScopeAndDenialFlags()
	{
		var result = DependencyEdgeEvaluation.Denied(
			"blockedSites blocks Field",
			DependencyDenialKind.SiteFilter,
			"/Ordering",
			"Application",
			"Repository");

		result.IsAllowed.Should().BeFalse();
		result.DenialReason.Should().Be("blockedSites blocks Field");
		result.ScopePath.Should().Be("/Ordering");
		result.FromPath.Should().Be("Application");
		result.ToPath.Should().Be("Repository");
		result.IsDeniedBySiteFilter.Should().BeTrue();
		result.IsDeniedByBlockedEdge.Should().BeFalse();
	}
}
