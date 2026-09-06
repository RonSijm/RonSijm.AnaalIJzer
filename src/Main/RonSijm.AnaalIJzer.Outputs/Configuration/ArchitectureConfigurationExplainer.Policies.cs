using System.Text;
using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.Outputs.Configuration;

internal static partial class ArchitectureConfigurationExplainer
{
	private static void AppendTypePolicy(StringBuilder sb, XElement element, int depth)
	{
		var kind = element.Name.LocalName == "Allowed" ? "allows only matching dependency types" : "rejects matching dependency types";
		AppendLine(sb, depth, "- Type policy " + kind + ".");
		AppendDescription(sb, element, depth + 1);
		foreach (var matcher in element.Elements().Where(child => child.Name.LocalName is "Class" or "Namespace" or "Assembly"))
		{
			AppendLine(sb, depth + 1, "- " + matcher.Name.LocalName + " " + FormatMatcher(matcher));
			AppendDescription(sb, matcher, depth + 2);
		}
	}

	private static void AppendNameRules(StringBuilder sb, XElement element, int depth)
	{
		AppendLine(sb, depth, "- Name rules check whether important names keep the same meaning.");
		AppendDescription(sb, element, depth + 1);
		foreach (var rule in element.Elements())
		{
			var ruleName = rule.Name.LocalName switch
			{
				"RequireMatchingNames" => "Require matching value names",
				"RequireDeclarationNameMatchesType" => "Require declaration name to match its type",
				_ => null
			};
			if (ruleName is null)
			{
				continue;
			}

			var details = new List<string>();
			AddAttribute(details, rule, "allowedSites");
			AddAttribute(details, rule, "blockedSites");
			AddAttribute(details, rule, "valueTracking");
			var detailText = details.Count == 0 ? string.Empty : " (" + string.Join(", ", details) + ")";
			AppendLine(sb, depth + 1, "- " + ruleName + detailText + ".");
			AppendDescription(sb, rule, depth + 2);
			foreach (var child in rule.Elements())
			{
				AppendLine(sb, depth + 2, "- " + child.Name.LocalName + " " + FormatMatcher(child));
				AppendDescription(sb, child, depth + 3);
			}
		}
	}

	private static void AppendVisibilityPolicy(StringBuilder sb, XElement element, int depth)
	{
		var targets = element.Attribute("targets")?.Value ?? "(missing targets)";
		var allowed = element.Attribute("allowedAccessibilities")?.Value;
		var blocked = element.Attribute("blockedAccessibilities")?.Value;
		var behavior = allowed is not null
			? "allows only `" + Escape(allowed) + "`"
			: "blocks `" + Escape(blocked ?? "(missing accessibility list)") + "`";
		AppendLine(sb, depth, "- Visibility policy for `" + Escape(targets) + "` " + behavior + ".");
		AppendDescription(sb, element, depth + 1);
	}

	private static void AppendInheritancePolicy(StringBuilder sb, XElement element, int depth)
	{
		var typeKinds = element.Attribute("typeKinds")?.Value ?? "(missing typeKinds)";
		var requiredBaseTypes = element.Attribute("requiredBaseTypes")?.Value;
		var requiredInterfaces = element.Attribute("requiredInterfaces")?.Value;
		var requirements = new List<string>();
		if (!string.IsNullOrWhiteSpace(requiredBaseTypes))
		{
			requirements.Add("inherit `" + Escape(requiredBaseTypes) + "`");
		}

		if (!string.IsNullOrWhiteSpace(requiredInterfaces))
		{
			requirements.Add("implement `" + Escape(requiredInterfaces) + "`");
		}

		var requirementText = requirements.Count == 0 ? "declare an explicit inheritance contract" : string.Join(" and ", requirements);
		AppendLine(sb, depth, "- Inheritance policy requires `" + Escape(typeKinds) + "` declarations to " + requirementText + ".");
		AppendDescription(sb, element, depth + 1);
	}

