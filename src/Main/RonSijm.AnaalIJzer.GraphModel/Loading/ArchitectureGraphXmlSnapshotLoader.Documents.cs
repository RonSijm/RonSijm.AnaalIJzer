using System.Collections.Immutable;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.Graphing.Loading;

public static partial class ArchitectureGraphXmlSnapshotLoader
{
	private static void CollectConfigurationDocuments(
		XElement root,
		string sourcePath,
		ArchitectureConfigurationSourceKind sourceKind,
		ImmutableArray<ConfigurationDocumentPart>.Builder documents,
		HashSet<string> visitedPaths)
	{
		var fullPath = Path.GetFullPath(sourcePath);
		if (!visitedPaths.Add(fullPath))
		{
			return;
		}

		documents.Add(new ConfigurationDocumentPart(root, fullPath, sourceKind));
		foreach (var include in root.Elements().Where(element => IsElement(element, "Include")))
		{
			var includePath = include.Attribute("path")?.Value?.Trim();
			if (string.IsNullOrWhiteSpace(includePath))
			{
				continue;
			}

			var includedFullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fullPath) ?? string.Empty, includePath));
			var includedSource = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, includedFullPath);
			if (!ArchitectureConfigurationDocumentLoader.TryReadConfigurationDocument(includedSource, out var includedDocument, out var message) || includedDocument?.Root is null)
			{
				throw new InvalidOperationException(message);
			}

			if (!IsElement(includedDocument.Root, "ArchitecturalLevels"))
			{
				throw new InvalidOperationException("Included AnaalIJzer configuration does not have an <ArchitecturalLevels> root element: " + includedFullPath);
			}

			CollectConfigurationDocuments(includedDocument.Root, includedFullPath, ArchitectureConfigurationSourceKind.XmlFile, documents, visitedPaths);
		}
	}

	private sealed class ConfigurationDocumentPart
	{
		public ConfigurationDocumentPart(XElement root, string sourcePath, ArchitectureConfigurationSourceKind sourceKind)
		{
			Root = root;
			SourcePath = sourcePath;
			SourceKind = sourceKind;
		}

		public XElement Root { get; }

		public string SourcePath { get; }

		public ArchitectureConfigurationSourceKind SourceKind { get; }
	}
}
