using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Matchers;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.Matchers.Declarations;
using RonSijm.AnaalIJzer.Core.Matchers.Observations;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ArchitectureHealthReportGenerator
{
	private static IReadOnlyList<HealthMatcherRule> GetMatcherRules(AnalyzerConfiguration config)
	{
		var rules = new List<HealthMatcherRule>();
		var items = config.Documentation.Items;
		var ancestors = new List<ArchitectureDocumentationItem>();
		for (var index = 0; index < items.Length; index++)
		{
			var item = items[index];
			while (ancestors.Count > item.Depth)
			{
				ancestors.RemoveAt(ancestors.Count - 1);
			}

			if (item.Kind is "Class" or "Namespace" or "Assembly"
			    && TryCreateMatcher(item, GetRequiredDeclarations(items, index), out var matcher))
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

	private static bool TryCreateMatcher(ArchitectureDocumentationItem item, ImmutableArray<DeclarationMatcher> requiredDeclarations, out PatternMatcher matcher)
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

		matcher = new PatternMatcher(target, conditions.ToImmutable(), requiredDeclarations);
		var result = conditions.Count > 0;

		return result;
	}

	private static ImmutableArray<DeclarationMatcher> GetRequiredDeclarations(ImmutableArray<ArchitectureDocumentationItem> items, int matcherIndex)
	{
		var parent = items[matcherIndex];
		var matchers = ImmutableArray.CreateBuilder<DeclarationMatcher>();
		for (var index = matcherIndex + 1; index < items.Length; index++)
		{
			var child = items[index];
			if (child.Depth <= parent.Depth)
			{
				break;
			}

			if (child.Depth != parent.Depth + 1 || !DeclarationMatchTargetParser.TryParse(child.Kind, out var target))
			{
				continue;
			}

			var conditions = ImmutableArray.CreateBuilder<MatchCondition>();
			TryAddCondition(child, "typeName", MatchKind.Equals, conditions, MatchOperand.AssociatedType);
			TryAddCondition(child, "exactName", MatchKind.Equals, conditions, MatchOperand.Declaration);
			TryAddCondition(child, "exactFullName", MatchKind.EqualsFullName, conditions, MatchOperand.AssociatedType);
			TryAddCondition(child, "inherits", MatchKind.Inherits, conditions, MatchOperand.AssociatedType);
			TryAddCondition(child, "implements", MatchKind.Implements, conditions, MatchOperand.AssociatedType);
			TryAddCondition(child, "withAttribute", MatchKind.HasAttribute, conditions, MatchOperand.Declaration);
			TryAddCondition(child, "withAccessModifier", MatchKind.HasAccessModifier, conditions, MatchOperand.Declaration);
			TryAddCondition(child, "typeKind", MatchKind.HasTypeKind, conditions, MatchOperand.AssociatedType);
			TryAddCondition(child, "endsWith", MatchKind.EndsWith, conditions, MatchOperand.Declaration);
			TryAddCondition(child, "startsWith", MatchKind.StartsWith, conditions, MatchOperand.Declaration);
			TryAddCondition(child, "contains", MatchKind.Contains, conditions, MatchOperand.Declaration);
			TryAddCondition(child, "regex", MatchKind.Regex, conditions, MatchOperand.Declaration);
			var requiredObservations = GetRequiredObservations(items, index);

			if (conditions.Count > 0)
			{
				matchers.Add(new DeclarationMatcher(target, conditions.ToImmutable(), requiredObservations));
			}
		}

		var result = matchers.ToImmutable();

		return result;
	}

	private static ImmutableArray<CodeObservationMatcher> GetRequiredObservations(ImmutableArray<ArchitectureDocumentationItem> items, int declarationMatcherIndex)
	{
		var parent = items[declarationMatcherIndex];
		var matchers = ImmutableArray.CreateBuilder<CodeObservationMatcher>();
		for (var index = declarationMatcherIndex + 1; index < items.Length; index++)
		{
			var child = items[index];
			if (child.Depth <= parent.Depth)
			{
				break;
			}

			if (child.Depth != parent.Depth + 1 || !CodeObservationMatchTargetParser.TryParse(child.Kind, out var target))
			{
				continue;
			}

			var conditions = ImmutableArray.CreateBuilder<MatchCondition>();
			TryAddCondition(child, "typeName", MatchKind.Equals, conditions, MatchOperand.AssociatedType);
			TryAddCondition(child, "exactName", MatchKind.Equals, conditions, MatchOperand.Declaration);
			TryAddCondition(child, "exactFullName", MatchKind.EqualsFullName, conditions, MatchOperand.AssociatedType);
			TryAddCondition(child, "endsWith", MatchKind.EndsWith, conditions, MatchOperand.Declaration);
			TryAddCondition(child, "startsWith", MatchKind.StartsWith, conditions, MatchOperand.Declaration);
			TryAddCondition(child, "contains", MatchKind.Contains, conditions, MatchOperand.Declaration);
			TryAddCondition(child, "regex", MatchKind.Regex, conditions, MatchOperand.Declaration);
			matchers.Add(new CodeObservationMatcher(target, conditions.ToImmutable()));
		}

		var result = matchers.ToImmutable();

		return result;
	}

	private static void TryAddCondition(ArchitectureDocumentationItem item, string attributeName, MatchKind kind, ImmutableArray<MatchCondition>.Builder conditions, MatchOperand operand = MatchOperand.Subject)
	{
		if (item.GetAttribute(attributeName) is not { } value)
		{
			return;
		}

		conditions.Add(new MatchCondition(kind, value, operand));
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

