using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.SymbolFacts;
using RonSijm.AnaalIJzer.Engine.Visibility;

namespace RonSijm.AnaalIJzer.Config.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static ImmutableArray<VisibilityPolicy> ParseVisibilityPolicies(IEnumerable<XElement> policyElements, string ownerLayerPath, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var policies = ImmutableArray.CreateBuilder<VisibilityPolicy>();
		foreach (var element in policyElements)
		{
			if (!TryParseVisibilityTargets(element, out var targets, out var targetError))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, targetError, element, xmlPath);
				continue;
			}

			var allowedValue = element.Attribute("allowedAccessibilities")?.Value;
			var blockedValue = element.Attribute("blockedAccessibilities")?.Value;
			if (string.IsNullOrWhiteSpace(allowedValue) == string.IsNullOrWhiteSpace(blockedValue))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "VisibilityPolicy requires exactly one of allowedAccessibilities or blockedAccessibilities.", element, xmlPath);
				continue;
			}

			var configuredValue = allowedValue ?? blockedValue!;
			if (!TryParseAccessibilities(configuredValue, out var accessibilities, out var accessibilityError))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, accessibilityError, element, xmlPath);
				continue;
			}

			var line = (IXmlLineInfo)element;
			policies.Add(new VisibilityPolicy(
				ownerLayerPath,
				targets,
				allowedValue is not null,
				accessibilities,
				element.Attribute("description")?.Value,
				xmlPath,
				line.HasLineInfo() ? line.LineNumber : 0,
				line.HasLineInfo() ? line.LinePosition : 0));
		}

		return policies.ToImmutable();
	}

	private static bool TryParseVisibilityTargets(XElement element, out ImmutableHashSet<VisibilityPolicyTarget> targets, out string error)
	{
		var values = element.Attribute("targets")?.Value;
		if (string.IsNullOrWhiteSpace(values))
		{
			targets = ImmutableHashSet<VisibilityPolicyTarget>.Empty;
			error = "VisibilityPolicy requires a non-empty targets value.";
			return false;
		}

		var builder = ImmutableHashSet.CreateBuilder<VisibilityPolicyTarget>();
		foreach (var value in values!.Split(','))
		{
			if (!VisibilityPolicyTargetParser.TryParse(value, out var target))
			{
				targets = ImmutableHashSet<VisibilityPolicyTarget>.Empty;
				error = $"VisibilityPolicy contains unknown target '{value.Trim()}'. Supported values: Type, Constructor, Method, Property, Field, Event, Operator, Conversion, NestedType.";
				return false;
			}

			builder.Add(target);
		}

		targets = builder.ToImmutable();
		error = string.Empty;
		return true;
	}

	private static bool TryParseAccessibilities(string values, out ImmutableHashSet<ArchitectureAccessibility> accessibilities, out string error)
	{
		var builder = ImmutableHashSet.CreateBuilder<ArchitectureAccessibility>();
		foreach (var value in values.Split(','))
		{
			if (!ArchitectureAccessibilityParser.TryParse(value, out var accessibility))
			{
				accessibilities = ImmutableHashSet<ArchitectureAccessibility>.Empty;
				error = $"VisibilityPolicy contains unknown accessibility '{value.Trim()}'. Supported values: Public, Internal, Protected, ProtectedInternal, PrivateProtected, Private, File.";
				return false;
			}

			builder.Add(accessibility);
		}

		if (builder.Count == 0)
		{
			accessibilities = ImmutableHashSet<ArchitectureAccessibility>.Empty;
			error = "VisibilityPolicy requires a non-empty accessibility list.";
			return false;
		}

		accessibilities = builder.ToImmutable();
		error = string.Empty;
		return true;
	}
}

