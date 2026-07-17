using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Model;

namespace RonSijm.AnaalIJzer.Parsing;

internal static partial class ArchitecturalConfigParser
{
	private static void AddDocumentationItems(XElement el, string configPath, int depth, string parentLayerPath, ImmutableArray<ArchitectureDocumentationItem>.Builder documentationItems)
	{
		var layerPath = parentLayerPath;
		if (el.Name.LocalName == "Layer" && el.Attribute("name")?.Value is { } layerName)
		{
			layerPath = string.IsNullOrEmpty(parentLayerPath) ? layerName : parentLayerPath + "/" + layerName;
		}

		documentationItems.Add(CreateDocumentationItem(el, configPath, depth, layerPath));

		foreach (var child in el.Elements())
		{
			AddDocumentationItems(child, configPath, depth + 1, layerPath, documentationItems);
		}
	}

	private static ArchitectureDocumentationItem CreateDocumentationItem(XElement el, string configPath, int depth, string layerPath)
	{
		var attributes = el.Attributes()
			.Where(attribute => !attribute.IsNamespaceDeclaration
			                    && !string.Equals(attribute.Name.LocalName, "description", StringComparison.Ordinal)
			                    && !string.Equals(attribute.Name.LocalName, "comment", StringComparison.Ordinal))
			.Select(attribute => new ArchitectureDocumentationAttribute(attribute.Name.LocalName, attribute.Value))
			.ToImmutableArray();

		var line = (IXmlLineInfo)el;
		return new ArchitectureDocumentationItem(el.Name.LocalName, GetDocumentationLabel(el), el.Attribute("description")?.Value, el.Attribute("comment")?.Value, attributes, depth, layerPath, configPath, line.HasLineInfo() ? line.LineNumber : 0);
	}

	private static string GetDocumentationLabel(XElement el)
	{
		var result = el.Name.LocalName switch
		{
			"Include" => el.Attribute("path")?.Value ?? "Include",
			"Layer" => el.Attribute("name")?.Value ?? "Layer",
			"Class" => "Class " + (GetMatcherDisplayName(el) ?? "(no matcher)"),
			"Namespace" => "Namespace " + (GetMatcherDisplayName(el) ?? "(no matcher)"),
			"Assembly" => "Assembly " + (GetMatcherDisplayName(el) ?? "(no matcher)"),
			"AllowedDependency" => $"{el.Attribute("from")?.Value ?? "?"} -> {el.Attribute("to")?.Value ?? "?"}",
			"BlockedDependency" => $"{el.Attribute("from")?.Value ?? "?"} -x-> {el.Attribute("to")?.Value ?? "?"}",
			"Fix" => "Fix " + (el.Attribute("Rename")?.Value ?? string.Empty),
			_ => el.Name.LocalName
		};

		return result;
	}

	private static string? GetMatcherDisplayName(XElement el)
	{
		var attributes = el.Attributes()
			.Where(attribute => IsMatcherAttribute(attribute.Name.LocalName))
			.Select(attribute => $"{attribute.Name.LocalName}=\"{attribute.Value}\"")
			.ToArray();

		return attributes.Length == 0 ? null : string.Join(" ", attributes);
	}

	private static string NormalizePath(string path)
	{
		try
		{
			return Path.GetFullPath(path);
		}
		catch
		{
			return path;
		}
	}
}
