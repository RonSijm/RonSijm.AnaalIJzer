using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Engine.NameRules;

namespace RonSijm.AnaalIJzer.Config.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static ImmutableArray<NameMatchingRule> ParseNameRules(IEnumerable<XElement> containers, string layerName, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var rules = ImmutableArray.CreateBuilder<NameMatchingRule>();
		foreach (var container in containers)
		{
			foreach (var ruleElement in container.Elements())
			{
				switch (ruleElement.Name.LocalName)
				{
					case "RequireMatchingNames":
						TryParseNameRule(ruleElement, NameRuleKind.RequireMatchingNames, NameRuleTrigger.ValueMovement, "Source", "Target", layerName, xmlPath, issues, rules);
						break;
					case "RequireDeclarationNameMatchesType":
						TryParseNameRule(ruleElement, NameRuleKind.RequireDeclarationNameMatchesType, NameRuleTrigger.Declaration, "Type", "Name", layerName, xmlPath, issues, rules);
						break;
				}
			}
		}

		return rules.ToImmutable();
	}

	private static void TryParseNameRule(XElement ruleElement, NameRuleKind kind, NameRuleTrigger trigger, string sourceElementName, string targetElementName, string layerName, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, ImmutableArray<NameMatchingRule>.Builder rules)
	{
		if (!TryReadSiteFilter(ruleElement, out var siteFilter, out var siteFilterError))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, siteFilterError, ruleElement, xmlPath);
			return;
		}

		var nameMatchers = kind == NameRuleKind.RequireMatchingNames
			? ParseNameMatcherElements(ruleElement.Elements("Name"))
			: ImmutableArray<PatternMatcher>.Empty;
		var sourceMatchers = ParseNameMatcherElements(ruleElement.Elements(sourceElementName));
		var targetMatchers = ParseNameMatcherElements(ruleElement.Elements(targetElementName));
		var allowMappings = ParseNameRuleAllowMappings(ruleElement.Elements("Allow"), sourceElementName, targetElementName, xmlPath, issues);
		var line = (IXmlLineInfo)ruleElement;
		rules.Add(new NameMatchingRule(kind, trigger, nameMatchers, sourceMatchers, targetMatchers, allowMappings, siteFilter, layerName, ruleElement.Attribute("description")?.Value, xmlPath, line.LineNumber, line.LinePosition));
	}

	private static ImmutableArray<NameRuleAllowMapping> ParseNameRuleAllowMappings(IEnumerable<XElement> elements, string sourceElementName, string targetElementName, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var mappings = ImmutableArray.CreateBuilder<NameRuleAllowMapping>();
		foreach (var element in elements)
		{
			if (!TryReadSiteFilter(element, out var siteFilter, out var siteFilterError))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, siteFilterError, element, xmlPath);
				continue;
			}

			var sourceMatchers = ParseNameMatcherElements(element.Elements(sourceElementName));
			var targetMatchers = ParseNameMatcherElements(element.Elements(targetElementName));
			if (element.Attribute("from")?.Value is { } from)
			{
				sourceMatchers = sourceMatchers.Add(new PatternMatcher(MatchTarget.TypeName, MatchKind.Equals, from));
			}

			if (element.Attribute("to")?.Value is { } to)
			{
				targetMatchers = targetMatchers.Add(new PatternMatcher(MatchTarget.TypeName, MatchKind.Equals, to));
			}

			if (sourceMatchers.IsDefaultOrEmpty || targetMatchers.IsDefaultOrEmpty)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"NameRules Allow requires both a {sourceElementName} matcher and a {targetElementName} matcher.", element, xmlPath);
				continue;
			}

			mappings.Add(new NameRuleAllowMapping(sourceMatchers, targetMatchers, siteFilter, element.Attribute("description")?.Value));
		}

		return mappings.ToImmutable();
	}

	private static ImmutableArray<PatternMatcher> ParseNameMatcherElements(IEnumerable<XElement> elements)
	{
		var matchers = ImmutableArray.CreateBuilder<PatternMatcher>();
		foreach (var element in elements)
		{
			if (ArchitectureConfigurationMatcherReader.TryReadMatcher(element, MatchTarget.TypeName, out var matcher))
			{
				matchers.Add(matcher);
			}
		}

		return matchers.ToImmutable();
	}
}

