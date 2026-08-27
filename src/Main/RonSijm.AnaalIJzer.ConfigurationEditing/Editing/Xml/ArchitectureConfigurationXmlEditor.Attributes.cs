using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Sites;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;

internal static partial class ArchitectureConfigurationXmlEditor
{
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

	internal static int ReadIntegerAttribute(XElement element, string attributeName, int defaultValue)
	{
		var value = element.Attribute(attributeName)?.Value;
		if (int.TryParse(value, out var parsed))
		{
			return parsed;
		}

		var result = defaultValue;

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
