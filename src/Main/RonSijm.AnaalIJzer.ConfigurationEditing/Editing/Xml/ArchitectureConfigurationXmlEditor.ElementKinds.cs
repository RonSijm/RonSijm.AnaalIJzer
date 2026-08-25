using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;

internal static partial class ArchitectureConfigurationXmlEditor
{
	internal static bool IsMatcherElement(XElement element)
	{
		var result = element.Name.LocalName is "Class" or "Namespace" or "Assembly";

		return result;
	}

	internal static bool IsPolicyMatcherElement(XElement element)
	{
		var result = element.Name.LocalName is "Class" or "Namespace";

		return result;
	}

	internal static bool IsNameRuleElement(XElement element)
	{
		var result = element.Name.LocalName is ArchitectureConfigurationXmlNames.RequireMatchingNamesElementName or ArchitectureConfigurationXmlNames.RequireDeclarationNameMatchesTypeElementName;

		return result;
	}

	internal static bool IsSupportedElementKind(string elementKind, string containerKind)
	{
		var result = containerKind switch
		{
			"LayerMatcher" => elementKind is "Class" or "Namespace" or "Assembly",
			ArchitectureConfigurationXmlNames.NameRulesElementName => elementKind is ArchitectureConfigurationXmlNames.RequireMatchingNamesElementName or ArchitectureConfigurationXmlNames.RequireDeclarationNameMatchesTypeElementName,
			_ => elementKind is "Class" or "Namespace"
		};

		return result;
	}
}
