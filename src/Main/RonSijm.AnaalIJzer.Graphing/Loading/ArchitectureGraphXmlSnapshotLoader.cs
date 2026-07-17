using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.ConfigurationEditing.Sites;
using RonSijm.AnaalIJzer.Graphing.Model;

namespace RonSijm.AnaalIJzer.Graphing.Loading;

public static class ArchitectureGraphXmlSnapshotLoader
{
	public static ArchitectureGraphSnapshot Load(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentException("Choose an Architecture.anl file first.", nameof(path));
		}

		var fullPath = Path.GetFullPath(path);
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, fullPath);
		var result = Load(source);

		return result;
	}

	public static ArchitectureGraphSnapshot Load(ArchitectureConfigurationSource source)
	{
		if (!source.CanEdit)
		{
			throw new InvalidOperationException("Choose an editable AnaalIJzer configuration source first.");
		}

		var fullPath = Path.GetFullPath(source.Path);
		var normalizedSource = new ArchitectureConfigurationSource(source.Kind, fullPath);
		var readResult = ArchitectureConfigurationEditService.ReadConfiguration(normalizedSource, out var document);
		if (!readResult.Succeeded || document is null)
		{
			throw new InvalidOperationException(readResult.Message);
		}

		if (document.Root is null || !IsElement(document.Root, "ArchitecturalLevels"))
		{
			throw new InvalidOperationException("The selected AnaalIJzer configuration does not have an <ArchitecturalLevels> root element.");
		}

		var documents = ImmutableArray.CreateBuilder<ConfigurationDocumentPart>();
		CollectConfigurationDocuments(document.Root, fullPath, normalizedSource.Kind, documents, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
		var layers = ImmutableArray.CreateBuilder<ArchitectureGraphLayer>();
		foreach (var configurationDocument in documents)
		{
			CollectLayers(configurationDocument.Root, string.Empty, configurationDocument.SourcePath, configurationDocument.SourceKind, layers);
		}

		if (layers.Count == 0)
		{
			throw new InvalidOperationException("No <Layer> elements were found in " + fullPath + ".");
		}

		var layerPaths = layers.Select(layer => layer.Path).ToImmutableHashSet(StringComparer.Ordinal);
		var rules = ImmutableArray.CreateBuilder<ArchitectureGraphRule>();
		foreach (var configurationDocument in documents)
		{
			CollectRules(configurationDocument.Root, string.Empty, configurationDocument.SourcePath, configurationDocument.SourceKind, layerPaths, rules);
		}

		var result = new ArchitectureGraphSnapshot(
			true,
			false,
			layers.ToImmutable(),
			rules.ToImmutable(),
			ImmutableArray<string>.Empty,
			ImmutableArray<string>.Empty,
			normalizedSource);

		return result;
	}

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

			var includedFullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fullPath) ?? string.Empty, includePath!));
			var includedSource = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, includedFullPath);
			var readResult = ArchitectureConfigurationEditService.ReadConfiguration(includedSource, out var includedDocument);
			if (!readResult.Succeeded || includedDocument?.Root is null)
			{
				throw new InvalidOperationException(readResult.Message);
			}

			if (!IsElement(includedDocument.Root, "ArchitecturalLevels"))
			{
				throw new InvalidOperationException("Included AnaalIJzer configuration does not have an <ArchitecturalLevels> root element: " + includedFullPath);
			}

			CollectConfigurationDocuments(includedDocument.Root, includedFullPath, ArchitectureConfigurationSourceKind.XmlFile, documents, visitedPaths);
		}
	}

	private static void CollectLayers(
		XElement container,
		string parentPath,
		string sourcePath,
		ArchitectureConfigurationSourceKind sourceKind,
		ImmutableArray<ArchitectureGraphLayer>.Builder layers)
	{
		foreach (var layer in container.Elements().Where(element => IsElement(element, "Layer")))
		{
			var name = layer.Attribute("name")?.Value?.Trim();
			if (string.IsNullOrWhiteSpace(name))
			{
				continue;
			}

			var path = string.IsNullOrWhiteSpace(parentPath) ? name! : parentPath + "/" + name;
			var line = (IXmlLineInfo)layer;
			layers.Add(new ArchitectureGraphLayer(
				path,
				name!,
				layer.Attribute("description")?.Value,
				path.Count(character => character == '/'),
				layers.Count % 16 + 1,
				false,
				sourcePath,
				sourceKind,
				line.HasLineInfo() ? line.LineNumber : 0));
			CollectLayers(layer, path, sourcePath, sourceKind, layers);
		}
	}

	private static void CollectRules(
		XElement container,
		string scopePath,
		string sourcePath,
		ArchitectureConfigurationSourceKind sourceKind,
		ImmutableHashSet<string> layerPaths,
		ImmutableArray<ArchitectureGraphRule>.Builder rules)
	{
		foreach (var rule in container.Elements().Where(IsDependencyElement))
		{
			rules.Add(CreateRule(rule, scopePath, sourcePath, sourceKind, layerPaths));
		}

		foreach (var layer in container.Elements().Where(element => IsElement(element, "Layer")))
		{
			var name = layer.Attribute("name")?.Value?.Trim();
			if (string.IsNullOrWhiteSpace(name))
			{
				continue;
			}

			var nextScope = string.IsNullOrWhiteSpace(scopePath) ? name! : scopePath + "/" + name;
			CollectRules(layer, nextScope, sourcePath, sourceKind, layerPaths, rules);
		}
	}

	private static ArchitectureGraphRule CreateRule(
		XElement element,
		string scopePath,
		string sourcePath,
		ArchitectureConfigurationSourceKind sourceKind,
		ImmutableHashSet<string> layerPaths)
	{
		var from = element.Attribute("from")?.Value?.Trim() ?? string.Empty;
		var to = element.Attribute("to")?.Value?.Trim() ?? string.Empty;
		var allowedSites = ParseSites(element.Attribute("allowedSites")?.Value);
		var blockedSites = ParseSites(element.Attribute("blockedSites")?.Value);
		var line = (IXmlLineInfo)element;
		var result = new ArchitectureGraphRule(
			ResolveEndpoint(from, scopePath, layerPaths),
			ResolveEndpoint(to, scopePath, layerPaths),
			scopePath,
			element.Name.LocalName,
			FormatSites(allowedSites, blockedSites),
			IsTrue(element.Attribute("appliesToDescendants")?.Value),
			from == "*" || to == "*",
			false,
			from,
			to,
			sourcePath,
			sourceKind,
			line.HasLineInfo() ? line.LineNumber : 0,
			line.HasLineInfo() ? line.LinePosition : 0,
			allowedSites,
			blockedSites,
			element.Attribute("description")?.Value);

		return result;
	}

	private static string ResolveEndpoint(string endpoint, string scopePath, ImmutableHashSet<string> layerPaths)
	{
		if (endpoint == "*" || string.IsNullOrWhiteSpace(endpoint))
		{
			return endpoint;
		}

		if (endpoint.StartsWith("/", StringComparison.Ordinal))
		{
			return endpoint.TrimStart('/');
		}

		var scopedEndpoint = string.IsNullOrWhiteSpace(scopePath) ? endpoint : scopePath + "/" + endpoint;
		var result = layerPaths.Contains(scopedEndpoint) ? scopedEndpoint : endpoint;

		return result;
	}

	private static ImmutableArray<string> ParseSites(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return ImmutableArray<string>.Empty;
		}

		var selected = value!.Split(',')
			.Select(site => site.Trim())
			.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
		var result = ArchitectureDependencySiteNames.All
			.Where(site => selected.Contains(site))
			.ToImmutableArray();

		return result;
	}

	private static string FormatSites(ImmutableArray<string> allowedSites, ImmutableArray<string> blockedSites)
	{
		if (allowedSites.Length > 0)
		{
			return "allowed sites: " + string.Join(", ", allowedSites);
		}

		if (blockedSites.Length > 0)
		{
			return "blocked sites: " + string.Join(", ", blockedSites);
		}

		return "all sites";
	}

	private static bool IsDependencyElement(XElement element)
	{
		var result = IsElement(element, "AllowedDependency") || IsElement(element, "BlockedDependency");

		return result;
	}

	private static bool IsElement(XElement element, string name)
	{
		var result = string.Equals(element.Name.LocalName, name, StringComparison.Ordinal);

		return result;
	}

	private static bool IsTrue(string? value)
	{
		var result = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
		             || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);

		return result;
	}

	private sealed class ConfigurationDocumentPart
	{
		public XElement Root { get; }
		public string SourcePath { get; }
		public ArchitectureConfigurationSourceKind SourceKind { get; }

		public ConfigurationDocumentPart(XElement root, string sourcePath, ArchitectureConfigurationSourceKind sourceKind)
		{
			Root = root;
			SourcePath = sourcePath;
			SourceKind = sourceKind;
		}
	}
}
