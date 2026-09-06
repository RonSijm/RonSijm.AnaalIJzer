using RonSijm.AnaalIJzer.Core.OperationPolicies.Behavioral;
using RonSijm.AnaalIJzer.Core.OperationPolicies.Tests.Support;

namespace RonSijm.AnaalIJzer.Core.OperationPolicies.Tests.Behavioral;

public sealed class BehavioralOperationPolicyTests
{
	[Fact]
	public void RequiredOperation_Dominance_RejectsAnOperationThatOnlyRunsOnOneBranch()
	{
		var body = OperationPolicyTestFactory.GetBehavioralBodyAnalysis(
			"""
			public sealed class PizzaKitchen
			{
				public void Submit(bool isReady)
				{
					if (isReady)
					{
						PizzaValidator.Validate();
					}

					PizzaRepository.Save();
				}
			}

			public static class PizzaValidator { public static void Validate() { } }
			public static class PizzaRepository { public static void Save() { } }
			""",
			"Submit");
		var rule = CreateRule(BehavioralOperationRuleKind.RequiredOperation, [CreateInvocationMatcher("PizzaValidator", "Validate")], [], BehavioralOperationOrdering.Dominance, 0, "Validation");
		var policy = new BehavioralOperationPolicy("Kitchen", [rule], null, "Architecture.anl", 1, 1);

		var evaluations = policy.Evaluate(body);

		evaluations.Should().ContainSingle();
		evaluations[0].ViolationKind.Should().Be(BehavioralOperationViolationKind.RequiredOperationDoesNotDominateExit);
	}

	[Fact]
	public void RequiredOperation_Lexical_AllowsAnOperationThatOccursOnOneBranch()
	{
		var body = OperationPolicyTestFactory.GetBehavioralBodyAnalysis(
			"""
			public sealed class PizzaKitchen
			{
				public void Submit(bool isReady)
				{
					if (isReady)
					{
						PizzaValidator.Validate();
					}

					PizzaRepository.Save();
				}
			}

			public static class PizzaValidator { public static void Validate() { } }
			public static class PizzaRepository { public static void Save() { } }
			""",
			"Submit");
		var rule = CreateRule(BehavioralOperationRuleKind.RequiredOperation, [CreateInvocationMatcher("PizzaValidator", "Validate")], [], BehavioralOperationOrdering.Lexical, 0, "Validation");
		var policy = new BehavioralOperationPolicy("Kitchen", [rule], null, "Architecture.anl", 1, 1);

		var evaluations = policy.Evaluate(body);

		evaluations.Should().BeEmpty();
	}

	[Fact]
	public void RequiredOperationBefore_Dominance_RequiresTheOperationBeforeTheSelectedTarget()
	{
		var body = OperationPolicyTestFactory.GetBehavioralBodyAnalysis(
			"""
			public sealed class PizzaKitchen
			{
				public void Submit(bool isReady)
				{
					if (isReady)
					{
						PizzaValidator.Validate();
					}

					PizzaRepository.Save();
				}
			}

			public static class PizzaValidator { public static void Validate() { } }
			public static class PizzaRepository { public static void Save() { } }
			""",
			"Submit");
		var rule = CreateRule(
			BehavioralOperationRuleKind.RequiredOperationBefore,
			[CreateInvocationMatcher("PizzaValidator", "Validate")],
			[CreateInvocationMatcher("PizzaRepository", "Save")],
			BehavioralOperationOrdering.Dominance,
			0,
			"Validation before save");
		var policy = new BehavioralOperationPolicy("Kitchen", [rule], null, "Architecture.anl", 1, 1);

		var evaluations = policy.Evaluate(body);

		evaluations.Should().ContainSingle();
		evaluations[0].ViolationKind.Should().Be(BehavioralOperationViolationKind.MissingRequiredOperationBefore);
	}

	[Fact]
	public void RequiredOperationBefore_Dominance_AllowsTheOperationBeforeTheSelectedTarget()
	{
		var body = OperationPolicyTestFactory.GetBehavioralBodyAnalysis(
			"""
			public sealed class PizzaKitchen
			{
				public void Submit()
				{
					PizzaValidator.Validate();
					PizzaRepository.Save();
				}
			}

			public static class PizzaValidator { public static void Validate() { } }
			public static class PizzaRepository { public static void Save() { } }
			""",
			"Submit");
		var rule = CreateRule(
			BehavioralOperationRuleKind.RequiredOperationBefore,
			[CreateInvocationMatcher("PizzaValidator", "Validate")],
			[CreateInvocationMatcher("PizzaRepository", "Save")],
			BehavioralOperationOrdering.Dominance,
			0,
			"Validation before save");
		var policy = new BehavioralOperationPolicy("Kitchen", [rule], null, "Architecture.anl", 1, 1);

		var evaluations = policy.Evaluate(body);

		evaluations.Should().BeEmpty();
	}

