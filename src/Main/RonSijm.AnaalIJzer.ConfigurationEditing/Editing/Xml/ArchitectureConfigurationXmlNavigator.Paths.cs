using System.Collections.Immutable;
using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;

internal static partial class ArchitectureConfigurationXmlNavigator
{
	private static string GetParentPath(string path)
	{
		var slashIndex = path.LastIndexOf('/');
		var result = slashIndex <= 0 ? string.Empty : path.Substring(0, slashIndex);

		return result;
	}

	private static bool AttributesMatch(XElement element, ImmutableDictionary<string, string> expectedAttributes)
	{
		foreach (var attribute in expectedAttributes)
		{
			if (!string.Equals(element.Attribute(attribute.Key)?.Value, attribute.Value, StringComparison.Ordinal))
			{
				return false;
			}
		}

		return true;
	}

	private static string BuildLayerPath(XElement layerElement)
	{
		var parts = new Stack<string>();
		var current = layerElement;
		while (current is not null && string.Equals(current.Name.LocalName, ArchitectureConfigurationXmlNames.LayerElementName, StringComparison.Ordinal))
		{
			var name = current.Attribute("name")?.Value;
			if (!string.IsNullOrWhiteSpace(name))
			{
				parts.Push(name!);
			}

			current = current.Parent;
		}

		var result = string.Join("/", parts);

		return result;
	}

	internal static bool HasMatchingDependency(XElement container, string elementName, string from, string to)
	{
		var result = container
			.Elements(elementName)
			.Any(element => string.Equals(element.Attribute("from")?.Value, from, StringComparison.Ordinal)
			                && string.Equals(element.Attribute("to")?.Value, to, StringComparison.Ordinal));

		return result;
	}

	internal static XElement? FindDependencyInsertionContainer(XDocument document, string scopePath)
	{
		if (document.Root is null)
		{
			return null;
		}

		if (string.IsNullOrWhiteSpace(scopePath))
		{
			return document.Root;
		}

		var current = document.Root;
		foreach (var segment in ArchitectureConfigurationLayerPaths.SplitLayerPath(scopePath))
		{
			current = current
				.Elements(ArchitectureConfigurationXmlNames.LayerElementName)
				.FirstOrDefault(element => string.Equals(element.Attribute("name")?.Value, segment, StringComparison.Ordinal));
			if (current is null)
			{
				return null;
			}
		}

		return current;
	}

	internal static XElement? FindLayerInsertionContainer(XDocument document, string parentLayerPath)
	{
		var result = FindDependencyInsertionContainer(document, parentLayerPath);

		return result;
	}
}
