using RonSijm.AnaalIJzer.Core.SemanticOperations.Tests.Support;

namespace RonSijm.AnaalIJzer.Core.SemanticOperations.Tests.Matching;

public sealed class SemanticOperationMatcherTests
{
	[Fact]
	public void Matcher_CombinesOperationContainingTypeAndMemberConditions()
	{
		const string source = """
		                      using System;

		                      namespace Bakery;

		                      [AttributeUsage(AttributeTargets.Method)]
		                      public sealed class NoDirectClockAttribute : Attribute { }

		                      public static class Clock
		                      {
		                          [NoDirectClock]
		                          public static void Tick<T>(T value) { }
		                      }

		                      public sealed class BakingService
		                      {
		                          public void Bake()
		                          {
		                              Clock.Tick<int>(5);
		                          }
		                      }
		                      """;
		var operation = SemanticOperationTestFactory.GetOperation<InvocationExpressionSyntax>(source);
		var matcher = new SemanticOperationMatcher(
			ImmutableHashSet.Create(SemanticOperationKind.Invocation),
			[new MatchCondition(MatchKind.EqualsFullName, "Bakery.Clock")],
			[
				new MatchCondition(MatchKind.Equals, "Tick", MatchOperand.Declaration),
				new MatchCondition(MatchKind.HasAttribute, "NoDirectClock", MatchOperand.Declaration)
			],
			true);

		var result = matcher.Matches(operation);

		result.Should().BeTrue();
	}

	[Fact]
	public void Matcher_RejectsDifferentOperationKindsAndStaticAccess()
	{
		const string source = """
		                      namespace Bakery;

		                      public sealed class Clock
		                      {
		                          public void Tick() { }
		                      }

		                      public sealed class BakingService
		                      {
		                          public void Bake(Clock clock)
		                          {
		                              clock.Tick();
		                          }
		                      }
		                      """;
		var operation = SemanticOperationTestFactory.GetOperation<InvocationExpressionSyntax>(source);
		var propertyMatcher = new SemanticOperationMatcher(
			ImmutableHashSet.Create(SemanticOperationKind.PropertyRead),
			ImmutableArray<MatchCondition>.Empty,
			ImmutableArray<MatchCondition>.Empty);
		var staticMatcher = new SemanticOperationMatcher(
			ImmutableHashSet.Create(SemanticOperationKind.Invocation),
			ImmutableArray<MatchCondition>.Empty,
			ImmutableArray<MatchCondition>.Empty,
			true);

		propertyMatcher.Matches(operation).Should().BeFalse();
		staticMatcher.Matches(operation).Should().BeFalse();
	}

	[Theory]
	[InlineData("Invocation", SemanticOperationKind.Invocation)]
	[InlineData("propertyread", SemanticOperationKind.PropertyRead)]
	[InlineData("EventAccess", SemanticOperationKind.EventAccess)]
	public void OperationKindParser_AcceptsCanonicalTokensCaseInsensitively(string token, SemanticOperationKind expected)
	{
		var parsed = SemanticOperationKindParser.TryParse(token, out var kind);

		parsed.Should().BeTrue();
		kind.Should().Be(expected);
	}

	[Fact]
	public void OperationKindParser_RejectsUnknownTokens()
	{
		var parsed = SemanticOperationKindParser.TryParse("PantryTeleport", out _);

		parsed.Should().BeFalse();
	}

	[Fact]
	public void MemberKindMatcher_DistinguishesPropertiesFromMethods()
	{
		const string source = """
			                      namespace Bakery;

			                      public sealed class Clock
			                      {
			                          public int Minute { get; }

			                          public int ReadMinute() => Minute;
			                      }

			                      public sealed class BakingService
			                      {
			                          public int Bake(Clock clock) => clock.Minute;
			                      }
			                      """;
		var operation = SemanticOperationTestFactory.GetOperation<MemberAccessExpressionSyntax>(source, node => node.Name.Identifier.ValueText == "Minute");
		var matcher = new SemanticOperationMatcher(
			ImmutableHashSet.Create(SemanticOperationKind.PropertyRead),
			ImmutableArray<MatchCondition>.Empty,
			ImmutableArray<MatchCondition>.Empty,
			null,
			ImmutableHashSet.Create(SemanticOperationMemberKind.Property));

		var result = matcher.Matches(operation);

		result.Should().BeTrue();
	}

	[Theory]
	[InlineData("Method", SemanticOperationMemberKind.Method)]
	[InlineData("property", SemanticOperationMemberKind.Property)]
	[InlineData("EVENT", SemanticOperationMemberKind.Event)]
	public void MemberKindParser_AcceptsCanonicalTokensCaseInsensitively(string token, SemanticOperationMemberKind expected)
	{
		var parsed = SemanticOperationMemberKindParser.TryParse(token, out var kind);

		parsed.Should().BeTrue();
		kind.Should().Be(expected);
	}
}