	private static void AppendReturnValuePolicy(StringBuilder sb, XElement element, int depth)
	{
		AppendLine(sb, depth, "- Return-value policy forbids configured direct returned expressions.");
		AppendDescription(sb, element, depth + 1);
		foreach (var matcher in element.Elements().Where(child => child.Name.LocalName is "Literal" or "Invocation" or "New" or "Identifier" or "MemberAccess"))
		{
			AppendLine(sb, depth + 1, "- Forbids returned " + matcher.Name.LocalName.ToLowerInvariant() + " " + FormatMatcher(matcher) + ".");
			AppendDescription(sb, matcher, depth + 2);
		}
	}

	private static void AppendForbiddenOperations(StringBuilder sb, XElement element, int depth)
	{
		AppendLine(sb, depth, "- Forbidden-operation policy blocks selected resolved members while leaving other members on the same type available.");
		AppendDescription(sb, element, depth + 1);
		foreach (var rule in element.Elements("ForbiddenOperation"))
		{
			var details = new List<string>();
			AddAttribute(details, rule, "allowedSites");
			AddAttribute(details, rule, "blockedSites");
			var detailText = details.Count == 0 ? " at all sites" : " (" + string.Join(", ", details) + ")";
			AppendLine(sb, depth + 1, "- Blocks a selected operation" + detailText + ".");
			AppendDescription(sb, rule, depth + 2);
			foreach (var matcher in rule.Elements("OperationMatcher"))
			{
				var kind = matcher.Attribute("kind")?.Value ?? "(missing kind)";
				var staticAccess = matcher.Attribute("staticAccess")?.Value;
				var staticText = string.IsNullOrWhiteSpace(staticAccess) ? string.Empty : " (staticAccess=" + Escape(staticAccess) + ")";
				AppendLine(sb, depth + 2, "- Matches " + Escape(kind) + staticText + ".");
				AppendDescription(sb, matcher, depth + 3);
				foreach (var child in matcher.Elements().Where(child => child.Name.LocalName is "ContainingType" or "Member"))
				{
					AppendLine(sb, depth + 3, "- " + child.Name.LocalName + " " + FormatMatcher(child) + ".");
					AppendDescription(sb, child, depth + 4);
				}
			}
		}
	}

	private static void AppendBehavioralOperations(StringBuilder sb, XElement element, int depth)
	{
		AppendLine(sb, depth, "- Behavioral-operation policies prove configured operation presence, ordering, or count facts for explicitly selected declaration bodies.");
		AppendDescription(sb, element, depth + 1);
		foreach (var rule in element.Elements().Where(child => child.Name.LocalName is "RequiredOperation" or "RequiredOperationBefore" or "ForbiddenOperationAfter" or "MaximumOperationCount"))
		{
			var details = new List<string>();
			AddAttribute(details, rule, "ordering");
			AddAttribute(details, rule, "maximum");
			AddAttribute(details, rule, "allowedSites");
			AddAttribute(details, rule, "blockedSites");
			var detailText = details.Count == 0 ? string.Empty : " (" + string.Join(", ", details) + ")";
			AppendLine(sb, depth + 1, "- " + FormatBehavioralRuleName(rule.Name.LocalName) + detailText + ".");
			AppendDescription(sb, rule, depth + 2);
			AppendBehavioralDeclarationMatcher(sb, rule.Element("DeclarationMatcher"), depth + 2);
			AppendBehavioralOperationMatchers(sb, rule.Elements("OperationMatcher"), "Matches", depth + 2);
			AppendBehavioralRelatedOperationMatchers(sb, rule.Element("BeforeOperation"), "Must happen before", depth + 2);
			AppendBehavioralRelatedOperationMatchers(sb, rule.Element("AfterOperation"), "Terminal operation", depth + 2);
		}
	}

