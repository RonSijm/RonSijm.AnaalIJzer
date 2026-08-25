using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;

internal static partial class ArchitectureConfigurationXmlEditor
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
}
