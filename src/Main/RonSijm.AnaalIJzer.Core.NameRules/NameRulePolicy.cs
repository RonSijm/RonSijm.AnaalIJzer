using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.NameRules;

public readonly struct NameRulePolicy(
	ImmutableArray<NameMatchingRule> rules)
{
	public ImmutableArray<NameMatchingRule> Rules { get; } = rules;

	public NameRuleViolation? Evaluate(NameRuleTrigger trigger, NameRuleSubject source, NameRuleSubject target, string site)
	{
		foreach (var rule in Rules)
		{
			if (rule.Trigger != trigger)
			{
				continue;
			}

			var result = rule.Evaluate(source, target, site);
			if (result is not null)
			{
				return result;
			}
		}

		return null;
	}
}