	private static void AppendBehavioralDeclarationMatcher(StringBuilder sb, XElement? declarationMatcher, int depth)
	{
		if (declarationMatcher is null)
		{
			return;
		}

		AppendLine(sb, depth, "- Applies to declarations selected by:");
		AppendDescription(sb, declarationMatcher, depth + 1);
		foreach (var selector in declarationMatcher.Elements().Where(child => child.Name.LocalName is "ContainingType" or "Member"))
		{
			AppendLine(sb, depth + 1, "- " + selector.Name.LocalName + " " + FormatMatcher(selector) + ".");
			AppendDescription(sb, selector, depth + 2);
		}
	}

	private static void AppendBehavioralOperationMatchers(StringBuilder sb, IEnumerable<XElement> matchers, string prefix, int depth)
	{
		foreach (var matcher in matchers)
		{
			var kind = matcher.Attribute("kind")?.Value ?? "(missing kind)";
			AppendLine(sb, depth, "- " + prefix + " " + Escape(kind) + ".");
			AppendDescription(sb, matcher, depth + 1);
			foreach (var selector in matcher.Elements().Where(child => child.Name.LocalName is "ContainingType" or "Member"))
			{
				AppendLine(sb, depth + 1, "- " + selector.Name.LocalName + " " + FormatMatcher(selector) + ".");
				AppendDescription(sb, selector, depth + 2);
			}
		}
	}

	private static void AppendBehavioralRelatedOperationMatchers(StringBuilder sb, XElement? container, string prefix, int depth)
	{
		if (container is null)
		{
			return;
		}

		AppendLine(sb, depth, "- " + prefix + ":");
		AppendDescription(sb, container, depth + 1);
		AppendBehavioralOperationMatchers(sb, container.Elements("OperationMatcher"), "Matches", depth + 1);
	}

	private static string FormatBehavioralRuleName(string elementName)
	{
		var result = elementName switch
		{
			"RequiredOperation" => "Requires an operation",
			"RequiredOperationBefore" => "Requires an operation before another operation",
			"ForbiddenOperationAfter" => "Forbids an operation after another operation",
			"MaximumOperationCount" => "Limits operation count",
			_ => elementName
		};

		return result;
	}

	private static void AppendApiSurface(StringBuilder sb, XElement element, int depth)
	{
		var requiresRecognition = string.Equals(element.Attribute("requireRecognizedTypes")?.Value, "true", StringComparison.OrdinalIgnoreCase)
		                          || element.Attribute("requireRecognizedTypes")?.Value == "1";
		var recognition = requiresRecognition
			? " Unclassified exposed types are rejected."
			: " Unclassified framework and third-party types are ignored.";
		AppendLine(sb, depth, "- API surface policy controls externally visible signatures." + recognition);
		AppendLine(sb, depth + 1, "- This does not control whether the layer may use a type internally.");
		AppendDescription(sb, element, depth + 1);
		var transitive = element.Elements().FirstOrDefault(child => child.Name.LocalName == "TransitiveExposure");
		if (transitive is not null)
		{
			var maxDepth = transitive.Attribute("maxDepth")?.Value ?? "3";
			AppendLine(sb, depth + 1, "- Transitive exposure inspection follows externally visible members breadth-first to a maximum depth of `" + Escape(maxDepth) + "`.");
			AppendLine(sb, depth + 2, "- Traversal is opt-in, bounded, cached, cycle-safe, and reports the shortest forbidden path.");
			AppendDescription(sb, transitive, depth + 2);
		}

		foreach (var rule in element.Elements().Where(child => child.Name.LocalName is "AllowedLayer" or "BlockedLayer"))
		{
			var behavior = rule.Name.LocalName == "AllowedLayer" ? "allows exposure of" : "blocks exposure of";
			var details = new List<string>();
			AddAttribute(details, rule, "allowedSites");
			AddAttribute(details, rule, "blockedSites");
			var detailText = details.Count == 0 ? string.Empty : " (" + string.Join(", ", details) + ")";
			AppendLine(sb, depth + 1, "- " + behavior + " `" + Escape(rule.Attribute("path")?.Value ?? "(missing path)") + "`" + detailText + ".");
			AppendDescription(sb, rule, depth + 2);
		}
	}
}
