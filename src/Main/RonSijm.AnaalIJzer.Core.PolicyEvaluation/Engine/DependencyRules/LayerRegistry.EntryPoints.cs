using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.EntryPoints;
using RonSijm.AnaalIJzer.Core.LayerModel;

namespace RonSijm.AnaalIJzer.Core.PolicyEvaluation.Engine.DependencyRules;

public readonly partial struct LayerRegistry
{
	private static BoundaryEntryPointEvaluation EvaluateBoundaryPolicy(BoundaryEntryPointPolicy policy, LayerMatch dependencyMatch, string dependencyTypeName, string dependencyNamespace, ITypeSymbol dependencyType, string site)
	{
		BoundaryEntryPointRule? siteFilteredRule = null;
		foreach (var rule in policy.Rules)
		{
			if (!rule.Selector.Matches(dependencyMatch.Layer.Name, dependencyTypeName, dependencyNamespace, dependencyType))
			{
				continue;
			}

			if (rule.SiteFilter.Allows(site))
			{
				return BoundaryEntryPointEvaluation.Allowed;
			}

			siteFilteredRule ??= rule;
		}

		if (siteFilteredRule is { } matchingRule)
		{
			return BoundaryEntryPointEvaluation.Denied(policy, $"the matching entry point does not allow site {site}", matchingRule.ToDisplayText(), matchingRule);
		}

		var permittedEntries = policy.Rules.IsDefaultOrEmpty
			? "no valid entry points"
			: string.Join(", ", policy.Rules.Select(rule => rule.ToDisplayText()));
		var reason = $"the boundary permits entry only through {permittedEntries}";
		var result = BoundaryEntryPointEvaluation.Denied(policy, reason, null);

		return result;
	}
}
