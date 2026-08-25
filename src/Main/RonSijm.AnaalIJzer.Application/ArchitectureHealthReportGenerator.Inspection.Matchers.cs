using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.Model;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ArchitectureHealthReportGenerator
{
	private static IReadOnlyList<HealthMatcherRule> GetMatcherRules(AnalyzerConfiguration config)
	{
		var rules = new List<HealthMatcherRule>();
		var ancestors = new List<ArchitectureDocumentationItem>();
		foreach (var item in config.Documentation.Items)
		{
			while (ancestors.Count > item.Depth)
			{
				ancestors.RemoveAt(ancestors.Count - 1);
			}

			if (item.Kind is "Class" or "Namespace" or "Assembly" && TryCreateMatcher(item, out var matcher))
			{
				var parent = ancestors.LastOrDefault(ancestor => ancestor.Kind is "Layer" or "Allowed" or "Forbidden");
				var isException = ancestors.Any(ancestor => ancestor.Kind == "Exceptions");
				rules.Add(new HealthMatcherRule(item, parent.Kind, parent.Label, matcher, isException));
			}

			if (ancestors.Count == item.Depth)
			{
				ancestors.Add(item);
			}
			else
			{
				ancestors[item.Depth] = item;
			}
		}

		return rules;
	}

	private static bool TryCreateMatcher(ArchitectureDocumentationItem item, out PatternMatcher matcher)
	{
		var target = item.Kind switch
		{
			"Namespace" => MatchTarget.Namespace,
			"Assembly" => MatchTarget.Assembly,
			_ => MatchTarget.TypeName
		};
		var candidates = new (string Attribute, MatchKind Kind)[]
		{
			("typeName", MatchKind.Equals),
			("exactName", MatchKind.Equals),
			("exactFullName", MatchKind.EqualsFullName),
			("inherits", MatchKind.Inherits),
			("implements", MatchKind.Implements),
			("withAttribute", MatchKind.HasAttribute),
			("withAccessModifier", MatchKind.HasAccessModifier),
			("typeKind", MatchKind.HasTypeKind),
			("endsWith", MatchKind.EndsWith),
			("startsWith", MatchKind.StartsWith),
			("contains", MatchKind.Contains),
			("regex", MatchKind.Regex)
		};
		var conditions = ImmutableArray.CreateBuilder<MatchCondition>();
		foreach (var candidate in candidates)
		{
			if (item.GetAttribute(candidate.Attribute) is { } value)
			{
				conditions.Add(new MatchCondition(candidate.Kind, value));
			}
		}

		matcher = new PatternMatcher(target, conditions.ToImmutable());
		var result = conditions.Count > 0;

		return result;
	}

	private static bool RuleMatches(HealthMatcherRule rule, INamedTypeSymbol type)
	{
		var result = rule.Matcher.TryMatch(type.Name, GetNamespace(type), type) is not null;

		return result;
	}

	private static bool HasDivergentPaths(IReadOnlyList<string> paths)
	{
		for (var left = 0; left < paths.Count; left++)
		{
			for (var right = left + 1; right < paths.Count; right++)
			{
				if (!IsAncestor(paths[left], paths[right]) && !IsAncestor(paths[right], paths[left]))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static bool IsAncestor(string ancestor, string descendant)
	{
		var result = descendant == ancestor || descendant.StartsWith(ancestor + "/", StringComparison.Ordinal);

		return result;
	}

	private sealed record HealthMatcherRule(ArchitectureDocumentationItem Item, string ParentKind, string ParentLabel, PatternMatcher Matcher, bool IsException);
}

