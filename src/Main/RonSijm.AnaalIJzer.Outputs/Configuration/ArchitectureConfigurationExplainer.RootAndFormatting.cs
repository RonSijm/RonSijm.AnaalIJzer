using System.Text;
using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.Outputs.Configuration;

internal static partial class ArchitectureConfigurationExplainer
{
	private static void AppendRootSettings(StringBuilder sb, XElement root)
	{
		var settings = new List<string>();
		AddSetting(settings, root, "requireRecognizedDependencies");
		AddSetting(settings, root, "enforceAcyclic");
		AddSetting(settings, root, "enableReport");
		AddSetting(settings, root, "reportPath");
		AddSetting(settings, root, "enableDocumentation");
		AddSetting(settings, root, "documentationPath");
		AddSetting(settings, root, "includeInput");
		if (settings.Count == 0)
		{
			return;
		}

		sb.AppendLine();
		sb.AppendLine("## Root Settings");
		sb.AppendLine();
		foreach (var setting in settings)
		{
			sb.AppendLine("- " + setting);
		}
	}

	private static string FormatMatcher(XElement element)
	{
		var attributes = element.Attributes()
			.Where(attribute => attribute.Name.LocalName is not "description" and not "comment")
			.Select(attribute => attribute.Name.LocalName + "=\"" + Escape(attribute.Value) + "\"")
			.ToArray();
		var result = attributes.Length == 0 ? "(no matcher attributes)" : string.Join(" ", attributes);

		return result;
	}

	private static void AppendDescription(StringBuilder sb, XElement element, int depth)
	{
		var description = element.Attribute("description")?.Value ?? element.Attribute("comment")?.Value;
		if (!string.IsNullOrWhiteSpace(description))
		{
			AppendLine(sb, depth, "- Description: " + Escape(description));
		}
	}

	private static void AddSetting(List<string> settings, XElement element, string attributeName)
	{
		var attribute = element.Attribute(attributeName);
		if (attribute is not null)
		{
			settings.Add("`" + attributeName + "` = `" + Escape(attribute.Value) + "`");
		}
	}

	private static void AddAttribute(List<string> settings, XElement element, string attributeName)
	{
		var attribute = element.Attribute(attributeName);
		if (attribute is not null)
		{
			settings.Add(attributeName + "=\"" + Escape(attribute.Value) + "\"");
		}
	}

	private static void AppendLine(StringBuilder sb, int depth, string text)
	{
		sb.Append(new string(' ', depth * 2));
		sb.AppendLine(text);
	}

	private static string Escape(string? value)
	{
		var result = (value ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

		return result;
	}
}
