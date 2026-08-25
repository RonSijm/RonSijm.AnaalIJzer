using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Document;

public static partial class ArchitectureConfigurationFileService
{
	public static async Task FormatAsync(string inputPath, string outputPath, bool force, CancellationToken cancellationToken)
	{
		var fullInputPath = Path.GetFullPath(inputPath);
		var fullOutputPath = Path.GetFullPath(outputPath);
		XDocument document;
		try
		{
			document = ArchitectureConfigurationDocumentLoader.LoadXmlFile(fullInputPath);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
		{
			throw new ArchitectureConfigurationFileOperationException($"Could not read configuration file {fullInputPath}: {ex.Message}");
		}

		if (document.Root?.Name.LocalName != "ArchitecturalLevels")
		{
			throw new ArchitectureConfigurationFileOperationException($"Configuration root must be <ArchitecturalLevels>: {fullInputPath}");
		}

		var sameFile = string.Equals(fullInputPath, fullOutputPath, StringComparison.OrdinalIgnoreCase);
		if (File.Exists(fullOutputPath) && !force && !sameFile)
		{
			throw new ArchitectureConfigurationFileOperationException($"Output already exists: {fullOutputPath}. Enable overwrite to replace it.");
		}

		Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
		await WriteTextAsync(fullOutputPath, SerializeDocument(document), cancellationToken);
	}

	public static async Task MergeAsync(IReadOnlyList<string> inputPaths, string outputPath, bool force, CancellationToken cancellationToken)
	{
		var configuration = LoadFlattened(inputPaths, cancellationToken);
		var root = CreateRoot(configuration, outputPath, includeSettings: true);
		root.Add(configuration.Elements.Select(item => new XElement(item.Element)));
		await WriteFileAsync(outputPath, Serialize(root), force, cancellationToken);
	}

	public static async Task<int> SplitAsync(string inputPath, string outputDirectory, bool force, CancellationToken cancellationToken)
	{
		var configuration = LoadFlattened([inputPath], cancellationToken);
		var components = FindGraphComponents(configuration.Elements);
		if (components.Count <= 1)
		{
			throw new ArchitectureConfigurationFileOperationException("The configuration contains only one dependency graph; there is nothing to split.");
		}

		var files = new List<(string Path, string Content)>();
		var manifestPath = Path.Combine(outputDirectory, ArchitectureConfigurationDocumentLoader.ConfigFileName);
		var manifestRoot = CreateRoot(configuration, manifestPath, includeSettings: true);
		var sharedElements = configuration.Elements.Where(item => item.Element.Name.LocalName is not "Layer" and not "AllowedDependency" and not "BlockedDependency").ToArray();
		if (sharedElements.Length > 0)
		{
			var sharedPath = Path.Combine(outputDirectory, "Shared.anl");
			var sharedRoot = CreateRoot(configuration, sharedPath, includeSettings: false);
			sharedRoot.Add(sharedElements.Select(item => new XElement(item.Element)));
			files.Add((sharedPath, Serialize(sharedRoot)));
			manifestRoot.Add(new XElement("Include", new XAttribute("path", Path.GetFileName(sharedPath))));
		}

		for (var index = 0; index < components.Count; index++)
		{
			var component = components[index];
			var graphPath = Path.Combine(outputDirectory, CreateGraphFileName(index, component.LayerNames));
			var graphRoot = CreateRoot(configuration, graphPath, includeSettings: false);
			graphRoot.Add(component.Elements.Select(item => new XElement(item.Element)));
			files.Add((graphPath, Serialize(graphRoot)));
			manifestRoot.Add(new XElement("Include", new XAttribute("path", Path.GetFileName(graphPath))));
		}

		files.Insert(0, (manifestPath, Serialize(manifestRoot)));
		await WriteFilesAsync(files, outputDirectory, force, cancellationToken);
		return components.Count;
	}
}
