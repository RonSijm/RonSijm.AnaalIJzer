using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

internal static class ArchitectureConfigurationXmlNavigator
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

	internal static IEnumerable<XElement> GetConfigurationElementCandidates(XDocument document, ArchitectureConfigurationElementEditHandle handle)
	{
		var containerRoot = GetConfigurationElementContainerRoot(document, handle);
		if (containerRoot is null)
		{
			return Enumerable.Empty<XElement>();
		}

		if (handle.ContainerKind == ArchitectureConfigurationXmlNames.IncludeElementName)
		{
			var result = containerRoot
				.Elements(ArchitectureConfigurationXmlNames.IncludeElementName)
				.Where(element => string.Equals(element.Name.LocalName, handle.ElementKind, StringComparison.Ordinal));

			return result;
		}

		if (handle.ContainerKind == "LayerMatcher")
		{
			var result = containerRoot
				.Elements()
				.Where(element => string.Equals(element.Name.LocalName, handle.ElementKind, StringComparison.Ordinal) && ArchitectureConfigurationXmlEditor.IsMatcherElement(element));

			return result;
		}

		if (handle.ContainerKind is ArchitectureConfigurationXmlNames.AllowedElementName or ArchitectureConfigurationXmlNames.ForbiddenElementName)
		{
			var result = containerRoot
				.Elements(handle.ContainerKind)
				.SelectMany(container => container.Elements())
				.Where(element => string.Equals(element.Name.LocalName, handle.ElementKind, StringComparison.Ordinal));

			return result;
		}

		return Enumerable.Empty<XElement>();
	}

	internal static XElement? GetConfigurationElementContainerRoot(XDocument document, ArchitectureConfigurationElementEditHandle handle)
	{
		if (string.IsNullOrWhiteSpace(handle.LayerPath))
		{
			return document.Root;
		}

		var layerHandle = new ArchitectureLayerEditHandle(handle.SourceKind, handle.SourcePath, 0, handle.LayerPath, string.Empty, ArchitectureConfigurationXmlNavigator.GetParentPath(handle.LayerPath), null);
		var result = FindLayerElement(document, layerHandle);

		return result;
	}

	internal static string GetParentPath(string path)
	{
		var slashIndex = path.LastIndexOf('/');
		var result = slashIndex <= 0 ? string.Empty : path.Substring(0, slashIndex);

		return result;
	}

	internal static bool AttributesMatch(XElement element, ImmutableDictionary<string, string> expectedAttributes)
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

	internal static string BuildLayerPath(XElement layerElement)
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
				.Elements("Layer")
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
