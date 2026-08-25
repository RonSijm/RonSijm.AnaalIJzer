using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;

internal static partial class ArchitectureConfigurationXmlNavigator
{
	private static IEnumerable<XElement> GetExceptionElementCandidates(XElement containerRoot, bool isRootScope)
	{
		var ownerElements = isRootScope
			? containerRoot.Elements(ArchitectureConfigurationXmlNames.AllowedElementName)
				.SelectMany(container => container.Elements().Where(ArchitectureConfigurationXmlEditor.IsPolicyMatcherElement))
				.Concat(containerRoot.Elements(ArchitectureConfigurationXmlNames.ForbiddenElementName).SelectMany(container => container.Elements().Where(ArchitectureConfigurationXmlEditor.IsPolicyMatcherElement)))
			: containerRoot.Elements().Where(ArchitectureConfigurationXmlEditor.IsMatcherElement)
				.Concat(containerRoot.Elements(ArchitectureConfigurationXmlNames.AllowedElementName).SelectMany(container => container.Elements().Where(ArchitectureConfigurationXmlEditor.IsPolicyMatcherElement)))
				.Concat(containerRoot.Elements(ArchitectureConfigurationXmlNames.ForbiddenElementName).SelectMany(container => container.Elements().Where(ArchitectureConfigurationXmlEditor.IsPolicyMatcherElement)));
		var result = ownerElements.SelectMany(EnumerateExceptionElements);

		return result;
	}

	private static IEnumerable<XElement> EnumerateExceptionElements(XElement ownerElement)
	{
		var exceptions = ownerElement.Element(ArchitectureConfigurationXmlNames.ExceptionsElementName);
		if (exceptions is null)
		{
			yield break;
		}

		foreach (var matcher in EnumerateExceptionElementsRecursive(exceptions))
		{
			yield return matcher;
		}
	}

	private static IEnumerable<XElement> EnumerateExceptionElementsRecursive(XElement exceptionsContainer)
	{
		foreach (var matcher in exceptionsContainer.Elements().Where(ArchitectureConfigurationXmlEditor.IsMatcherElement))
		{
			yield return matcher;

			var nestedExceptions = matcher.Element(ArchitectureConfigurationXmlNames.ExceptionsElementName);
			if (nestedExceptions is null)
			{
				continue;
			}

			foreach (var nestedMatcher in EnumerateExceptionElementsRecursive(nestedExceptions))
			{
				yield return nestedMatcher;
			}
		}
	}
}
