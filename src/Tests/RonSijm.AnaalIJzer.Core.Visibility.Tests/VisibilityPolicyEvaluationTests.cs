using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Engine.Visibility;
using RonSijm.AnaalIJzer.SymbolFacts;

namespace RonSijm.AnaalIJzer.Core.Visibility.Tests;

public sealed class VisibilityPolicyEvaluationTests
{
	[Fact]
	public void Evaluate_ReturnsNull_WhenTargetIsNotCovered()
	{
		var policy = CreatePolicy(
			ImmutableHashSet.Create(VisibilityPolicyTarget.Method),
			isAllowList: true,
			ImmutableHashSet.Create(ArchitectureAccessibility.Public));

		var result = policy.Evaluate(VisibilityPolicyTarget.Property, ArchitectureAccessibility.Private);

		result.Should().BeNull();
	}

	[Fact]
	public void Evaluate_ReturnsNull_WhenAllowListContainsAccessibility()
	{
		var policy = CreatePolicy(
			ImmutableHashSet.Create(VisibilityPolicyTarget.Method),
			isAllowList: true,
			ImmutableHashSet.Create(ArchitectureAccessibility.Public, ArchitectureAccessibility.Internal));

		var result = policy.Evaluate(VisibilityPolicyTarget.Method, ArchitectureAccessibility.Public);

		result.Should().BeNull();
	}

	[Fact]
	public void Evaluate_ReturnsViolation_WhenAllowListDoesNotContainAccessibility()
	{
		var policy = CreatePolicy(
			ImmutableHashSet.Create(VisibilityPolicyTarget.Property),
			isAllowList: true,
			ImmutableHashSet.Create(ArchitectureAccessibility.Internal, ArchitectureAccessibility.File));

		var result = policy.Evaluate(VisibilityPolicyTarget.Property, ArchitectureAccessibility.Public);

		result.Should().NotBeNull();
		result!.Value.Policy.OwnerLayerPath.Should().Be("Ordering/QuerySurface");
		result.Value.Target.Should().Be(VisibilityPolicyTarget.Property);
		result.Value.Accessibility.Should().Be(ArchitectureAccessibility.Public);
		result.Value.Reason.Should().Be("the VisibilityPolicy for Property in layer 'Ordering/QuerySurface' allows only Internal, File");
	}

	[Fact]
	public void Evaluate_ReturnsViolation_WhenBlockListContainsAccessibility()
	{
		var policy = CreatePolicy(
			ImmutableHashSet.Create(VisibilityPolicyTarget.Type),
			isAllowList: false,
			ImmutableHashSet.Create(ArchitectureAccessibility.Public, ArchitectureAccessibility.Protected));

		var result = policy.Evaluate(VisibilityPolicyTarget.Type, ArchitectureAccessibility.Public);

		result.Should().NotBeNull();
		result!.Value.Reason.Should().Be("the VisibilityPolicy for Type in layer 'Ordering/QuerySurface' blocks Public, Protected");
	}

	private static VisibilityPolicy CreatePolicy(
		ImmutableHashSet<VisibilityPolicyTarget> targets,
		bool isAllowList,
		ImmutableHashSet<ArchitectureAccessibility> accessibilities)
	{
		var result = new VisibilityPolicy(
			"Ordering/QuerySurface",
			targets,
			isAllowList,
			accessibilities,
			"Keep the query surface tucked in.",
			"Architecture.anl",
			12,
			4);

		return result;
	}
}
