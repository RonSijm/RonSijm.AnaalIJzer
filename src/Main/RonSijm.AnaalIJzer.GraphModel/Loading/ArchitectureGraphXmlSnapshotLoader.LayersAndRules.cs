using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.ConfigurationEditing.Sites;
using RonSijm.AnaalIJzer.Graphing.Model;

namespace RonSijm.AnaalIJzer.Graphing.Loading;

public static partial class ArchitectureGraphXmlSnapshotLoader
{
	private static void CollectLayers(
		XElement container,
		string parentPath,
		string sourcePath,
		ArchitectureConfigurationSourceKind sourceKind,
		ImmutableArray<ArchitectureGraphLayer>.Builder layers)
	{
		foreach (var layer in container.Elements().Where(element => IsElement(element, LayerElementName)))
		{
			var name = layer.Attribute("name")?.Value?.Trim();
			if (string.IsNullOrWhiteSpace(name))
			{
				continue;
			}

			var path = string.IsNullOrWhiteSpace(parentPath) ? name! : parentPath + "/" + name!;
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

		foreach (var layer in container.Elements().Where(element => IsElement(element, LayerElementName)))
		{
			var name = layer.Attribute("name")?.Value?.Trim();
			if (string.IsNullOrWhiteSpace(name))
			{
				continue;
			}

			var nextScope = string.IsNullOrWhiteSpace(scopePath) ? name! : scopePath + "/" + name!;
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

	private static bool IsTrue(string? value)
	{
		var result = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
		             || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);

		return result;
	}
}
