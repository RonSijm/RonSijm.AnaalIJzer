using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Exceptions;
using RonSijm.AnaalIJzer.Core.LayerModel;

namespace RonSijm.AnaalIJzer.Core.PolicyEvaluation.Engine.DependencyRules;

public readonly partial struct LayerRegistry
{
	private LayerMatch? FindFlatRootExact(string typeName, string namespaceName, ITypeSymbol? symbol)
	{
		foreach (var root in _catalog.Roots)
		{
			if (root.Children.Length > 0)
			{
				continue;
			}

			var exact = FindOwnMatch(root, typeName, namespaceName, symbol, exactOnly: true);
			if (exact is { } match)
			{
				return CreateMatch(match.Rule, [root.Definition], [CreateMatcherMatch(match.Rule)], match.Result);
			}
		}

		return null;
	}

	private LayerMatch? FindNormal(string typeName, string namespaceName, ITypeSymbol? symbol)
	{
		foreach (var root in _catalog.Roots)
		{
			var result = FindInNode(root, ImmutableArray<LayerDefinition>.Empty, typeName, namespaceName, symbol);
			if (result is not null)
			{
				return result;
			}
		}

		return null;
	}

	private LayerMatch? FindForbidden(string typeName, string namespaceName, ITypeSymbol? symbol, bool exactOnly)
	{
		if (exactOnly && _catalog.ForbiddenTypeNames.TryGetValue(typeName, out var exact)
		    && !IsExcepted(exact.Exceptions, typeName, namespaceName, symbol))
		{
			return CreateMatch(exact, [exact.Layer], [CreateMatcherMatch(exact)], null);
		}

		if (exactOnly)
		{
			return null;
		}

		foreach (var (matcher, rule) in _catalog.ForbiddenMatchers)
		{
			var result = matcher.TryMatch(typeName, namespaceName, symbol);
			if (result is not null && !IsExcepted(rule.Exceptions, typeName, namespaceName, symbol))
			{
				return CreateMatch(rule, [rule.Layer], [CreateMatcherMatch(rule)], result);
			}
		}

		return null;
	}

	private static LayerMatch? FindInNode(LayerNode node, ImmutableArray<LayerDefinition> ancestors, string typeName, string namespaceName, ITypeSymbol? symbol, ImmutableArray<LayerMatcherMatch> ancestorMatcherMatches = default)
	{
		var scopeMatch = FindOwnMatch(node, typeName, namespaceName, symbol, exactOnly: false);
		if (node.HasMatchers && scopeMatch is null)
		{
			return null;
		}

		var layers = ancestors.Add(node.Definition);
		var matcherMatches = ancestorMatcherMatches.IsDefault ? ImmutableArray<LayerMatcherMatch>.Empty : ancestorMatcherMatches;
		if (scopeMatch is { } matchedScope)
		{
			matcherMatches = matcherMatches.Add(CreateMatcherMatch(matchedScope.Rule));
		}

		foreach (var child in node.Children)
		{
			var childMatch = FindInNode(child, layers, typeName, namespaceName, symbol, matcherMatches);
			if (childMatch is not null)
			{
				return childMatch;
			}
		}

		if (scopeMatch is not { } match)
		{
			return null;
		}

		return CreateMatch(match.Rule, layers, matcherMatches, match.Result);
	}

	private static (MatcherRule Rule, string Result)? FindOwnMatch(LayerNode node, string typeName, string namespaceName, ITypeSymbol? symbol, bool exactOnly)
	{
		foreach (var exactPass in new[] { true, false })
		{
			if (exactOnly && !exactPass)
			{
				continue;
			}

			foreach (var (matcher, rule) in node.Matchers)
			{
				var isExactTypeName = matcher.IsExactTypeName;
				if (isExactTypeName != exactPass)
				{
					continue;
				}

				var result = matcher.TryMatch(typeName, namespaceName, symbol);
				if (result is not null && !IsExcepted(rule.Exceptions, typeName, namespaceName, symbol))
				{
					return (rule, result);
				}
			}
		}

		return null;
	}

	private static LayerMatch CreateMatch(MatcherRule rule, ImmutableArray<LayerDefinition> layers, ImmutableArray<LayerMatcherMatch> matcherMatches, string? result)
	{
		var match = new LayerMatch(rule.Layer, layers, matcherMatches, string.IsNullOrEmpty(result) ? null : result, rule.XmlLineNumber, rule.XmlLinePosition, rule.XmlPath);

		return match;
	}

	private static LayerMatcherMatch CreateMatcherMatch(MatcherRule rule)
	{
		var result = new LayerMatcherMatch(rule.Layer, rule.XmlLineNumber, rule.XmlLinePosition, rule.XmlPath);

		return result;
	}

	private static bool IsExcepted(ImmutableArray<ExceptionMatcher> exceptions, string typeName, string namespaceName, ITypeSymbol? symbol)
	{
		if (exceptions.IsDefaultOrEmpty)
		{
			return false;
		}

		var deepestMatchingDepth = 0;
		foreach (var exception in exceptions)
		{
			var depth = exception.FindDeepestMatchingDepth(typeName, namespaceName, symbol, 1);
			if (depth > deepestMatchingDepth)
			{
				deepestMatchingDepth = depth;
			}
		}

		return deepestMatchingDepth % 2 == 1;
	}
}
