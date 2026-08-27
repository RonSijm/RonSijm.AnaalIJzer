using System.Collections.Immutable;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Sources;

namespace RonSijm.AnaalIJzer.GraphModel.Loading;

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
			if (include.Attribute("path")?.Value is not string includePathValue)
			{
				continue;
			}

			includePathValue = includePathValue.Trim();
			if (string.IsNullOrWhiteSpace(includePathValue))
			{
				continue;
			}

			var includedPaths = ArchitectureConfigurationIncludeResolver.ResolveFileSystemPaths(fullPath, includePathValue);
			if (includedPaths.Length == 0)
			{
				throw new InvalidOperationException(ArchitectureConfigurationIncludeResolver.CreateMissingIncludeMessage(includePathValue));
			}

			foreach (var includedFullPath in includedPaths)
			{
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
	}

	private sealed class ConfigurationDocumentPart(XElement root, string sourcePath, ArchitectureConfigurationSourceKind sourceKind)
	{
		public XElement Root { get; } = root;

		public string SourcePath { get; } = sourcePath;

		public ArchitectureConfigurationSourceKind SourceKind { get; } = sourceKind;
	}
}
