using System.Text;
using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.Outputs.Configuration;

internal static partial class ArchitectureConfigurationExplainer
{
	private static void AppendOperationContracts(StringBuilder sb, XElement element, int depth)
	{
		AppendLine(sb, depth, "- Explicit operation contracts connect selected owner methods, optional request and response types, and selected entry points without inferring framework conventions.");
		AppendDescription(sb, element, depth + 1);
		foreach (var operation in element.Elements("Operation"))
		{
			var operationName = operation.Attribute("name")?.Value ?? "(unnamed operation)";
			var details = new List<string>();
			AddAttribute(details, operation, "allowedOwnerLayers");
			AddAttribute(details, operation, "allowedEntryPointLayers");
			var detailText = details.Count == 0 ? string.Empty : " (" + string.Join(", ", details) + ")";
			AppendLine(sb, depth + 1, "- Operation `" + Escape(operationName) + "`" + detailText + ".");
			AppendDescription(sb, operation, depth + 2);
			AppendOperationContractDeclaration(sb, operation.Element("Owner"), "Owner", depth + 2);
			AppendOperationContractType(sb, operation.Element("Request"), "Request", depth + 2);
			AppendOperationContractType(sb, operation.Element("Response"), "Response", depth + 2);
			foreach (var entryPoint in operation.Elements("EntryPoint"))
			{
				AppendOperationContractDeclaration(sb, entryPoint, "Entry point", depth + 2);
			}
		}
	}

	private static void AppendOperationContractDeclaration(StringBuilder sb, XElement? container, string label, int depth)
	{
		if (container?.Element("DeclarationMatcher") is not { } matcher)
		{
			return;
		}

		AppendLine(sb, depth, "- " + label + " declaration:");
		AppendDescription(sb, container, depth + 1);
		foreach (var selector in matcher.Elements().Where(child => child.Name.LocalName is "ContainingType" or "Member"))
		{
			AppendLine(sb, depth + 1, "- " + selector.Name.LocalName + " " + FormatMatcher(selector) + ".");
			AppendDescription(sb, selector, depth + 2);
		}
	}

	private static void AppendOperationContractType(StringBuilder sb, XElement? container, string label, int depth)
	{
		if (container?.Element("Class") is not { } matcher)
		{
			return;
		}

		AppendLine(sb, depth, "- " + label + " type: Class " + FormatMatcher(matcher) + ".");
		AppendDescription(sb, container, depth + 1);
		AppendDescription(sb, matcher, depth + 1);
	}
}
