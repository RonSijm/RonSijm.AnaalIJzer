using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Core.NameRules;

namespace RonSijm.AnaalIJzer.Core.PolicyEvaluation.Engine.DependencyRules;

public readonly partial struct LayerRegistry
{
	public NameRuleViolation? EvaluateNameRules(LayerMatch layerMatch, NameRuleTrigger trigger, NameRuleSubject source, NameRuleSubject target, string site)
	{
		var policy = GetNameRulePolicy(layerMatch);
		var result = policy.Evaluate(trigger, source, target, site);

		return result;
	}

	private NameRulePolicy GetNameRulePolicy(LayerMatch layerMatch)
	{
		var rules = ImmutableArray.CreateBuilder<NameMatchingRule>();

		foreach (var layer in layerMatch.Layers)
		{
			if (!_catalog.NodesByPath.TryGetValue(layer.Name, out var node) || node.NameRules.IsDefaultOrEmpty)
			{
				continue;
			}

			rules.AddRange(node.NameRules);
		}

		var result = new NameRulePolicy(rules.ToImmutable());

		return result;
	}
}
