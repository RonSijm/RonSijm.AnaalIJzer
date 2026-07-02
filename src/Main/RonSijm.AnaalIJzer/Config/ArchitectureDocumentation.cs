using System;
using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Config;

internal readonly struct ArchitectureDocumentation
{
	public static ArchitectureDocumentation Empty { get; } = new(null, ImmutableArray<ArchitectureDocumentationItem>.Empty);

	public ArchitectureDocumentation(string? description, ImmutableArray<ArchitectureDocumentationItem> items)
	{
		Description = description;
		Items = items;
	}

	public string? Description { get; }
	public ImmutableArray<ArchitectureDocumentationItem> Items { get; }
}

internal readonly struct ArchitectureDocumentationItem
{
	public ArchitectureDocumentationItem(string kind, string label, string? description, string? comment, ImmutableArray<ArchitectureDocumentationAttribute> attributes, int depth, string layerPath, string sourcePath, int xmlLineNumber)
	{
		Kind = kind;
		Label = label;
		Description = description;
		Comment = comment;
		Attributes = attributes;
		Depth = depth;
		LayerPath = layerPath;
		SourcePath = sourcePath;
		XmlLineNumber = xmlLineNumber;
	}

	public string Kind { get; }
	public string Label { get; }
	public string? Description { get; }
	public string? Comment { get; }
	public ImmutableArray<ArchitectureDocumentationAttribute> Attributes { get; }
	public int Depth { get; }
	public string LayerPath { get; }
	public string SourcePath { get; }
	public int XmlLineNumber { get; }

	public string? GetAttribute(string name)
	{
		foreach (var attribute in Attributes)
		{
			if (string.Equals(attribute.Name, name, StringComparison.Ordinal))
			{
				return attribute.Value;
			}
		}

		return null;
	}
}

internal readonly struct ArchitectureDocumentationAttribute
{
	public ArchitectureDocumentationAttribute(string name, string value)
	{
		Name = name;
		Value = value;
	}

	public string Name { get; }
	public string Value { get; }
}
