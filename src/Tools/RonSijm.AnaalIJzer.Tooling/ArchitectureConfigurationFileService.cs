using System.Text;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Parsing;

namespace RonSijm.AnaalIJzer.Tooling;

internal static class ArchitectureConfigurationFileService
{
	private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

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
			throw new ToolingException("The configuration contains only one dependency graph; there is nothing to split.");
		}

		var files = new List<(string Path, string Content)>();
		var manifestPath = Path.Combine(outputDirectory, ArchitecturalConfigParser.ConfigFileName);
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

	private static FlattenedConfiguration LoadFlattened(IEnumerable<string> inputPaths, CancellationToken cancellationToken)
	{
		var documents = new List<ConfigurationDocument>();
		var elements = new List<ConfigurationElement>();
		var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var inputPath in inputPaths)
		{
			CollectFile(Path.GetFullPath(inputPath), documents, elements, visited, cancellationToken);
		}

		return new FlattenedConfiguration(documents, elements);
	}

	private static void CollectFile(string path, List<ConfigurationDocument> documents, List<ConfigurationElement> elements, HashSet<string> visited, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (!visited.Add(path))
		{
			return;
		}

		if (!File.Exists(path))
		{
			throw new ToolingException($"Configuration file not found: {path}");
		}

		XDocument document;
		try
		{
			document = XDocument.Load(path, LoadOptions.SetLineInfo);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
		{
			throw new ToolingException($"Could not read configuration file {path}: {ex.Message}");
		}

		if (document.Root?.Name.LocalName != "ArchitecturalLevels")
		{
			throw new ToolingException($"Configuration root must be <ArchitecturalLevels>: {path}");
		}

		documents.Add(new ConfigurationDocument(document.Root, path));
		foreach (var child in document.Root.Elements())
		{
			if (child.Name.LocalName != "Include")
			{
				elements.Add(new ConfigurationElement(new XElement(child)));
				continue;
			}

			var includePath = child.Attribute("path")?.Value;
			if (string.IsNullOrWhiteSpace(includePath))
			{
				throw new ToolingException($"Include without a path in {path}.");
			}

			var resolvedInclude = Path.GetFullPath(includePath, Path.GetDirectoryName(path)!);
			CollectFile(resolvedInclude, documents, elements, visited, cancellationToken);
		}
	}

	private static XElement CreateRoot(FlattenedConfiguration configuration, string outputPath, bool includeSettings)
	{
		var root = new XElement("ArchitecturalLevels");
		var schemaDocument = configuration.Documents.FirstOrDefault(document => document.Root.Attribute(Xsi + "noNamespaceSchemaLocation") is not null);
		var schemaPath = schemaDocument?.Root.Attribute(Xsi + "noNamespaceSchemaLocation")?.Value;
		if (schemaDocument is not null && !string.IsNullOrWhiteSpace(schemaPath))
		{
			root.Add(new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName));
			root.Add(new XAttribute(Xsi + "noNamespaceSchemaLocation", RebasePath(schemaPath!, schemaDocument.Path, outputPath)));
		}

		if (!includeSettings)
		{
			return root;
		}

		var description = configuration.Documents.Select(document => document.Root.Attribute("description")?.Value).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
		if (description is not null)
		{
			root.Add(new XAttribute("description", description));
		}

		var requiredRecognizedDependencySites = configuration.Documents
			.SelectMany(document => (document.Root.Attribute("requireRecognizedDependencies")?.Value ?? string.Empty).Split(','))
			.Select(site => site.Trim())
			.Where(site => site.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
		if (requiredRecognizedDependencySites.Length > 0)
		{
			root.Add(new XAttribute("requireRecognizedDependencies", string.Join(", ", requiredRecognizedDependencySites)));
		}

		if (configuration.Documents.Any(document => IsTrue(document.Root, "enforceAcyclic")))
		{
			root.Add(new XAttribute("enforceAcyclic", "true"));
		}

		AddOutputSettings(root, configuration.Documents, outputPath, "enableReport", "reportPath", "architectural-violations.md");
		AddOutputSettings(root, configuration.Documents, outputPath, "enableDocumentation", "documentationPath", "architecture-documentation.md");
		return root;
	}

	private static void AddOutputSettings(XElement root, IReadOnlyList<ConfigurationDocument> documents, string outputPath, string enableAttribute, string pathAttribute, string defaultFileName)
	{
		var enabledDocument = documents.FirstOrDefault(document => IsTrue(document.Root, enableAttribute));
		if (enabledDocument is not null)
		{
			root.Add(new XAttribute(enableAttribute, "true"));
			var configuredPath = enabledDocument.Root.Attribute(pathAttribute)?.Value ?? defaultFileName;
			root.Add(new XAttribute(pathAttribute, RebasePath(configuredPath, enabledDocument.Path, outputPath)));
			return;
		}

		var pathDocument = documents.FirstOrDefault(document => document.Root.Attribute(pathAttribute) is not null);
		var configuredValue = pathDocument?.Root.Attribute(pathAttribute)?.Value;
		if (pathDocument is not null && configuredValue is not null)
		{
			root.Add(new XAttribute(pathAttribute, RebasePath(configuredValue, pathDocument.Path, outputPath)));
		}
	}

	private static bool IsTrue(XElement root, string attributeName)
	{
		var result = bool.TryParse(root.Attribute(attributeName)?.Value, out var value) && value;

		return result;
	}

	private static string RebasePath(string configuredPath, string sourceFilePath, string outputFilePath)
	{
		if (Uri.TryCreate(configuredPath, UriKind.Absolute, out var uri) && !uri.IsFile)
		{
			return configuredPath;
		}

		var absolutePath = Path.IsPathRooted(configuredPath)
			? Path.GetFullPath(configuredPath)
			: Path.GetFullPath(configuredPath, Path.GetDirectoryName(sourceFilePath)!);
		var outputDirectory = Path.GetDirectoryName(outputFilePath)!;
		return Path.GetRelativePath(outputDirectory, absolutePath);
	}

	private static IReadOnlyList<GraphComponent> FindGraphComponents(IReadOnlyList<ConfigurationElement> elements)
	{
		var layerNames = elements
			.Where(item => item.Element.Name.LocalName == "Layer")
			.Select(item => item.Element.Attribute("name")?.Value)
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Cast<string>()
			.Distinct(StringComparer.Ordinal)
			.ToList();

		if (layerNames.Count == 0)
		{
			throw new ToolingException("The configuration does not define any dependency graph nodes.");
		}

		var sets = new DisjointSet(layerNames);
		foreach (var item in elements)
		{
			if (item.Element.Name.LocalName is "AllowedDependency" or "BlockedDependency")
			{
				UnionEdgeRoots(item.Element, null, layerNames, sets);
			}
			else if (item.Element.Name.LocalName == "Layer" && item.Element.Attribute("name")?.Value is { } ownerRoot)
			{
				foreach (var edge in item.Element.Descendants().Where(element => element.Name.LocalName is "AllowedDependency" or "BlockedDependency"))
				{
					UnionEdgeRoots(edge, ownerRoot, layerNames, sets);
				}
			}
		}

		var componentNames = new Dictionary<string, List<string>>(StringComparer.Ordinal);
		foreach (var layerName in layerNames)
		{
			var root = sets.Find(layerName);
			if (!componentNames.TryGetValue(root, out var names))
			{
				names = [];
				componentNames.Add(root, names);
			}

			names.Add(layerName);
		}

		var components = componentNames.Select(pair => new GraphComponent(pair.Value, [])).ToList();
		var componentsByLayer = components.SelectMany(component => component.LayerNames.Select(name => (name, component)))
			.ToDictionary(item => item.name, item => item.component, StringComparer.Ordinal);
		foreach (var item in elements)
		{
			GraphComponent? component = null;
			if (item.Element.Name.LocalName == "Layer" && item.Element.Attribute("name")?.Value is { } layerName)
			{
				componentsByLayer.TryGetValue(layerName, out component);
			}
			else if (item.Element.Name.LocalName is "AllowedDependency" or "BlockedDependency")
			{
				var from = item.Element.Attribute("from")?.Value;
				var to = item.Element.Attribute("to")?.Value;
				var namedEndpoint = GetRootReference(from) ?? GetRootReference(to);
				if (from == "*" && to == "*")
				{
					namedEndpoint = layerNames[0];
				}

				if (namedEndpoint is not null)
				{
					componentsByLayer.TryGetValue(namedEndpoint, out component);
				}
			}

			component?.Elements.Add(item);
		}

		return components;
	}

	private static void UnionEdgeRoots(XElement edge, string? ownerRoot, IReadOnlyCollection<string> layerNames, DisjointSet sets)
	{
		var fromReference = edge.Attribute("from")?.Value;
		var toReference = edge.Attribute("to")?.Value;
		var from = ownerRoot is not null && fromReference?.StartsWith("/", StringComparison.Ordinal) != true ? ownerRoot : GetRootReference(fromReference);
		var to = ownerRoot is not null && toReference?.StartsWith("/", StringComparison.Ordinal) != true ? ownerRoot : GetRootReference(toReference);
		if (edge.Attribute("from")?.Value == "*" || edge.Attribute("to")?.Value == "*")
		{
			foreach (var layerName in layerNames)
			{
				if (ownerRoot is not null)
				{
					sets.Union(ownerRoot, layerName);
				}
			}
			return;
		}

		if (from is not null && to is not null && layerNames.Contains(from) && layerNames.Contains(to))
		{
			sets.Union(from, to);
		}
	}

	private static string? GetRootReference(string? reference)
	{
		if (string.IsNullOrWhiteSpace(reference) || reference == "*")
		{
			return null;
		}

		var normalized = reference!.TrimStart('/');
		var separator = normalized.IndexOf('/');
		return separator < 0 ? normalized : normalized.Substring(0, separator);
	}

	private static string CreateGraphFileName(int index, IReadOnlyList<string> layerNames)
	{
		var descriptiveName = string.Join("-", layerNames.Take(3).Select(SanitizeFileName));
		if (string.IsNullOrWhiteSpace(descriptiveName))
		{
			descriptiveName = "Unnamed";
		}

		if (descriptiveName.Length > 72)
		{
			descriptiveName = descriptiveName[..72].TrimEnd('-');
		}

		return $"Graph.{index + 1:D2}.{descriptiveName}.anl";
	}

	private static string SanitizeFileName(string value)
	{
		var invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
		var characters = value.Select(character => invalidCharacters.Contains(character) || char.IsWhiteSpace(character) ? '-' : character).ToArray();
		return new string(characters).Trim('-');
	}

	private static string Serialize(XElement root)
	{
		var result = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine + root + Environment.NewLine;

		return result;
	}

    private static async Task WriteFilesAsync(IReadOnlyList<(string Path, string Content)> files, string outputDirectory, bool force, CancellationToken cancellationToken)
	{
		if (File.Exists(outputDirectory))
		{
			throw new ToolingException($"Output directory is a file: {outputDirectory}");
		}

		var existingFile = files.Select(file => file.Path).FirstOrDefault(File.Exists);
		if (existingFile is not null && !force)
		{
			throw new ToolingException($"Output already exists: {existingFile}. Enable overwrite to replace generated files.");
		}

		Directory.CreateDirectory(outputDirectory);
		foreach (var file in files)
		{
			await File.WriteAllTextAsync(file.Path, file.Content, new UTF8Encoding(false), cancellationToken);
		}
	}

	private static async Task WriteFileAsync(string outputPath, string content, bool force, CancellationToken cancellationToken)
	{
		if (File.Exists(outputPath) && !force)
		{
			throw new ToolingException($"Output already exists: {outputPath}. Enable overwrite to replace it.");
		}

		Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
		await File.WriteAllTextAsync(outputPath, content, new UTF8Encoding(false), cancellationToken);
	}

	private sealed record ConfigurationDocument(XElement Root, string Path);
	private sealed record ConfigurationElement(XElement Element);
	private sealed record FlattenedConfiguration(IReadOnlyList<ConfigurationDocument> Documents, IReadOnlyList<ConfigurationElement> Elements);
	private sealed record GraphComponent(IReadOnlyList<string> LayerNames, List<ConfigurationElement> Elements);

	private sealed class DisjointSet
	{
		private readonly Dictionary<string, string> _parents;

		public DisjointSet(IEnumerable<string> values)
		{
			_parents = values.ToDictionary(value => value, value => value, StringComparer.Ordinal);
		}

		public string Find(string value)
		{
			if (!_parents.TryGetValue(value, out var parent))
			{
				_parents[value] = value;
				return value;
			}

			if (parent != value)
			{
				_parents[value] = Find(parent);
			}

			return _parents[value];
		}

		public void Union(string left, string right)
		{
			var leftRoot = Find(left);
			var rightRoot = Find(right);
			if (leftRoot != rightRoot)
			{
				_parents[rightRoot] = leftRoot;
			}
		}
	}
}
