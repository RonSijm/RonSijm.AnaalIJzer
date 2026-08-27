using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.Core.Matchers;

namespace RonSijm.AnaalIJzer.Core.NameRules.Tests.NameRules;

public sealed class NameRulePolicyTests
{
	[Fact]
	public void Evaluate_ReturnsTheFirstViolationForTheMatchingTrigger()
	{
		var firstRule = CreateRule(
			NameRuleKind.RequireMatchingNames,
			NameRuleTrigger.ValueMovement,
			layerName: "Application");
		var secondRule = CreateRule(
			NameRuleKind.RequireMatchingNames,
			NameRuleTrigger.ValueMovement,
			layerName: "Application/Inner");
		var policy = new NameRulePolicy([firstRule, secondRule]);
		var source = CreateValueSubject("patientId");
		var target = CreateValueSubject("doctorId");

		var result = policy.Evaluate(NameRuleTrigger.ValueMovement, source, target, DependencySites.Method);

		result.Should().NotBeNull();
		result.Value.LayerName.Should().Be("Application");
	}

	[Fact]
	public void Evaluate_IgnoresRulesForOtherTriggers()
	{
		var rule = CreateRule(
			NameRuleKind.RequireMatchingNames,
			NameRuleTrigger.Declaration,
			layerName: "Application");
		var policy = new NameRulePolicy([rule]);
		var source = CreateValueSubject("patientId");
		var target = CreateValueSubject("doctorId");

		var result = policy.Evaluate(NameRuleTrigger.ValueMovement, source, target, DependencySites.Method);

		result.Should().BeNull();
	}

	[Fact]
	public void Evaluate_ReturnsNullWhenTheComparedNamesAlreadyMatch()
	{
		var rule = CreateRule(
			NameRuleKind.RequireMatchingNames,
			NameRuleTrigger.ValueMovement,
			layerName: "Application");
		var policy = new NameRulePolicy([rule]);
		var source = CreateValueSubject("patientId");
		var target = CreateValueSubject("patientId");

		var result = policy.Evaluate(NameRuleTrigger.ValueMovement, source, target, DependencySites.Method);

		result.Should().BeNull();
	}

	private static NameMatchingRule CreateRule(NameRuleKind kind, NameRuleTrigger trigger, string layerName)
	{
		var result = new NameMatchingRule(
			kind,
			trigger,
			ImmutableArray<PatternMatcher>.Empty,
			ImmutableArray<PatternMatcher>.Empty,
			ImmutableArray<PatternMatcher>.Empty,
			ImmutableArray<NameRuleAllowMapping>.Empty,
			DependencySiteFilter.All,
			layerName,
			null,
			"Architecture.anl",
			12,
			3);

		return result;
	}

	private static NameRuleSubject CreateValueSubject(string name)
	{
		var result = new NameRuleSubject(name, [name], symbol: null);

		return result;
	}
}
