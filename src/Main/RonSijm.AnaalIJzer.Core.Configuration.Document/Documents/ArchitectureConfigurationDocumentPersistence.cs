using System.Text;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Persistence;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;

public static class ArchitectureConfigurationDocumentPersistence
{
	public static ArchitectureConfigurationDocumentOperationResult CreateXmlFile(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("Choose where Architecture.anl should be created.");
		}

		var fullPath = Path.GetFullPath(path);
		if (File.Exists(fullPath))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("Architecture configuration already exists: " + fullPath);
		}

		var directory = Path.GetDirectoryName(fullPath);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}

		var document = new XDocument(
			new XDeclaration("1.0", "utf-8", null),
			new XElement("ArchitecturalLevels"));
		File.WriteAllText(fullPath, ArchitectureConfigurationXmlSerializer.SerializeXml(document), Encoding.UTF8);

		var result = ArchitectureConfigurationDocumentOperationResult.Success("Created AnaalIJzer configuration: " + fullPath);

		return result;
	}

	public static ArchitectureConfigurationDocumentOperationResult EditConfiguration(
		ArchitectureConfigurationSource source,
		Func<XDocument, ArchitectureConfigurationDocumentOperationResult> edit)
	{
		var result = EditConfiguration(source.Kind, source.Path, edit);

		return result;
	}

	public static ArchitectureConfigurationDocumentOperationResult EditConfiguration(
		ArchitectureConfigurationSourceKind sourceKind,
		string sourcePath,
		Func<XDocument, ArchitectureConfigurationDocumentOperationResult> edit)
	{
		if (sourceKind == ArchitectureConfigurationSourceKind.XmlFile)
		{
			return EditXmlFile(sourcePath, edit);
		}

		if (sourceKind == ArchitectureConfigurationSourceKind.InlineAssemblyMetadata)
		{
			return EditInlineAssemblyMetadata(sourcePath, edit);
		}

		return ArchitectureConfigurationDocumentOperationResult.Failure("This architecture configuration source cannot be edited.");
	}

	public static ArchitectureConfigurationDocumentOperationResult ReadConfiguration(
		ArchitectureConfigurationSource source,
		out XDocument? document)
	{
		var result = ReadConfiguration(source.Kind, source.Path, out document);

		return result;
	}

	public static ArchitectureConfigurationDocumentOperationResult ReadConfiguration(
		ArchitectureConfigurationSourceKind sourceKind,
		string sourcePath,
		out XDocument? document)
	{
		var source = new ArchitectureConfigurationSource(sourceKind, sourcePath);
		if (!ArchitectureConfigurationDocumentLoader.TryReadConfigurationDocument(source, out document, out var message))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure(message);
		}

		return ArchitectureConfigurationDocumentOperationResult.Success("Loaded architecture configuration.");
	}

	private static ArchitectureConfigurationDocumentOperationResult EditXmlFile(
		string path,
		Func<XDocument, ArchitectureConfigurationDocumentOperationResult> edit)
	{
		if (!File.Exists(path))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("Architecture configuration file does not exist: " + path);
		}

		var document = ArchitectureConfigurationDocumentLoader.LoadXmlFile(path);
		var result = edit(document);
		if (!result.Succeeded)
		{
			return result;
		}

		File.WriteAllText(path, ArchitectureConfigurationXmlSerializer.SerializeXml(document), Encoding.UTF8);
		return result;
	}

	private static ArchitectureConfigurationDocumentOperationResult EditInlineAssemblyMetadata(
		string path,
		Func<XDocument, ArchitectureConfigurationDocumentOperationResult> edit)
	{
		if (!File.Exists(path))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("Inline settings source file does not exist: " + path);
		}

		var source = File.ReadAllText(path);
		if (!InlineAssemblyMetadataSettings.TryFindInlineSettings(source, out var settings, out var message))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure(message);
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
			return ArchitectureConfigurationDocumentOperationResult.Failure(message);
		}

		var updatedSource = source.Remove(settings.LiteralSpan.Start, settings.LiteralSpan.Length)
			.Insert(settings.LiteralSpan.Start, updatedLiteral);
		File.WriteAllText(path, updatedSource, Encoding.UTF8);

		return result;
	}
}
