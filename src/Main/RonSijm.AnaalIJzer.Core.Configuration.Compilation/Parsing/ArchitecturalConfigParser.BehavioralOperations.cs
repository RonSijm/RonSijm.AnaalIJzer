using System.Collections.Immutable;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.OperationPolicies.Behavioral;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Matching;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Model;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static ImmutableArray<BehavioralOperationPolicy> ParseBehavioralOperationPolicies(IEnumerable<XElement> containers, string ownerLayerPath, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var policies = ImmutableArray.CreateBuilder<BehavioralOperationPolicy>();
		foreach (var container in containers)
		{
			if (HasUnexpectedAttributes(container, "description", "comment"))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "BehavioralOperations supports only description and comment attributes.", container, xmlPath);
				continue;
			}

			var rules = ImmutableArray.CreateBuilder<BehavioralOperationRule>();
			foreach (var child in container.Elements())
			{
				if (!TryParseBehavioralOperationRule(child, xmlPath, issues, out var rule))
				{
					continue;
				}

				rules.Add(rule);
			}

			if (rules.Count == 0)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"BehavioralOperations in layer '{ownerLayerPath}' requires at least one valid behavioral operation rule.", container, xmlPath);
				continue;
			}

			var line = (IXmlLineInfo)container;
			var policy = new BehavioralOperationPolicy(
				ownerLayerPath,
				rules.ToImmutable(),
				container.Attribute("description")?.Value,
				xmlPath,
				line.HasLineInfo() ? line.LineNumber : 0,
				line.HasLineInfo() ? line.LinePosition : 0);
			policies.Add(policy);
		}

		var result = policies.ToImmutable();

		return result;
	}

	private static bool TryParseBehavioralOperationRule(XElement element, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out BehavioralOperationRule rule)
	{
		if (!TryGetBehavioralOperationRuleKind(element.Name.LocalName, out var kind))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "BehavioralOperations supports only RequiredOperation, RequiredOperationBefore, ForbiddenOperationAfter, and MaximumOperationCount children.", element, xmlPath);
			rule = default;

			return false;
		}

		var supportsOrdering = kind is not BehavioralOperationRuleKind.MaximumOperationCount;
		var requiresRelatedOperations = kind is BehavioralOperationRuleKind.RequiredOperationBefore or BehavioralOperationRuleKind.ForbiddenOperationAfter;
		var relatedContainerName = kind == BehavioralOperationRuleKind.RequiredOperationBefore ? "BeforeOperation" : "AfterOperation";
		if (!HasValidBehavioralOperationRuleAttributes(element, supportsOrdering, kind == BehavioralOperationRuleKind.MaximumOperationCount))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, CreateBehavioralOperationRuleAttributeMessage(element.Name.LocalName), element, xmlPath);
			rule = default;

			return false;
		}

		if (!TryReadSiteFilter(element, out var siteFilter, out var siteFilterError))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, siteFilterError, element, xmlPath);
			rule = default;

			return false;
		}

		if (!TryParseBehavioralOperationOrdering(element, supportsOrdering, xmlPath, issues, out var ordering)
			|| !TryParseBehavioralMaximumCount(element, kind, xmlPath, issues, out var maximumCount)
			|| !TryParseBehavioralDeclarationMatcher(element, xmlPath, issues, out var declarationMatcher)
			|| !TryParseBehavioralOperationMatchers(element.Elements("OperationMatcher"), element, "OperationMatcher", xmlPath, issues, out var operationMatchers)
			|| requiresRelatedOperations && !TryParseBehavioralRelatedOperationMatchers(element, relatedContainerName, xmlPath, issues, out var relatedOperationMatchers))
		{
			rule = default;

			return false;
		}

		if (!requiresRelatedOperations)
		{
			relatedOperationMatchers = ImmutableArray<SemanticOperationMatcher>.Empty;
		}

		if (!HasOnlyBehavioralOperationRuleChildren(element, requiresRelatedOperations ? relatedContainerName : null))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, CreateBehavioralOperationRuleChildMessage(element.Name.LocalName, requiresRelatedOperations ? relatedContainerName : null), element, xmlPath);
			rule = default;

			return false;
		}

		var line = (IXmlLineInfo)element;
		rule = new BehavioralOperationRule(
			kind,
			declarationMatcher,
			operationMatchers,
			relatedOperationMatchers,
			siteFilter,
			ordering,
			maximumCount,
			CreateBehavioralOperationRuleDisplayName(element, kind),
			element.Attribute("description")?.Value,
			xmlPath,
			line.HasLineInfo() ? line.LineNumber : 0,
			line.HasLineInfo() ? line.LinePosition : 0);

		return true;
	}

	private static bool TryParseBehavioralDeclarationMatcher(XElement ruleElement, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out SemanticDeclarationMatcher matcher)
	{
		var declarations = ruleElement.Elements("DeclarationMatcher").ToArray();
		if (declarations.Length != 1)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, ruleElement.Name.LocalName + " requires exactly one DeclarationMatcher.", ruleElement, xmlPath);
			matcher = default;

			return false;
		}

		var declaration = declarations[0];
		if (HasUnexpectedAttributes(declaration, "description", "comment"))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "DeclarationMatcher supports only description and comment attributes.", declaration, xmlPath);
			matcher = default;

			return false;
		}

		var unexpectedChildren = declaration.Elements().Where(child => child.Name.LocalName is not ("ContainingType" or "Member")).ToArray();
		if (unexpectedChildren.Length > 0)
		{
			foreach (var child in unexpectedChildren)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "DeclarationMatcher supports only ContainingType and Member children.", child, xmlPath);
			}

			matcher = default;

			return false;
		}

		var containingTypes = declaration.Elements("ContainingType").ToArray();
		var members = declaration.Elements("Member").ToArray();
		if (containingTypes.Length > 1 || members.Length > 1)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "DeclarationMatcher permits at most one ContainingType and one Member matcher.", declaration, xmlPath);
			matcher = default;

			return false;
		}

		if (!TryParseOperationMatcherConditions(containingTypes.SingleOrDefault(), MatcherAttributeProfile.Type, false, "ContainingType", xmlPath, issues, out var containingTypeConditions, out _)
			|| !TryParseOperationMatcherConditions(members.SingleOrDefault(), MatcherAttributeProfile.SemanticCodeObservation, true, "Member", xmlPath, issues, out var memberConditions, out var memberKinds))
		{
			matcher = default;

			return false;
		}

		if (containingTypeConditions.IsDefaultOrEmpty && memberConditions.IsDefaultOrEmpty && memberKinds.Count == 0)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "DeclarationMatcher requires at least one matcher attribute or memberKind.", declaration, xmlPath);
			matcher = default;

			return false;
		}

		matcher = new SemanticDeclarationMatcher(containingTypeConditions, memberConditions, memberKinds);

		return true;
	}

	private static bool TryParseBehavioralOperationMatchers(IEnumerable<XElement> elements, XElement owner, string label, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out ImmutableArray<SemanticOperationMatcher> matchers)
	{
		var builder = ImmutableArray.CreateBuilder<SemanticOperationMatcher>();
		foreach (var element in elements)
		{
			if (!TryParseOperationMatcher(element, xmlPath, issues, out var matcher))
			{
				continue;
			}

			builder.Add(matcher);
		}

		if (builder.Count == 0)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, owner.Name.LocalName + " requires at least one valid " + label + ".", owner, xmlPath);
			matchers = ImmutableArray<SemanticOperationMatcher>.Empty;

			return false;
		}

		matchers = builder.ToImmutable();

		return true;
	}

	private static bool TryParseBehavioralRelatedOperationMatchers(XElement ruleElement, string containerName, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out ImmutableArray<SemanticOperationMatcher> matchers)
	{
		var containers = ruleElement.Elements(containerName).ToArray();
		if (containers.Length != 1)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, ruleElement.Name.LocalName + " requires exactly one " + containerName + " container.", ruleElement, xmlPath);
			matchers = ImmutableArray<SemanticOperationMatcher>.Empty;

			return false;
		}

		var container = containers[0];
		if (HasUnexpectedAttributes(container, "description", "comment"))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, containerName + " supports only description and comment attributes.", container, xmlPath);
			matchers = ImmutableArray<SemanticOperationMatcher>.Empty;

			return false;
		}

		var unexpectedChildren = container.Elements().Where(child => child.Name.LocalName != "OperationMatcher").ToArray();
		if (unexpectedChildren.Length > 0)
		{
			foreach (var child in unexpectedChildren)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, containerName + " supports only OperationMatcher children.", child, xmlPath);
			}

			matchers = ImmutableArray<SemanticOperationMatcher>.Empty;

			return false;
		}

		var result = TryParseBehavioralOperationMatchers(container.Elements("OperationMatcher"), container, "OperationMatcher", xmlPath, issues, out matchers);

		return result;
	}

	private static bool TryParseBehavioralOperationOrdering(XElement element, bool supportsOrdering, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out BehavioralOperationOrdering ordering)
	{
		if (!supportsOrdering)
		{
			ordering = BehavioralOperationOrdering.Lexical;

			return true;
		}

		var configuredOrdering = element.Attribute("ordering")?.Value;
		if (string.IsNullOrWhiteSpace(configuredOrdering))
		{
			ordering = BehavioralOperationOrdering.Dominance;

			return true;
		}

		var normalizedOrdering = configuredOrdering!.Trim().ToLowerInvariant();
		switch (normalizedOrdering)
		{
			case "dominance":
				ordering = BehavioralOperationOrdering.Dominance;

				return true;
			case "lexical":
				ordering = BehavioralOperationOrdering.Lexical;

				return true;
			default:
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, element.Name.LocalName + " ordering must be Dominance or Lexical.", element, xmlPath);
				ordering = default;

				return false;
		}
	}

	private static bool TryParseBehavioralMaximumCount(XElement element, BehavioralOperationRuleKind kind, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out int maximumCount)
	{
		if (kind is not BehavioralOperationRuleKind.MaximumOperationCount)
		{
			maximumCount = 0;

			return true;
		}

		var configuredMaximum = element.Attribute("maximum")?.Value;
		if (!int.TryParse(configuredMaximum, NumberStyles.None, CultureInfo.InvariantCulture, out maximumCount) || maximumCount < 1)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "MaximumOperationCount maximum must be a whole number greater than zero.", element, xmlPath);
			maximumCount = 0;

			return false;
		}

		return true;
	}

	private static bool TryGetBehavioralOperationRuleKind(string elementName, out BehavioralOperationRuleKind kind)
	{
		switch (elementName)
		{
			case "RequiredOperation":
				kind = BehavioralOperationRuleKind.RequiredOperation;

				return true;
			case "RequiredOperationBefore":
				kind = BehavioralOperationRuleKind.RequiredOperationBefore;

				return true;
			case "ForbiddenOperationAfter":
				kind = BehavioralOperationRuleKind.ForbiddenOperationAfter;

				return true;
			case "MaximumOperationCount":
				kind = BehavioralOperationRuleKind.MaximumOperationCount;

				return true;
			default:
				kind = default;

				return false;
		}
	}

	private static bool HasValidBehavioralOperationRuleAttributes(XElement element, bool supportsOrdering, bool supportsMaximum)
	{
		var allowedAttributes = new List<string> { "description", "comment", "allowedSites", "blockedSites" };
		if (supportsOrdering)
		{
			allowedAttributes.Add("ordering");
		}

		if (supportsMaximum)
		{
			allowedAttributes.Add("maximum");
		}

		var result = !HasUnexpectedAttributes(element, [.. allowedAttributes]);

		return result;
	}

	private static bool HasOnlyBehavioralOperationRuleChildren(XElement element, string? relatedContainerName)
	{
		var result = element.Elements().All(child => child.Name.LocalName == "DeclarationMatcher"
			|| child.Name.LocalName == "OperationMatcher"
			|| child.Name.LocalName == relatedContainerName);

		return result;
	}

	private static string CreateBehavioralOperationRuleAttributeMessage(string elementName)
	{
		var result = elementName == "MaximumOperationCount"
			? "MaximumOperationCount supports description, comment, allowedSites, blockedSites, and maximum attributes."
			: elementName + " supports description, comment, allowedSites, blockedSites, and ordering attributes.";

		return result;
	}

	private static string CreateBehavioralOperationRuleChildMessage(string elementName, string? relatedContainerName)
	{
		var result = relatedContainerName is null
			? elementName + " supports one DeclarationMatcher and one or more OperationMatcher children."
			: elementName + " supports one DeclarationMatcher, one or more OperationMatcher children, and one " + relatedContainerName + " container.";

		return result;
	}

	private static string CreateBehavioralOperationRuleDisplayName(XElement element, BehavioralOperationRuleKind kind)
	{
		var operationKinds = element.Elements("OperationMatcher")
			.Select(matcher => matcher.Attribute("kind")?.Value)
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.ToArray();
		var selectedOperation = operationKinds.Length == 0 ? "configured operation" : string.Join(" or ", operationKinds) + " operation";
		var result = kind switch
		{
			BehavioralOperationRuleKind.RequiredOperation => "required " + selectedOperation,
			BehavioralOperationRuleKind.RequiredOperationBefore => "required " + selectedOperation + " before configured operation",
			BehavioralOperationRuleKind.ForbiddenOperationAfter => "forbidden " + selectedOperation + " after configured operation",
			BehavioralOperationRuleKind.MaximumOperationCount => "maximum count for " + selectedOperation,
			_ => selectedOperation
		};

		return result;
	}
}
