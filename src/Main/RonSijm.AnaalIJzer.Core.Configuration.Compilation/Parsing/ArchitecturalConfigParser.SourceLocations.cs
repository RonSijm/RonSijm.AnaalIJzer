using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.SourceLocations;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static ImmutableArray<SourceLocationPolicy> ParseSourceLocationPolicies(IEnumerable<XElement> policyElements, string ownerLayerPath, string xmlPath, bool isInlineConfiguration, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var result = ImmutableArray.CreateBuilder<SourceLocationPolicy>();
		foreach (var element in policyElements)
		{
			if (!TryReadSourceLocationBase(element.Attribute("relativeTo")?.Value, out var relativeTo))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "SourceLocations contains an invalid relativeTo value. Use Project, Configuration, or Absolute.", element, xmlPath);
				continue;
			}

			if (relativeTo == SourceLocationBase.Configuration && isInlineConfiguration)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "SourceLocations may not use relativeTo='Configuration' inside inline AssemblyMetadata settings.", element, xmlPath);
				continue;
			}

			var rules = ParseSourceLocationRules(element.Elements("Source"), xmlPath, issues);
			if (rules.IsDefaultOrEmpty)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"SourceLocations for layer '{ownerLayerPath}' requires at least one valid Source matcher.", element, xmlPath);
				continue;
			}

			var line = (IXmlLineInfo)element;
			result.Add(new SourceLocationPolicy(
				ownerLayerPath,
				relativeTo,
				rules,
				element.Attribute("description")?.Value,
				xmlPath,
				line.HasLineInfo() ? line.LineNumber : 0,
				line.HasLineInfo() ? line.LinePosition : 0));
		}

		return result.ToImmutable();
	}

	private static ImmutableArray<SourceLocationRule> ParseSourceLocationRules(IEnumerable<XElement> ruleElements, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var result = ImmutableArray.CreateBuilder<SourceLocationRule>();
		foreach (var element in ruleElements)
		{
			if (!TryReadSourceLocationRule(element, xmlPath, issues, out var rule))
			{
				continue;
			}

			result.Add(rule);
		}

		return result.ToImmutable();
	}

	private static bool TryReadSourceLocationRule(XElement element, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues, out SourceLocationRule rule)
	{
		var conditions = ImmutableArray.CreateBuilder<MatchCondition>();

		if (element.Attribute("typeName")?.Value is { } typeName)
		{
			conditions.Add(new MatchCondition(MatchKind.Equals, typeName));
		}

		if (element.Attribute("exactName")?.Value is { } exactName)
		{
			conditions.Add(new MatchCondition(MatchKind.Equals, exactName));
		}

		if (element.Attribute("startsWith")?.Value is { } startsWith)
		{
			conditions.Add(new MatchCondition(MatchKind.StartsWith, startsWith));
		}

		if (element.Attribute("endsWith")?.Value is { } endsWith)
		{
			conditions.Add(new MatchCondition(MatchKind.EndsWith, endsWith));
		}

		if (element.Attribute("contains")?.Value is { } contains)
		{
			conditions.Add(new MatchCondition(MatchKind.Contains, contains));
		}

		if (element.Attribute("regex")?.Value is { } regex)
		{
			conditions.Add(new MatchCondition(MatchKind.Regex, regex));
		}

		if (conditions.Count == 0)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Source requires at least one matcher attribute.", element, xmlPath);
			rule = default;
			return false;
		}

		var line = (IXmlLineInfo)element;
		rule = new SourceLocationRule(
			conditions.ToImmutable(),
			element.Attribute("assemblyName")?.Value,
			element.Attribute("description")?.Value,
			xmlPath,
			line.HasLineInfo() ? line.LineNumber : 0,
			line.HasLineInfo() ? line.LinePosition : 0);

		return true;
	}

	private static bool TryReadSourceLocationBase(string? value, out SourceLocationBase relativeTo)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			relativeTo = SourceLocationBase.Project;
			return true;
		}

		if (string.Equals(value, nameof(SourceLocationBase.Project), StringComparison.OrdinalIgnoreCase))
		{
			relativeTo = SourceLocationBase.Project;
			return true;
		}

		if (string.Equals(value, nameof(SourceLocationBase.Configuration), StringComparison.OrdinalIgnoreCase))
		{
			relativeTo = SourceLocationBase.Configuration;
			return true;
		}

		if (string.Equals(value, nameof(SourceLocationBase.Absolute), StringComparison.OrdinalIgnoreCase))
		{
			relativeTo = SourceLocationBase.Absolute;
			return true;
		}

		relativeTo = default;
		return false;
	}
}

