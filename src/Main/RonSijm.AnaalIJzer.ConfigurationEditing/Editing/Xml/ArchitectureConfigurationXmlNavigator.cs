using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;

internal static partial class ArchitectureConfigurationXmlNavigator
{
	internal static XElement? FindDependencyElement(XDocument document, ArchitectureDependencyRuleEditHandle handle)
	{
		var elements = document
			.Descendants()
			.Where(element => string.Equals(element.Name.LocalName, handle.ElementKind, StringComparison.Ordinal))
			.ToArray();

		if (handle.XmlLineNumber > 0)
		{
			var byLine = elements.FirstOrDefault(element =>
			{
				var line = (IXmlLineInfo)element;
				var result = line.HasLineInfo() && line.LineNumber == handle.XmlLineNumber;

				return result;
			});
			if (byLine is not null)
			{
				return byLine;
			}
		}

		var byAttributes = elements.FirstOrDefault(element =>
			string.Equals(element.Attribute("from")?.Value, handle.ConfiguredFrom, StringComparison.Ordinal)
			&& string.Equals(element.Attribute("to")?.Value, handle.ConfiguredTo, StringComparison.Ordinal));

		return byAttributes;
	}

	internal static XElement? FindLayerElement(XDocument document, ArchitectureLayerEditHandle handle)
	{
		var elements = document
			.Descendants(ArchitectureConfigurationXmlNames.LayerElementName)
			.ToArray();

		if (handle.XmlLineNumber > 0)
		{
			var byLine = elements.FirstOrDefault(element =>
			{
				var line = (IXmlLineInfo)element;
				var result = line.HasLineInfo() && line.LineNumber == handle.XmlLineNumber;

				return result;
			});
			if (byLine is not null)
			{
				return byLine;
			}
		}

		var byPath = elements.FirstOrDefault(element => string.Equals(ArchitectureConfigurationXmlNavigator.BuildLayerPath(element), handle.LayerPath, StringComparison.Ordinal));

		return byPath;
	}

	internal static XElement? FindConfigurationElement(XDocument document, ArchitectureConfigurationElementEditHandle handle)
	{
		var candidates = GetConfigurationElementCandidates(document, handle).ToArray();
		if (handle.XmlLineNumber > 0)
		{
			var byLine = candidates.FirstOrDefault(element =>
			{
				var line = (IXmlLineInfo)element;
				var result = line.HasLineInfo() && line.LineNumber == handle.XmlLineNumber;

				return result;
			});
			if (byLine is not null)
			{
				return byLine;
			}
		}

		var byAttributes = candidates.FirstOrDefault(element => AttributesMatch(element, handle.Attributes));

		return byAttributes;
	}
}
