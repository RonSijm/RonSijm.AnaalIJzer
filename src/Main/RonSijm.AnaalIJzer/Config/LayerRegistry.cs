using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Matching;

namespace RonSijm.AnaalIJzer.Config;

/// <summary>
///     Owns the two-tier type-to-layer lookup (exact-name fast path + pattern list)
///     together with the <see cref="FindLayer" /> and exception-evaluation logic
///     that was previously inlined on <see cref="AnalyzerConfig" />.
/// </summary>
internal readonly struct LayerRegistry
{
	private readonly ImmutableArray<LayerNode> roots;
	private readonly IReadOnlyDictionary<string, LayerNode> nodesByPath;
	private readonly IReadOnlyDictionary<string, MatcherRule> forbiddenTypeNames;
	private readonly ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> forbiddenMatchers;
	private readonly ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> allowedTypeMatchers;

	public LayerRegistry(ImmutableArray<LayerNode> roots, IReadOnlyDictionary<string, LayerNode> nodesByPath, IReadOnlyDictionary<string, MatcherRule> forbiddenTypeNames, ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> forbiddenMatchers, ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> allowedTypeMatchers)
	{
		this.roots = roots;
		this.nodesByPath = nodesByPath;
		this.forbiddenTypeNames = forbiddenTypeNames;
		this.forbiddenMatchers = forbiddenMatchers;
		this.allowedTypeMatchers = allowedTypeMatchers;
	}

	public bool HasLayers => !roots.IsDefaultOrEmpty;

	/// <summary>
	///     Finds the layer for a type.
	///     Exact <c>typeName=</c> / <c>exactName=</c> matches take precedence over pattern
	///     and semantic matches. Pattern matches are evaluated in document order.
	///     Pass <paramref name="symbol" /> to enable semantic matchers
	///     (<c>inherits</c>, <c>implements</c>, <c>withAttribute</c>, <c>withAccessModifier</c>);
	///     omitting it limits matching to the string-based attributes.
	/// </summary>
	public LayerMatch? FindLayer(string typeName, string namespaceName, ITypeSymbol? symbol = null)
	{
		var match = FindFlatRootExact(typeName, namespaceName, symbol);
		if (match is not null)
		{
			return match;
		}

		match = FindForbidden(typeName, namespaceName, symbol, exactOnly: true);
		if (match is not null)
		{
			return match;
		}

		return FindNormal(typeName, namespaceName, symbol)
		       ?? FindForbidden(typeName, namespaceName, symbol, exactOnly: false);
	}

	public TypePolicyViolation? EvaluateTypePolicy(LayerMatch layerMatch, string typeName, string namespaceName, ITypeSymbol? symbol)
	{
		if (layerMatch.Layer.IsForbidden)
		{
			return null;
		}

		if (TryFindGlobalForbiddenMatch(typeName, namespaceName, symbol, out var globalForbiddenRule, out var globalForbiddenSuffix))
		{
			return CreateForbiddenViolation(globalForbiddenRule, globalForbiddenSuffix, layerMatch.Layer.Name, "global");
		}

		foreach (var layer in layerMatch.Layers)
		{
			if (nodesByPath.TryGetValue(layer.Name, out var node)
			    && TryFindPolicyMatch(node.ForbiddenTypeMatchers, typeName, namespaceName, symbol, out var rule, out var suffix))
			{
				return CreateForbiddenViolation(rule, suffix, layerMatch.Layer.Name, $"layer '{layer.Name}'");
			}
		}

		if (!allowedTypeMatchers.IsDefaultOrEmpty
		    && !MatchesAnyPolicy(allowedTypeMatchers, typeName, namespaceName, symbol))
		{
			return new TypePolicyViolation("the global <Allowed> list has no matching rule", layerMatch.Layer.Name, null, null, null);
		}

		foreach (var layer in layerMatch.Layers)
		{
			if (nodesByPath.TryGetValue(layer.Name, out var node)
			    && !node.AllowedTypeMatchers.IsDefaultOrEmpty
			    && !MatchesAnyPolicy(node.AllowedTypeMatchers, typeName, namespaceName, symbol))
			{
				return new TypePolicyViolation($"the <Allowed> list scoped to layer '{layer.Name}' has no matching rule", layerMatch.Layer.Name, null, null, null);
			}
		}

		return null;
	}

	private LayerMatch? FindFlatRootExact(string typeName, string namespaceName, ITypeSymbol? symbol)
	{
		foreach (var root in roots)
		{
			if (root.Children.Length > 0)
			{
				continue;
			}

			var exact = FindOwnMatch(root, typeName, namespaceName, symbol, exactOnly: true);
			if (exact is { } match)
			{
				return CreateMatch(match.Rule, ImmutableArray.Create(root.Definition), ImmutableArray.Create(CreateMatcherMatch(match.Rule)), match.Result);
			}
		}

		return null;
	}

	private LayerMatch? FindNormal(string typeName, string namespaceName, ITypeSymbol? symbol)
	{
		foreach (var root in roots)
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
		if (exactOnly && forbiddenTypeNames.TryGetValue(typeName, out var exact)
		    && !IsExcepted(exact.Exceptions, typeName, namespaceName, symbol))
		{
			return CreateMatch(exact, ImmutableArray.Create(exact.Layer), ImmutableArray.Create(CreateMatcherMatch(exact)), null);
		}

		if (exactOnly)
		{
			return null;
		}

		foreach (var (matcher, rule) in forbiddenMatchers)
		{
			var result = matcher.TryMatch(typeName, namespaceName, symbol);
			if (result is not null && !IsExcepted(rule.Exceptions, typeName, namespaceName, symbol))
			{
				return CreateMatch(rule, ImmutableArray.Create(rule.Layer), ImmutableArray.Create(CreateMatcherMatch(rule)), result);
			}
		}

		return null;
	}

	private bool TryFindGlobalForbiddenMatch(string typeName, string namespaceName, ITypeSymbol? symbol, out MatcherRule rule, out string? matchedSuffix)
	{
		if (forbiddenTypeNames.TryGetValue(typeName, out rule)
		    && !IsExcepted(rule.Exceptions, typeName, namespaceName, symbol))
		{
			matchedSuffix = null;
			return true;
		}

		return TryFindPolicyMatch(forbiddenMatchers, typeName, namespaceName, symbol, out rule, out matchedSuffix);
	}

	private static bool MatchesAnyPolicy(ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> matchers, string typeName, string namespaceName, ITypeSymbol? symbol) =>
		TryFindPolicyMatch(matchers, typeName, namespaceName, symbol, out _, out _);

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
				var isExactTypeName = matcher.Target == MatchTarget.TypeName && matcher.Kind == MatchKind.Equals;
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

	private static LayerMatch CreateMatch(MatcherRule rule, ImmutableArray<LayerDefinition> layers, ImmutableArray<LayerMatcherMatch> matcherMatches, string? result) =>
		new(rule.Layer, layers, matcherMatches, string.IsNullOrEmpty(result) ? null : result, rule.XmlLineNumber, rule.XmlLinePosition, rule.XmlPath);

	private static LayerMatcherMatch CreateMatcherMatch(MatcherRule rule) =>
		new(rule.Layer, rule.XmlLineNumber, rule.XmlLinePosition, rule.XmlPath);

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
