using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.ConfigurationEditing.Sites;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

internal static class ArchitectureConfigurationXmlEditor
{
	internal static ArchitectureConfigurationElementDetails CreateElementDetails(XElement element, ArchitectureLayerEditHandle layerHandle, string containerKind)
	{
		var attributes = element
			.Attributes()
			.ToImmutableDictionary(attribute => attribute.Name.LocalName, attribute => attribute.Value, StringComparer.Ordinal);
		var line = (IXmlLineInfo)element;
		var handle = new ArchitectureConfigurationElementEditHandle(
			layerHandle.SourceKind,
			layerHandle.SourcePath,
			line.HasLineInfo() ? line.LineNumber : 0,
			layerHandle.LayerPath,
			containerKind,
			element.Name.LocalName,
			attributes);
		var result = new ArchitectureConfigurationElementDetails(
			handle,
			element.Name.LocalName,
			containerKind,
			attributes,
			FormatElementSummary(element.Name.LocalName, attributes),
			FormatChildXml(element));

		return result;
	}

	internal static string FormatElementSummary(string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var attributeText = attributes.Count == 0
			? string.Empty
			: " " + string.Join(" ", attributes.OrderBy(attribute => attribute.Key, StringComparer.Ordinal).Select(attribute => attribute.Key + "=\"" + attribute.Value + "\""));
		var result = "<" + elementKind + attributeText + " />";

		return result;
	}

	internal static string FormatChildXml(XElement element)
	{
		if (!element.Nodes().Any())
		{
			return string.Empty;
		}

		var result = string.Join(Environment.NewLine, element.Nodes().Select(node => node.ToString(SaveOptions.DisableFormatting)));

		return result;
	}

	internal static bool TryParseChildNodes(string childXml, out ImmutableArray<XNode> childNodes, out string message)
	{
		if (string.IsNullOrWhiteSpace(childXml))
		{
			childNodes = ImmutableArray<XNode>.Empty;
			message = string.Empty;
			return true;
		}

		try
		{
			var wrapper = XElement.Parse("<AnaalIJzerChildren>" + childXml + "</AnaalIJzerChildren>", LoadOptions.PreserveWhitespace);
			childNodes = wrapper.Nodes().Select(CloneNode).ToImmutableArray();
			message = string.Empty;
			return true;
		}
		catch (XmlException exception)
		{
			childNodes = ImmutableArray<XNode>.Empty;
			message = "Child XML is invalid: " + exception.Message;
			return false;
		}
	}

	internal static XNode CloneNode(XNode node)
	{
		XNode result = node switch
		{
			XElement element => new XElement(element),
			XText text => new XText(text.Value),
			XComment comment => new XComment(comment.Value),
			_ => new XText(node.ToString())
		};

		return result;
	}

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

	internal static bool IsSupportedElementKind(string elementKind, string containerKind)
	{
		var result = containerKind == "LayerMatcher"
			? elementKind is "Class" or "Namespace" or "Assembly"
			: elementKind is "Class" or "Namespace";

		return result;
	}

	internal static bool TryCreateAttributes(ImmutableDictionary<string, string> attributes, out ImmutableArray<XAttribute> xAttributes, out string message)
	{
		var builder = ImmutableArray.CreateBuilder<XAttribute>();
		foreach (var attribute in attributes.OrderBy(item => item.Key, StringComparer.Ordinal))
		{
			if (string.IsNullOrWhiteSpace(attribute.Key))
			{
				xAttributes = ImmutableArray<XAttribute>.Empty;
				message = "Attribute names may not be empty.";
				return false;
			}

			try
			{
				XmlConvert.VerifyName(attribute.Key);
			}
			catch (XmlException exception)
			{
				xAttributes = ImmutableArray<XAttribute>.Empty;
				message = "Invalid attribute name '" + attribute.Key + "': " + exception.Message;
				return false;
			}

			var value = attribute.Value?.Trim();
			if (!string.IsNullOrWhiteSpace(value))
			{
				builder.Add(new XAttribute(attribute.Key, value));
			}
		}

		xAttributes = builder.ToImmutable();
		message = string.Empty;
		return true;
	}

	internal static void SetOptionalAttribute(XElement element, string attributeName, string? value)
	{
		var trimmed = value?.Trim();
		if (string.IsNullOrWhiteSpace(trimmed))
		{
			element.Attribute(attributeName)?.Remove();
			return;
		}

		element.SetAttributeValue(attributeName, trimmed);
	}

	internal static void SetOptionalBooleanAttribute(XElement element, string attributeName, bool value)
	{
		element.SetAttributeValue(attributeName, value ? "true" : null);
	}

	internal static bool ReadBooleanAttribute(XElement element, string attributeName)
	{
		var value = element.Attribute(attributeName)?.Value;
		var result = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
		             || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);

		return result;
	}

	internal static void ApplySiteFilter(XElement element, ArchitectureSiteFilterEditMode mode, ImmutableArray<string> sites)
	{
		element.Attribute(ArchitectureConfigurationXmlNames.AllowedSitesAttributeName)?.Remove();
		element.Attribute(ArchitectureConfigurationXmlNames.BlockedSitesAttributeName)?.Remove();
		if (mode == ArchitectureSiteFilterEditMode.All)
		{
			return;
		}

		var normalizedSites = ArchitectureDependencySiteNames.All
			.Where(site => sites.Contains(site, StringComparer.Ordinal))
			.ToImmutableArray();
		if (normalizedSites.Length == 0)
		{
			return;
		}

		var attributeName = mode == ArchitectureSiteFilterEditMode.AllowedSites ? ArchitectureConfigurationXmlNames.AllowedSitesAttributeName : ArchitectureConfigurationXmlNames.BlockedSitesAttributeName;
		element.SetAttributeValue(attributeName, string.Join(", ", normalizedSites));
	}
}