	[Fact]
	public void ForbiddenOperationAfter_RejectsAnOperationAfterTheConfiguredTerminalOperation()
	{
		var body = OperationPolicyTestFactory.GetBehavioralBodyAnalysis(
			"""
			public sealed class PizzaKitchen
			{
				public void Submit()
				{
					PizzaRepository.Commit();
					PizzaAudit.Record();
				}
			}

			public static class PizzaRepository { public static void Commit() { } }
			public static class PizzaAudit { public static void Record() { } }
			""",
			"Submit");
		var rule = CreateRule(
			BehavioralOperationRuleKind.ForbiddenOperationAfter,
			[CreateInvocationMatcher("PizzaAudit", "Record")],
			[CreateInvocationMatcher("PizzaRepository", "Commit")],
			BehavioralOperationOrdering.Dominance,
			0,
			"Audit after commit");
		var policy = new BehavioralOperationPolicy("Kitchen", [rule], null, "Architecture.anl", 1, 1);

		var evaluations = policy.Evaluate(body);

		evaluations.Should().ContainSingle();
		evaluations[0].ViolationKind.Should().Be(BehavioralOperationViolationKind.ForbiddenOperationAfter);
	}

	[Fact]
	public void MaximumOperationCount_RejectsEachOperationBeyondTheConfiguredLimit()
	{
		var body = OperationPolicyTestFactory.GetBehavioralBodyAnalysis(
			"""
			public sealed class PizzaKitchen
			{
				public void Submit()
				{
					PizzaPublisher.Publish();
					PizzaPublisher.Publish();
				}
			}

			public static class PizzaPublisher { public static void Publish() { } }
			""",
			"Submit");
		var rule = CreateRule(BehavioralOperationRuleKind.MaximumOperationCount, [CreateInvocationMatcher("PizzaPublisher", "Publish")], [], BehavioralOperationOrdering.Lexical, 1, "Publish");
		var policy = new BehavioralOperationPolicy("Kitchen", [rule], null, "Architecture.anl", 1, 1);

		var evaluations = policy.Evaluate(body);

		evaluations.Should().ContainSingle();
		evaluations[0].ViolationKind.Should().Be(BehavioralOperationViolationKind.MaximumOperationCountExceeded);
	}

	[Fact]
	public void DeclarationMatcher_LimitsAPolicyToTheSelectedMethod()
	{
		var body = OperationPolicyTestFactory.GetBehavioralBodyAnalysis(
			"""
			public sealed class PizzaKitchen
			{
				public void Submit() { }
				public void Other() { }
			}
			""",
			"Other");
		var rule = CreateRule(BehavioralOperationRuleKind.RequiredOperation, [CreateInvocationMatcher("PizzaValidator", "Validate")], [], BehavioralOperationOrdering.Lexical, 0, "Validation", "Submit");
		var policy = new BehavioralOperationPolicy("Kitchen", [rule], null, "Architecture.anl", 1, 1);

		var evaluations = policy.Evaluate(body);

		evaluations.Should().BeEmpty();
	}

	private static BehavioralOperationRule CreateRule(BehavioralOperationRuleKind kind, ImmutableArray<SemanticOperationMatcher> operationMatchers, ImmutableArray<SemanticOperationMatcher> relatedOperationMatchers, BehavioralOperationOrdering ordering, int maximumCount, string displayName, string declarationName = "Submit")
	{
		var declarationMatcher = new SemanticDeclarationMatcher(
			ImmutableArray<MatchCondition>.Empty,
			[new MatchCondition(MatchKind.Equals, declarationName, MatchOperand.Declaration)],
			ImmutableHashSet.Create(SemanticOperationMemberKind.Method));
		var result = new BehavioralOperationRule(
			kind,
			declarationMatcher,
			operationMatchers,
			relatedOperationMatchers,
			DependencySiteFilter.All,
			ordering,
			maximumCount,
			displayName,
			null,
			"Architecture.anl",
			1,
			1);

		return result;
	}

	private static SemanticOperationMatcher CreateInvocationMatcher(string containingTypeName, string memberName)
	{
		var result = new SemanticOperationMatcher(
			ImmutableHashSet.Create(SemanticOperationKind.Invocation),
			[new MatchCondition(MatchKind.Equals, containingTypeName)],
			[new MatchCondition(MatchKind.Equals, memberName, MatchOperand.Declaration)],
			true,
			ImmutableHashSet.Create(SemanticOperationMemberKind.Method));

		return result;
	}
}
