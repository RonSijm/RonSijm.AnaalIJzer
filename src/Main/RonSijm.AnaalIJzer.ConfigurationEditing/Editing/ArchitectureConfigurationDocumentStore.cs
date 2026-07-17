using System.Text;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

internal static class ArchitectureConfigurationDocumentStore
{
	internal static ArchitectureConfigurationEditResult EditConfiguration(ArchitectureConfigurationSourceKind sourceKind, string sourcePath, Func<XDocument, ArchitectureConfigurationEditResult> edit)
	{
		if (sourceKind == ArchitectureConfigurationSourceKind.XmlFile)
		{
			return EditXmlFile(sourcePath, edit);
		}

		if (sourceKind == ArchitectureConfigurationSourceKind.InlineAssemblyMetadata)
		{
			return EditInlineAssemblyMetadata(sourcePath, edit);
		}

		return ArchitectureConfigurationEditResult.Failure("This architecture configuration source cannot be edited.");
	}

	internal static ArchitectureConfigurationEditResult EditXmlFile(string path, Func<XDocument, ArchitectureConfigurationEditResult> edit)
	{
		if (!File.Exists(path))
		{
			return ArchitectureConfigurationEditResult.Failure("Architecture configuration file does not exist: " + path);
		}

		var document = LoadXmlFile(path);
		var result = edit(document);
		if (!result.Succeeded)
		{
			return result;
		}

		File.WriteAllText(path, ArchitectureConfigurationXmlSerializer.SerializeXml(document), Encoding.UTF8);
		return result;
	}

	internal static ArchitectureConfigurationEditResult EditInlineAssemblyMetadata(string path, Func<XDocument, ArchitectureConfigurationEditResult> edit)
	{
		if (!File.Exists(path))
		{
			return ArchitectureConfigurationEditResult.Failure("Inline settings source file does not exist: " + path);
		}

		var source = File.ReadAllText(path);
		if (!InlineAssemblyMetadataSettings.TryFindInlineSettings(source, out var settings, out var message))
		{
			return ArchitectureConfigurationEditResult.Failure(message);
		}

		var document = XDocument.Parse(settings.Xml, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
		var result = edit(document);
		if (!result.Succeeded)
		{
			return result;
		}

		var updatedXml = ArchitectureConfigurationXmlSerializer.SerializeXml(document);
		if (!InlineAssemblyMetadataSettings.TryCreateInlineSettingsLiteral(settings, updatedXml, InlineAssemblyMetadataSettings.DetectNewLine(source), out var updatedLiteral, out message))
		{
			return ArchitectureConfigurationEditResult.Failure(message);
		}

		var updatedSource = source.Remove(settings.LiteralSpan.Start, settings.LiteralSpan.Length)
			.Insert(settings.LiteralSpan.Start, updatedLiteral);
		File.WriteAllText(path, updatedSource, Encoding.UTF8);

		return result;
	}

	internal static ArchitectureConfigurationEditResult ReadConfiguration(ArchitectureConfigurationSourceKind sourceKind, string sourcePath, out XDocument? document)
	{
		if (sourceKind == ArchitectureConfigurationSourceKind.XmlFile)
		{
			return ReadXmlFile(sourcePath, out document);
		}

		if (sourceKind == ArchitectureConfigurationSourceKind.InlineAssemblyMetadata)
		{
			return ReadInlineAssemblyMetadata(sourcePath, out document);
		}

		document = null;
		return ArchitectureConfigurationEditResult.Failure("This architecture configuration source cannot be inspected.");
	}

	internal static ArchitectureConfigurationEditResult ReadXmlFile(string path, out XDocument? document)
	{
		if (!File.Exists(path))
		{
			document = null;
			return ArchitectureConfigurationEditResult.Failure("Architecture configuration file does not exist: " + path);
		}

		document = LoadXmlFile(path);
		return ArchitectureConfigurationEditResult.Success("Loaded architecture configuration.");
	}

	internal static XDocument LoadXmlFile(string path)
	{
		var xml = ReadXmlText(path);
		var result = XDocument.Parse(xml, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

		return result;
	}

	internal static string ReadXmlText(string path)
	{
		using var reader = new StreamReader(path, Encoding.UTF8, true);
		var result = reader.ReadToEnd();

		return result;
	}

	internal static ArchitectureConfigurationEditResult ReadInlineAssemblyMetadata(string path, out XDocument? document)
	{
		if (!File.Exists(path))
		{
			document = null;
			return ArchitectureConfigurationEditResult.Failure("Inline settings source file does not exist: " + path);
		}

		var source = File.ReadAllText(path);
		if (!InlineAssemblyMetadataSettings.TryFindInlineSettings(source, out var settings, out var message))
		{
			document = null;
			return ArchitectureConfigurationEditResult.Failure(message);
		}

		document = XDocument.Parse(settings.Xml, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
		return ArchitectureConfigurationEditResult.Success("Loaded inline architecture configuration.");
	}

}
