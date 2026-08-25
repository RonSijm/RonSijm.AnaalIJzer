using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Document;

public static class ArchitectureConfigurationDocumentationBuilder
{
	public static void AddDocumentationItems(XElement element, string configPath, int depth, string parentLayerPath, ImmutableArray<ArchitectureDocumentationItem>.Builder documentationItems)
	{
		var layerPath = parentLayerPath;
		if (element.Name.LocalName == "Layer" && element.Attribute("name")?.Value is { } layerName)
		{
			layerPath = string.IsNullOrEmpty(parentLayerPath) ? layerName : parentLayerPath + "/" + layerName;
		}

		documentationItems.Add(CreateDocumentationItem(element, configPath, depth, layerPath));

		foreach (var child in element.Elements())
		{
			AddDocumentationItems(child, configPath, depth + 1, layerPath, documentationItems);
		}
	}

	private static ArchitectureDocumentationItem CreateDocumentationItem(XElement element, string configPath, int depth, string layerPath)
	{
		var attributes = element.Attributes()
			.Where(attribute => !attribute.IsNamespaceDeclaration
			                    && !string.Equals(attribute.Name.LocalName, "description", StringComparison.Ordinal)
			                    && !string.Equals(attribute.Name.LocalName, "comment", StringComparison.Ordinal))
			.Select(attribute => new ArchitectureDocumentationAttribute(attribute.Name.LocalName, attribute.Value))
			.ToImmutableArray();

		var line = (IXmlLineInfo)element;
		var result = new ArchitectureDocumentationItem(
			element.Name.LocalName,
			GetDocumentationLabel(element),
			element.Attribute("description")?.Value,
			element.Attribute("comment")?.Value,
			attributes,
			depth,
			layerPath,
			configPath,
			line.HasLineInfo() ? line.LineNumber : 0);

		return result;
	}

	private static string GetDocumentationLabel(XElement element)
	{
		var result = element.Name.LocalName switch
		{
			"Include" => element.Attribute("path")?.Value ?? "Include",
			"Layer" => element.Attribute("name")?.Value ?? "Layer",
			"Class" => "Class " + (GetMatcherDisplayName(element) ?? "(no matcher)"),
			"Namespace" => "Namespace " + (GetMatcherDisplayName(element) ?? "(no matcher)"),
			"Assembly" => "Assembly " + (GetMatcherDisplayName(element) ?? "(no matcher)"),
			"AllowedDependency" => $"{element.Attribute("from")?.Value ?? "?"} -> {element.Attribute("to")?.Value ?? "?"}",
			"BlockedDependency" => $"{element.Attribute("from")?.Value ?? "?"} -x-> {element.Attribute("to")?.Value ?? "?"}",
			"ProjectArchitecture" => "Project topology",
			"ProjectGroup" => element.Attribute("name")?.Value ?? "ProjectGroup",
			"Project" => "Project " + (GetMatcherDisplayName(element) ?? "(no matcher)"),
			"AllowedProjectReference" => $"{element.Attribute("from")?.Value ?? "?"} -> {element.Attribute("to")?.Value ?? "?"}",
			"BlockedProjectReference" => $"{element.Attribute("from")?.Value ?? "?"} -x-> {element.Attribute("to")?.Value ?? "?"}",
			"PackagePolicy" => $"Package policy for {element.Attribute("projectGroup")?.Value ?? "?"}",
			"Package" => "Package " + (GetMatcherDisplayName(element) ?? "(no matcher)"),
			"RequireMatchingNames" => "Require matching names",
			"RequireDeclarationNameMatchesType" => "Require declaration name matches type",
			"VisibilityPolicy" => $"Visibility {element.Attribute("targets")?.Value ?? "?"}",
			"ApiSurface" => "API surface",
			"TransitiveExposure" => $"Traverse public object graph to depth {element.Attribute("maxDepth")?.Value ?? "3"}",
			"AllowedLayer" => $"Allow exposure of {element.Attribute("path")?.Value ?? "?"}",
			"BlockedLayer" => $"Block exposure of {element.Attribute("path")?.Value ?? "?"}",
			"SourceLocations" => $"Source ownership ({element.Attribute("relativeTo")?.Value ?? "Project"})",
			"EntryPoints" => "Boundary entry points",
			"EntryPoint" => element.Attribute("layer")?.Value is { Length: > 0 } layer
				? $"Entry via {layer}"
				: "Entry via matcher",
			"Name" => "Name " + (GetMatcherDisplayName(element) ?? "(all names)"),
			"Type" => "Type " + (GetMatcherDisplayName(element) ?? "(all types)"),
			"Source" => "Source " + (GetMatcherDisplayName(element) ?? "(no matcher)"),
			"Target" => "Target " + (GetMatcherDisplayName(element) ?? "(no matcher)"),
			"Allow" => $"Allow {element.Attribute("from")?.Value ?? GetChildMatcherDisplayName(element, "Source") ?? GetChildMatcherDisplayName(element, "Type") ?? "?"} -> {element.Attribute("to")?.Value ?? GetChildMatcherDisplayName(element, "Target") ?? GetChildMatcherDisplayName(element, "Name") ?? "?"}",
			"Fix" => "Fix " + (element.Attribute("Rename")?.Value ?? string.Empty),
			_ => element.Name.LocalName
		};

		return result;
	}

	public static string? GetMatcherDisplayName(XElement element)
	{
		var result = ArchitectureConfigurationMatcherReader.GetMatcherDisplayName(element);

		return result;
	}

	private static string? GetChildMatcherDisplayName(XElement element, string childName)
	{
		var child = element.Elements(childName).FirstOrDefault();
		var result = child is null ? null : GetMatcherDisplayName(child);

		return result;
	}

}
