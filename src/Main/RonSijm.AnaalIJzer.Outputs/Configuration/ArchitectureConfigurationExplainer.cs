using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.Outputs.Configuration;

internal static partial class ArchitectureConfigurationExplainer
{
	public static string GenerateMarkdown(string configPath)
	{
		var fullPath = Path.GetFullPath(configPath);
		XDocument document;
		try
		{
			document = XDocument.Load(fullPath, LoadOptions.SetLineInfo);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
		{
			throw new OutputGenerationException($"Could not read configuration file {fullPath}: {ex.Message}");
		}

		if (document.Root?.Name.LocalName != "ArchitecturalLevels")
		{
			throw new OutputGenerationException($"Configuration root must be <ArchitecturalLevels>: {fullPath}");
		}

		var sb = new StringBuilder();
		sb.AppendLine("# Architecture Configuration Explanation");
		sb.AppendLine();
		sb.AppendLine($"Source: `{Escape(Path.GetFileName(fullPath))}`");
		AppendRootSettings(sb, document.Root);
		AppendElements(sb, document.Root.Elements(), 0);
		var result = sb.ToString().TrimEnd() + Environment.NewLine;

		return result;
	}
}
