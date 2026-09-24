using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.NameRules;

public readonly struct NameRulePolicy(
    ImmutableArray<NameMatchingRule> rules)
{
    public ImmutableArray<NameMatchingRule> Rules { get; } = rules;

    public bool HasIntraProceduralRules { get; } = rules.Any(rule => rule.ValueTracking == NameRuleValueTrackingMode.IntraProcedural);

    public NameRuleViolation? Evaluate(NameRuleTrigger trigger, NameRuleSubject source, NameRuleSubject target, string site, NameRuleValueTrackingMode? valueTracking = null)
    {
        foreach (var rule in Rules)
        {
            if (rule.Trigger != trigger)
            {
                continue;
            }

            if (valueTracking.HasValue && rule.ValueTracking != valueTracking.Value)
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