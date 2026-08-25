using System.Xml;
using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Document;

public static partial class ArchitectureConfigurationFileService
{
	private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

	private static FlattenedConfiguration LoadFlattened(IEnumerable<string> inputPaths, CancellationToken cancellationToken)
	{
		var documents = new List<ConfigurationDocument>();
		var elements = new List<ConfigurationElement>();
		var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var inputPath in inputPaths)
		{
			CollectFile(Path.GetFullPath(inputPath), documents, elements, visited, cancellationToken);
		}

		var result = new FlattenedConfiguration(documents, elements);

		return result;
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
			throw new ArchitectureConfigurationFileOperationException($"Configuration file not found: {path}");
		}

		XDocument document;
		try
		{
			document = ArchitectureConfigurationDocumentLoader.LoadXmlFile(path);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
		{
			throw new ArchitectureConfigurationFileOperationException($"Could not read configuration file {path}: {ex.Message}");
		}

		if (document.Root?.Name.LocalName != "ArchitecturalLevels")
		{
			throw new ArchitectureConfigurationFileOperationException($"Configuration root must be <ArchitecturalLevels>: {path}");
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
				throw new ArchitectureConfigurationFileOperationException($"Include without a path in {path}.");
			}

			var resolvedInclude = GetAbsolutePath(includePath!, Path.GetDirectoryName(path)!);
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

		var absolutePath = GetAbsolutePath(configuredPath, Path.GetDirectoryName(sourceFilePath)!);
		var outputDirectory = Path.GetDirectoryName(outputFilePath)!;
		var result = GetRelativePath(outputDirectory, absolutePath);

		return result;
	}

	private static string GetAbsolutePath(string path, string baseDirectory)
	{
		var combined = Path.IsPathRooted(path) ? path : Path.Combine(baseDirectory, path);
		var result = Path.GetFullPath(combined);

		return result;
	}

	private static string GetRelativePath(string relativeToDirectory, string targetPath)
	{
		var relativeDirectory = AppendDirectorySeparatorChar(Path.GetFullPath(relativeToDirectory));
		var target = Path.GetFullPath(targetPath);
		var relativeUri = new Uri(relativeDirectory, UriKind.Absolute);
		var targetUri = new Uri(target, UriKind.Absolute);
		var relative = Uri.UnescapeDataString(relativeUri.MakeRelativeUri(targetUri).ToString().Replace('/', Path.DirectorySeparatorChar));

		return relative;
	}

	private static string AppendDirectorySeparatorChar(string path)
	{
		var result = path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
			? path
			: path + Path.DirectorySeparatorChar;

		return result;
	}

	private sealed class ConfigurationDocument
	{
		public ConfigurationDocument(XElement root, string path)
		{
			Root = root;
			Path = path;
		}

		public XElement Root { get; }

		public string Path { get; }
	}

	private sealed class ConfigurationElement
	{
		public ConfigurationElement(XElement element)
		{
			Element = element;
		}

		public XElement Element { get; }
	}

	private sealed class FlattenedConfiguration
	{
		public FlattenedConfiguration(IReadOnlyList<ConfigurationDocument> documents, IReadOnlyList<ConfigurationElement> elements)
		{
			Documents = documents;
			Elements = elements;
		}

		public IReadOnlyList<ConfigurationDocument> Documents { get; }

		public IReadOnlyList<ConfigurationElement> Elements { get; }
	}
}
