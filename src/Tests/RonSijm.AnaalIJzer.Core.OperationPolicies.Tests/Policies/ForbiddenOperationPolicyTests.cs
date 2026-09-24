using RonSijm.AnaalIJzer.Core.OperationPolicies.Tests.Support;

namespace RonSijm.AnaalIJzer.Core.OperationPolicies.Tests.Policies;

public sealed class ForbiddenOperationPolicyTests
{
    [Fact]
    public void Policy_RejectsMatchedOperationAtAnAllowedSite()
    {
        const string source = """
			using System;

			public sealed class PizzaKitchen
			{
				public DateTime Prepare() => DateTime.UtcNow;
			}
			""";
        var operation = OperationPolicyTestFactory.GetOperation<MemberAccessExpressionSyntax>(source, node => node.Name.Identifier.ValueText == "UtcNow");
        var matcher = new SemanticOperationMatcher(
            ImmutableHashSet.Create(SemanticOperationKind.PropertyRead),
            [new MatchCondition(MatchKind.EqualsFullName, "System.DateTime")],
            [new MatchCondition(MatchKind.Equals, "UtcNow", MatchOperand.Declaration)],
            true,
            ImmutableHashSet.Create(SemanticOperationMemberKind.Property));
        var rule = new ForbiddenOperationRule(
            [matcher],
            new DependencySiteFilter(ImmutableHashSet.Create(DependencySites.StaticMember), ImmutableHashSet<string>.Empty),
            "PropertyRead operation",
            null,
            "Architecture.anl",
            1,
            1);
        var policy = new ForbiddenOperationPolicy("Application", [rule], "Application receives time through an adapter.", "Architecture.anl", 1, 1);

        var evaluation = policy.Evaluate(operation);

        evaluation.Should().NotBeNull();
        evaluation!.Value.Reason.Should().Contain("DateTime.UtcNow");
        evaluation.Value.Reason.Should().Contain(DependencySites.StaticMember);
    }

    [Fact]
    public void Policy_DoesNotApplyWhenItsSiteFilterExcludesTheOperation()
    {
        const string source = """
			using System;

			public sealed class PizzaKitchen
			{
				public DateTime Prepare() => DateTime.UtcNow;
			}
			""";
        var operation = OperationPolicyTestFactory.GetOperation<MemberAccessExpressionSyntax>(source, node => node.Name.Identifier.ValueText == "UtcNow");
        var matcher = new SemanticOperationMatcher(
            ImmutableHashSet.Create(SemanticOperationKind.PropertyRead),
            ImmutableArray<MatchCondition>.Empty,
            ImmutableArray<MatchCondition>.Empty,
            true);
        var rule = new ForbiddenOperationRule(
            [matcher],
            new DependencySiteFilter(ImmutableHashSet.Create(DependencySites.Method), ImmutableHashSet<string>.Empty),
            "PropertyRead operation",
            null,
            "Architecture.anl",
            1,
            1);
        var policy = new ForbiddenOperationPolicy("Application", [rule], null, "Architecture.anl", 1, 1);

        var evaluation = policy.Evaluate(operation);

        evaluation.Should().BeNull();
    }
}