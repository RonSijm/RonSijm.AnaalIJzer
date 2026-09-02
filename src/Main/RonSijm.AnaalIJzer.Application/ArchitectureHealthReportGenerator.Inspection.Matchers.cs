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
		var profile = target == MatchTarget.TypeName
			? MatcherAttributeProfile.Type
			: MatcherAttributeProfile.NamespaceOrAssembly;
		var conditions = MatcherAttributeCatalog.CreateConditions(item.GetAttribute, profile);

		matcher = new PatternMatcher(target, conditions, requiredDeclarations);
		var result = conditions.Length > 0;

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

			var conditions = MatcherAttributeCatalog.CreateConditions(child.GetAttribute, MatcherAttributeProfile.Declaration);
			var requiredObservations = GetRequiredObservations(items, index);

			if (conditions.Length > 0)
			{
				matchers.Add(new DeclarationMatcher(target, conditions, requiredObservations));
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

			var conditions = MatcherAttributeCatalog.CreateConditions(
				child.GetAttribute,
				MatcherAttributeProfile.CodeObservation,
				target == CodeObservationMatchTarget.Literal);
			matchers.Add(new CodeObservationMatcher(target, conditions));
		}

		var result = matchers.ToImmutable();

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

