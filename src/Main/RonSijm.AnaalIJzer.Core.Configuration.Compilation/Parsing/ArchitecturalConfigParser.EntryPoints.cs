using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Engine.EntryPoints;
using RonSijm.AnaalIJzer.Exceptions;
using RonSijm.AnaalIJzer.Engine.LayerModel;

namespace RonSijm.AnaalIJzer.Config.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static ImmutableArray<BoundaryEntryPointPolicy> ParseBoundaryEntryPointPolicies(IEnumerable<XElement> containers, string ownerLayerPath, string xmlPath, IReadOnlyDictionary<string, LayerNode> nodesByPath, ArchitectureExceptionPolicy exceptionPolicy, ImmutableArray<ArchitectureExceptionDefinition>.Builder exceptionDefinitions, ImmutableArray<ArchitectureExceptionReview>.Builder exceptionReviews, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var policies = ImmutableArray.CreateBuilder<BoundaryEntryPointPolicy>();
		foreach (var container in containers)
		{
			var rules = ImmutableArray.CreateBuilder<BoundaryEntryPointRule>();
			foreach (var entryPointElement in container.Elements("EntryPoint"))
			{
				if (!TryParseBoundaryEntryPointRule(entryPointElement, ownerLayerPath, xmlPath, nodesByPath, exceptionPolicy, exceptionDefinitions, exceptionReviews, issues, out var rule))
				{
					continue;
				}

				rules.Add(rule);
			}

			if (rules.Count == 0)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Layer '{ownerLayerPath}' declares EntryPoints but no valid EntryPoint entries were found.", container, xmlPath);
			}

			var lineInfo = (IXmlLineInfo)container;
			var policy = new BoundaryEntryPointPolicy(
				ownerLayerPath,
				rules.ToImmutable(),
				container.Attribute("description")?.Value,
				xmlPath,
				lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0,
				lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0);
			policies.Add(policy);
		}

		var result = policies.ToImmutable();

		return result;
	}

	private static bool TryParseBoundaryEntryPointRule(XElement element, string ownerLayerPath, string xmlPath, IReadOnlyDictionary<string, LayerNode> nodesByPath, ArchitectureExceptionPolicy exceptionPolicy, ImmutableArray<ArchitectureExceptionDefinition>.Builder exceptionDefinitions, ImmutableArray<ArchitectureExceptionReview>.Builder exceptionReviews, ImmutableArray<ConfigurationIssue>.Builder issues, out BoundaryEntryPointRule rule)
	{
		if (!TryReadSiteFilter(element, out var siteFilter, out var siteFilterError))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, siteFilterError, element, xmlPath);
			rule = default;
			return false;
		}

		var layerReference = element.Attribute("layer")?.Value;
		var matcherElements = element.Elements().Where(child => child.Name.LocalName is "Class" or "Namespace" or "Assembly").ToArray();
		var hasLayerSelector = !string.IsNullOrWhiteSpace(layerReference);
		var hasMatcherSelector = matcherElements.Length > 0;
		if (hasLayerSelector == hasMatcherSelector)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "EntryPoint must use exactly one selector form: layer=\"...\" or matcher elements.", element, xmlPath);
			rule = default;
			return false;
		}

		BoundaryEntryPointSelector selector;
		if (hasLayerSelector)
		{
			if (!TryResolveLayerReference(layerReference!, ownerLayerPath, nodesByPath, out var resolvedLayer, out var error))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"EntryPoint layer {error}", element, xmlPath);
				rule = default;
				return false;
			}

			if (!BoundaryEntryPointSelector.IsContainedInBoundary(ownerLayerPath, resolvedLayer))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"EntryPoint layer '{layerReference}' must resolve to layer '{ownerLayerPath}' or one of its descendants.", element, xmlPath);
				rule = default;
				return false;
			}

			selector = new BoundaryEntryPointSelector(resolvedLayer, ImmutableArray<BoundaryEntryPointMatcher>.Empty);
		}
		else
		{
			var matchers = ImmutableArray.CreateBuilder<BoundaryEntryPointMatcher>();
			foreach (var matcherElement in matcherElements)
			{
				var target = matcherElement.Name.LocalName switch
				{
					"Namespace" => MatchTarget.Namespace,
					"Assembly" => MatchTarget.Assembly,
					_ => MatchTarget.TypeName
				};

				if (!ArchitectureConfigurationMatcherReader.TryReadMatcher(matcherElement, target, out var matcher))
				{
					AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "EntryPoint matcher element does not declare a valid matcher attribute.", matcherElement, xmlPath);
					continue;
				}

				var displayText = BuildEntryPointMatcherDisplayText(matcherElement);
				matchers.Add(new BoundaryEntryPointMatcher(matcher, ParseExceptions(matcherElement, xmlPath, ownerLayerPath, exceptionPolicy, exceptionDefinitions, exceptionReviews), displayText));
			}

			if (matchers.Count == 0)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "EntryPoint matcher selector must contain at least one valid matcher element.", element, xmlPath);
				rule = default;
				return false;
			}

			selector = new BoundaryEntryPointSelector(null, matchers.ToImmutable());
		}

		var lineInfo = (IXmlLineInfo)element;
		rule = new BoundaryEntryPointRule(
			selector,
			siteFilter,
			element.Attribute("description")?.Value,
			xmlPath,
			lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0,
			lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0);
		return true;
	}

	private static string BuildEntryPointMatcherDisplayText(XElement element)
	{
		var matcherDisplayName = ArchitectureConfigurationMatcherReader.GetMatcherDisplayName(element);
		var separator = string.IsNullOrEmpty(matcherDisplayName) ? string.Empty : " ";
		var result = "<" + element.Name.LocalName + separator + matcherDisplayName + " />";

		return result;
	}
}

