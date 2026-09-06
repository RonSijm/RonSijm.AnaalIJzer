using System.Collections.Immutable;
using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private static ImmutableArray<XElement> ParseApiSurfaceRules(string childXml)
	{
		try
		{
			var wrapper = XElement.Parse("<Root>" + childXml + "</Root>", LoadOptions.PreserveWhitespace);
			var result = wrapper.Elements().Where(element => element.Name.LocalName is "AllowedLayer" or "BlockedLayer").Select(element => new XElement(element)).ToImmutableArray();

			return result;
		}
		catch
		{
			return ImmutableArray<XElement>.Empty;
		}
	}

	private static XElement? ParseTransitiveExposure(string childXml)
	{
		try
		{
			var wrapper = XElement.Parse("<Root>" + childXml + "</Root>", LoadOptions.PreserveWhitespace);
			var result = wrapper.Elements().FirstOrDefault(element => element.Name.LocalName == "TransitiveExposure");

			return result is null ? null : new XElement(result);
		}
		catch
		{
			return null;
		}
	}

	private static XElement? CreateTransitiveExposureElement(bool enabled, string depthText, string? description)
	{
		if (!enabled)
		{
			return null;
		}

		if (!int.TryParse(depthText.Trim(), out var depth) || depth is < 1 or > 10)
		{
			return null;
		}

		var result = new XElement("TransitiveExposure", new XAttribute("maxDepth", depth));
		if (!string.IsNullOrWhiteSpace(description))
		{
			result.Add(new XAttribute("description", description!.Trim()));
		}

		return result;
	}

	private static ImmutableDictionary<string, string> CreateApiSurfaceAttributes(bool requireRecognizedTypes, string? description)
	{
		var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
		if (requireRecognizedTypes)
		{
			builder["requireRecognizedTypes"] = "true";
		}
		if (!string.IsNullOrWhiteSpace(description))
		{
			builder["description"] = description!.Trim();
		}

		return builder.ToImmutable();
	}

	private static bool ParseBoolean(ImmutableDictionary<string, string> attributes, string key)
	{
		var result = attributes.TryGetValue(key, out var value)
		             && (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1");

		return result;
	}
}
