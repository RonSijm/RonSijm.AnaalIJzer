using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.Model;
using RonSijm.AnaalIJzer.Engine.LayerModel;

namespace RonSijm.AnaalIJzer.Engine.DependencyRules;

public readonly partial struct LayerRegistry
{
	private bool TryFindGlobalForbiddenMatch(string typeName, string namespaceName, ITypeSymbol? symbol, out MatcherRule rule, out string? matchedSuffix)
	{
		if (catalog.ForbiddenTypeNames.TryGetValue(typeName, out rule)
		    && !IsExcepted(rule.Exceptions, typeName, namespaceName, symbol))
		{
			matchedSuffix = null;
			return true;
		}

		return TryFindPolicyMatch(catalog.ForbiddenMatchers, typeName, namespaceName, symbol, out rule, out matchedSuffix);
	}

	private static bool MatchesAnyPolicy(ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> matchers, string typeName, string namespaceName, ITypeSymbol? symbol)
	{
		var result = TryFindPolicyMatch(matchers, typeName, namespaceName, symbol, out _, out _);

		return result;
	}

	private static bool TryFindPolicyMatch(ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> matchers, string typeName, string namespaceName, ITypeSymbol? symbol, out MatcherRule matchedRule, out string? matchedSuffix)
	{
		foreach (var (matcher, rule) in matchers)
		{
			var result = matcher.TryMatch(typeName, namespaceName, symbol);
			if (result is not null && !IsExcepted(rule.Exceptions, typeName, namespaceName, symbol))
			{
				matchedRule = rule;
				matchedSuffix = string.IsNullOrEmpty(result) ? null : result;
				return true;
			}
		}

		matchedRule = default;
		matchedSuffix = null;
		return false;
	}

	private static TypePolicyViolation CreateForbiddenViolation(MatcherRule rule, string? matchedSuffix, string dependencyLayerName, string scope)
	{
		var reason = scope == "global"
			? "the type matches a global <Forbidden> rule"
			: $"the type matches a <Forbidden> rule scoped to {scope}";
		if (!string.IsNullOrWhiteSpace(rule.Layer.Comment))
		{
			reason += $": {rule.Layer.Comment}";
		}

		return new TypePolicyViolation(reason, dependencyLayerName, rule.Layer.Comment, rule, matchedSuffix);
	}
}
