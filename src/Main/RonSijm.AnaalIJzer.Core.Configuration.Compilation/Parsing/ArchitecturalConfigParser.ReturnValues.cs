using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.Matchers.Observations;
using RonSijm.AnaalIJzer.Core.ReturnValues.Policies;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
	internal static ImmutableArray<ReturnValuePolicy> ParseGlobalReturnValuePolicies(IEnumerable<ArchitectureConfigurationElementInput> policyInputs, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var policies = ImmutableArray.CreateBuilder<ReturnValuePolicy>();
		foreach (var policyInput in policyInputs)
		{
			policies.AddRange(ParseReturnValuePolicies([policyInput.Element], "Global", policyInput.Path, issues, true));
		}

		var result = policies.ToImmutable();

		return result;
	}

	private static ImmutableArray<ReturnValuePolicy> ParseReturnValuePolicies(IEnumerable<XElement> policyElements, string ownerLayerPath, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, bool isGlobal = false)
	{
		var policies = ImmutableArray.CreateBuilder<ReturnValuePolicy>();
		foreach (var element in policyElements)
		{
			if (element.Attributes().Any(attribute => attribute.Name.LocalName is not ("description" or "comment")))
			{
				continue;
			}

			var allowedReturnElements = element.Elements("AllowedReturn").ToArray();
			if (allowedReturnElements.Length > 1)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "ReturnValuePolicy supports at most one AllowedReturn block.", element, xmlPath);

				continue;
			}

			var rules = ParseReturnValueRules(element.Elements().Where(child => child.Name.LocalName != "AllowedReturn"), xmlPath, issues);
			var allowedRules = allowedReturnElements.Length == 0
				? ImmutableArray<ReturnValueRule>.Empty
				: ParseAllowedReturnRules(allowedReturnElements[0], xmlPath, issues);
			if (rules.IsDefaultOrEmpty && allowedRules.IsDefaultOrEmpty)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "ReturnValuePolicy requires at least one forbidden matcher or one AllowedReturn matcher.", element, xmlPath);
				continue;
			}

			var line = (IXmlLineInfo)element;
			var policy = new ReturnValuePolicy(
				ownerLayerPath,
				rules,
				element.Attribute("description")?.Value,
				xmlPath,
				line.HasLineInfo() ? line.LineNumber : 0,
				line.HasLineInfo() ? line.LinePosition : 0,
				allowedRules,
				isGlobal);

			policies.Add(policy);
		}

		var result = policies.ToImmutable();

		return result;
	}

	private static ImmutableArray<ReturnValueRule> ParseAllowedReturnRules(XElement allowedReturnElement, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		if (allowedReturnElement.Attributes().Any(attribute => attribute.Name.LocalName is not ("description" or "comment")))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "AllowedReturn supports description and comment attributes only.", allowedReturnElement, xmlPath);

			return ImmutableArray<ReturnValueRule>.Empty;
		}

		var result = ParseReturnValueRules(allowedReturnElement.Elements(), xmlPath, issues);
		if (result.IsDefaultOrEmpty)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "AllowedReturn requires at least one Literal, Invocation, New, Identifier, or MemberAccess matcher child.", allowedReturnElement, xmlPath);
		}

		return result;
	}

	private static ImmutableArray<ReturnValueRule> ParseReturnValueRules(IEnumerable<XElement> ruleElements, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var rules = ImmutableArray.CreateBuilder<ReturnValueRule>();
		foreach (var element in ruleElements)
		{
			if (!CodeObservationMatchTargetParser.TryParse(element.Name.LocalName, out var target)
				|| target == CodeObservationMatchTarget.Throw)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "ReturnValuePolicy supports Literal, Invocation, New, Identifier, and MemberAccess matcher children.", element, xmlPath);
				continue;
			}

			if (element.Attributes().Any(attribute => attribute.Name.LocalName is not "description" and not "comment"
				&& !MatcherAttributeCatalog.IsSupportedAttribute(
					attribute.Name.LocalName,
					MatcherAttributeProfile.SemanticCodeObservation,
					target == CodeObservationMatchTarget.Literal)))
			{
				continue;
			}

			if (!ArchitectureConfigurationMatcherReader.TryReadCodeObservationMatcher(element, true, out var matcher))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Could not read ReturnValuePolicy {element.Name.LocalName} matcher.", element, xmlPath);
				continue;
			}

			var line = (IXmlLineInfo)element;
			var rule = new ReturnValueRule(
				matcher,
				CreateReturnValueRuleDisplayName(element, target),
				element.Attribute("description")?.Value,
				xmlPath,
				line.HasLineInfo() ? line.LineNumber : 0,
				line.HasLineInfo() ? line.LinePosition : 0);

			rules.Add(rule);
		}

		var result = rules.ToImmutable();

		return result;
	}

	private static string CreateReturnValueRuleDisplayName(XElement element, CodeObservationMatchTarget target)
	{
		var matcherLabel = ArchitectureConfigurationMatcherReader.GetMatcherDisplayName(element);
		var targetLabel = target switch
		{
			CodeObservationMatchTarget.MemberAccess => "member access",
			CodeObservationMatchTarget.New => "object creation",
			_ => target.ToString().ToLowerInvariant()
		};
		var result = string.IsNullOrWhiteSpace(matcherLabel)
			? "any " + targetLabel
			: targetLabel + " " + matcherLabel;

		return result;
	}
}
