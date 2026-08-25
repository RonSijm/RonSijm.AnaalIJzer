using System.Text;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Document;

public static partial class ArchitectureConfigurationFileService
{
	private static string Serialize(XElement root)
	{
		var document = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement(root));
		var result = ArchitectureConfigurationXmlSerializer.SerializeXml(document).TrimEnd() + Environment.NewLine;

		return result;
	}

	private static string SerializeDocument(XDocument document)
	{
		var result = ArchitectureConfigurationXmlSerializer.SerializeXml(document).TrimEnd() + Environment.NewLine;

		return result;
	}

	private static async Task WriteFilesAsync(IReadOnlyList<(string Path, string Content)> files, string outputDirectory, bool force, CancellationToken cancellationToken)
	{
		if (File.Exists(outputDirectory))
		{
			throw new ArchitectureConfigurationFileOperationException($"Output directory is a file: {outputDirectory}");
		}

		var existingFile = files.Select(file => file.Path).FirstOrDefault(File.Exists);
		if (existingFile is not null && !force)
		{
			throw new ArchitectureConfigurationFileOperationException($"Output already exists: {existingFile}. Enable overwrite to replace generated files.");
		}

		Directory.CreateDirectory(outputDirectory);
		foreach (var file in files)
		{
			await WriteTextAsync(file.Path, file.Content, cancellationToken);
		}
	}

	private static async Task WriteFileAsync(string outputPath, string content, bool force, CancellationToken cancellationToken)
	{
		if (File.Exists(outputPath) && !force)
		{
			throw new ArchitectureConfigurationFileOperationException($"Output already exists: {outputPath}. Enable overwrite to replace it.");
		}

		Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
		await WriteTextAsync(outputPath, content, cancellationToken);
	}

	private static async Task WriteTextAsync(string path, string content, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		await Task.Run(() => File.WriteAllText(path, content, new UTF8Encoding(false)), cancellationToken);
	}
}
