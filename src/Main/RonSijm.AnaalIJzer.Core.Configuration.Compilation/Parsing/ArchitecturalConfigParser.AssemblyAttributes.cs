using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.AssemblyAttributes.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Matchers;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.Matchers.Symbols;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
	internal static AssemblyAttributePolicyCatalog ParseAssemblyAttributePolicies(IEnumerable<ArchitectureConfigurationElementInput> policyInputs, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var policies = ImmutableArray.CreateBuilder<AssemblyAttributePolicy>();
		foreach (var policyInput in policyInputs)
		{
			if (!TryParseAssemblyAttributePolicy(policyInput, issues, out var policy))
			{
				continue;
			}

			policies.Add(policy);
		}

		var result = new AssemblyAttributePolicyCatalog(policies.ToImmutable());

		return result;
	}

	private static bool TryParseAssemblyAttributePolicy(ArchitectureConfigurationElementInput policyInput, ImmutableArray<ConfigurationIssue>.Builder issues, out AssemblyAttributePolicy policy)
	{
		var element = policyInput.Element;
		if (HasUnexpectedAttributes(element, "description"))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "AssemblyAttributePolicy supports only a description attribute.", element, policyInput.Path);
			policy = default;

			return false;
		}

		var allowedRules = ParseAssemblyAttributeRuleContainer(element, "Allowed", policyInput.Path, issues);
		var forbiddenRules = ParseAssemblyAttributeRuleContainer(element, "Forbidden", policyInput.Path, issues);
		var invalidChildren = element.Elements().Where(child => child.Name.LocalName is not ("Allowed" or "Forbidden")).ToArray();
		foreach (var invalidChild in invalidChildren)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "AssemblyAttributePolicy supports only Allowed and Forbidden children.", invalidChild, policyInput.Path);
		}

		if (allowedRules.IsDefaultOrEmpty && forbiddenRules.IsDefaultOrEmpty)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "AssemblyAttributePolicy requires at least one valid Allowed or Forbidden Attribute rule.", element, policyInput.Path);
			policy = default;

			return false;
		}

		var line = (IXmlLineInfo)element;
		policy = new AssemblyAttributePolicy(
			allowedRules,
			forbiddenRules,
			element.Attribute("description")?.Value,
			policyInput.Path,
			line.HasLineInfo() ? line.LineNumber : 0,
			line.HasLineInfo() ? line.LinePosition : 0);

		return true;
	}

	private static ImmutableArray<AssemblyAttributeRule> ParseAssemblyAttributeRuleContainer(XElement policyElement, string containerName, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var rules = ImmutableArray.CreateBuilder<AssemblyAttributeRule>();
		foreach (var container in policyElement.Elements(containerName))
		{
			if (HasUnexpectedAttributes(container, "description"))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"AssemblyAttributePolicy {containerName} supports only a description attribute.", container, xmlPath);
				continue;
			}

			foreach (var child in container.Elements())
			{
				if (child.Name.LocalName != "Attribute")
				{
					AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"AssemblyAttributePolicy {containerName} supports only Attribute children.", child, xmlPath);
					continue;
				}

				if (TryParseAssemblyAttributeRule(child, xmlPath, issues, out var rule))
				{
					rules.Add(rule);
				}
			}
		}

		var result = rules.ToImmutable();

		return result;
	}

	private static bool TryParseAssemblyAttributeRule(XElement element, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out AssemblyAttributeRule rule)
	{
		var hasUnsupportedAttribute = element.Attributes().Any(attribute => attribute.Name.LocalName is not ("description" or "comment")
			&& !MatcherAttributeCatalog.IsSupportedAttribute(attribute.Name.LocalName, MatcherAttributeProfile.Type));
		if (hasUnsupportedAttribute)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "AssemblyAttributePolicy Attribute supports the standard type matcher attributes and documentation attributes.", element, xmlPath);
			rule = default;

			return false;
		}

		var conditions = MatcherAttributeCatalog.CreateConditions(attributeName => element.Attribute(attributeName)?.Value, MatcherAttributeProfile.Type);
		if (conditions.IsDefaultOrEmpty)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "AssemblyAttributePolicy Attribute requires at least one type matcher attribute.", element, xmlPath);
			rule = default;

			return false;
		}

		if (element.Attribute("typeKind")?.Value is { } typeKind && !ITypeSymbolTypeKindExtension.IsSupportedTypeKind(typeKind))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Unknown typeKind '{typeKind}'. Supported values: Class, Interface, Struct, Record, RecordStruct, Enum, Delegate.", element, xmlPath);
			rule = default;

			return false;
		}

		if (!HasValidAssemblyAttributeRegex(element, xmlPath, issues))
		{
			rule = default;

			return false;
		}

		var attributeMatcher = new PatternMatcher(MatchTarget.TypeName, conditions);

		var arguments = ImmutableArray.CreateBuilder<AssemblyAttributeArgumentMatcher>();
		foreach (var child in element.Elements())
		{
			if (child.Name.LocalName != "Argument")
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "AssemblyAttributePolicy Attribute supports only Argument children.", child, xmlPath);
				rule = default;

				return false;
			}

			if (!TryParseAssemblyAttributeArgumentMatcher(child, xmlPath, issues, out var argumentMatcher))
			{
				rule = default;

				return false;
			}

			arguments.Add(argumentMatcher);
		}

		var line = (IXmlLineInfo)element;
		rule = new AssemblyAttributeRule(
			attributeMatcher,
			arguments.ToImmutable(),
			CreateAssemblyAttributeRuleDisplayName(element),
			element.Attribute("description")?.Value ?? element.Attribute("comment")?.Value,
			xmlPath,
			line.HasLineInfo() ? line.LineNumber : 0,
			line.HasLineInfo() ? line.LinePosition : 0);

		return true;
	}

	private static bool TryParseAssemblyAttributeArgumentMatcher(XElement element, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out AssemblyAttributeArgumentMatcher matcher)
	{
		var hasUnsupportedAttribute = element.Attributes().Any(attribute => attribute.Name.LocalName is not ("index" or "name" or "description")
			&& !MatcherAttributeCatalog.IsSupportedAttribute(attribute.Name.LocalName, MatcherAttributeProfile.ProjectOrPackage));
		if (hasUnsupportedAttribute)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "AssemblyAttributePolicy Argument supports index or name plus typeName, exactName, startsWith, endsWith, contains, or regex matchers.", element, xmlPath);
			matcher = default;

			return false;
		}

		var indexText = element.Attribute("index")?.Value;
		var name = element.Attribute("name")?.Value?.Trim();
		if (string.IsNullOrWhiteSpace(indexText) == string.IsNullOrWhiteSpace(name))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "AssemblyAttributePolicy Argument requires exactly one of index or name.", element, xmlPath);
			matcher = default;

			return false;
		}

		int? index = null;
		if (!string.IsNullOrWhiteSpace(indexText))
		{
			if (!int.TryParse(indexText, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var parsedIndex) || parsedIndex < 0)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "AssemblyAttributePolicy Argument index must be a non-negative whole number.", element, xmlPath);
				matcher = default;

				return false;
			}

			index = parsedIndex;
		}

		var conditions = MatcherAttributeCatalog.CreateConditions(attributeName => element.Attribute(attributeName)?.Value, MatcherAttributeProfile.ProjectOrPackage);
		if (conditions.IsDefaultOrEmpty)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "AssemblyAttributePolicy Argument requires at least one textual matcher attribute.", element, xmlPath);
			matcher = default;

			return false;
		}

		if (!HasValidAssemblyAttributeRegex(element, xmlPath, issues))
		{
			matcher = default;

			return false;
		}

		matcher = new AssemblyAttributeArgumentMatcher(index, name, new PatternMatcher(MatchTarget.TypeName, conditions));

		return true;
	}

	private static string CreateAssemblyAttributeRuleDisplayName(XElement element)
	{
		var attributeDisplay = ArchitectureConfigurationMatcherReader.GetMatcherDisplayName(element) ?? "any attribute";
		var argumentDisplays = element.Elements("Argument")
			.Select(argument =>
			{
				var selector = argument.Attribute("index")?.Value is { } index
					? "argument #" + index
					: "argument '" + argument.Attribute("name")?.Value + "'";
				var matcherDisplay = ArchitectureConfigurationMatcherReader.GetMatcherDisplayName(argument) ?? "any value";

				return selector + " " + matcherDisplay;
			})
			.ToArray();
		var result = argumentDisplays.Length == 0
			? "attribute " + attributeDisplay
			: "attribute " + attributeDisplay + " with " + string.Join(" and ", argumentDisplays);

		return result;
	}

	private static bool HasValidAssemblyAttributeRegex(XElement element, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var regex = element.Attribute("regex")?.Value;
		if (regex is null)
		{
			return true;
		}

		try
		{
			_ = new Regex(regex, RegexOptions.CultureInvariant);
		}
		catch (ArgumentException exception)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Invalid regular expression '{regex}': {exception.Message}", element, xmlPath);

			return false;
		}

		return true;
	}
}
