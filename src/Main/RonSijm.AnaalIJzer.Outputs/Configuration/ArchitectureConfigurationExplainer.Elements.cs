using System.Text;
using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.Outputs.Configuration;

internal static partial class ArchitectureConfigurationExplainer
{
	private static void AppendElements(StringBuilder sb, IEnumerable<XElement> elements, int depth)
	{
		foreach (var element in elements)
		{
			switch (element.Name.LocalName)
			{
				case "Include":
					AppendInclude(sb, element, depth);
					break;
				case "Layer":
					AppendLayer(sb, element, depth);
					break;
				case "AllowedDependency":
				case "BlockedDependency":
					AppendDependency(sb, element, depth);
					break;
				case "ExceptionPolicy":
					AppendExceptionPolicy(sb, element, depth);
					break;
				case "Allowed":
				case "Forbidden":
					AppendTypePolicy(sb, element, depth);
					break;
				case "NameRules":
					AppendNameRules(sb, element, depth);
					break;
				case "VisibilityPolicy":
					AppendVisibilityPolicy(sb, element, depth);
					break;
				case "InheritancePolicy":
					AppendInheritancePolicy(sb, element, depth);
					break;
				case "ReturnValuePolicy":
					AppendReturnValuePolicy(sb, element, depth);
					break;
				case "ApiSurface":
					AppendApiSurface(sb, element, depth);
					break;
			}
		}
	}

	private static void AppendInclude(StringBuilder sb, XElement element, int depth)
	{
		AppendLine(sb, depth, "- Includes `" + Escape(element.Attribute("path")?.Value ?? "(missing path)") + "`.");
		AppendDescription(sb, element, depth + 1);
	}

	private static void AppendLayer(StringBuilder sb, XElement element, int depth)
	{
		var name = element.Attribute("name")?.Value ?? "(unnamed layer)";
		AppendLine(sb, depth, "- Layer `" + Escape(name) + "`");
		AppendDescription(sb, element, depth + 1);
		AppendMatchers(sb, element, depth + 1);
		AppendElements(sb, element.Elements(), depth + 1);
	}

	private static void AppendDependency(StringBuilder sb, XElement element, int depth)
	{
		var kind = element.Name.LocalName == "BlockedDependency" ? "blocks" : "allows";
		var from = element.Attribute("from")?.Value ?? "(missing from)";
		var to = element.Attribute("to")?.Value ?? "(missing to)";
		var details = new List<string>();
		AddAttribute(details, element, "allowedSites");
		AddAttribute(details, element, "blockedSites");
		AddAttribute(details, element, "appliesToDescendants");
		var detailText = details.Count == 0 ? string.Empty : " (" + string.Join(", ", details) + ")";
		AppendLine(sb, depth, "- Dependency rule " + kind + " `" + Escape(from) + "` -> `" + Escape(to) + "`" + detailText + ".");
		AppendDescription(sb, element, depth + 1);
	}

	private static void AppendExceptionPolicy(StringBuilder sb, XElement element, int depth)
	{
		var details = new List<string>();
		AddAttribute(details, element, "requireReason");
		AddAttribute(details, element, "requireOwner");
		AddAttribute(details, element, "requireExpiresOn");
		AddAttribute(details, element, "warnBeforeDays");
		var detailText = details.Count == 0 ? string.Empty : " (" + string.Join(", ", details) + ")";
		AppendLine(sb, depth, "- Exception policy enables temporary-exception governance" + detailText + ".");
		AppendDescription(sb, element, depth + 1);
	}

	private static void AppendMatchers(StringBuilder sb, XElement element, int depth)
	{
		foreach (var matcher in element.Elements().Where(child => child.Name.LocalName is "Class" or "Namespace" or "Assembly"))
		{
			AppendLine(sb, depth, "- Matches " + matcher.Name.LocalName.ToLowerInvariant() + " " + FormatMatcher(matcher));
			AppendDescription(sb, matcher, depth + 1);
		}
	}
}
