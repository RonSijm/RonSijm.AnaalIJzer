using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Model;

internal readonly struct ArchitectureDocumentation(
    string? description,
    ImmutableArray<ArchitectureDocumentationItem> items)
{
	public static ArchitectureDocumentation Empty { get; } = new(null, ImmutableArray<ArchitectureDocumentationItem>.Empty);

    public string? Description { get; } = description;
    public ImmutableArray<ArchitectureDocumentationItem> Items { get; } = items;
}

internal readonly struct ArchitectureDocumentationItem(
    string kind,
    string label,
    string? description,
    string? comment,
    ImmutableArray<ArchitectureDocumentationAttribute> attributes,
    int depth,
    string layerPath,
    string sourcePath,
    int xmlLineNumber)
{
    public string Kind { get; } = kind;
    public string Label { get; } = label;
    public string? Description { get; } = description;
    public string? Comment { get; } = comment;
    public ImmutableArray<ArchitectureDocumentationAttribute> Attributes { get; } = attributes;
    public int Depth { get; } = depth;
    public string LayerPath { get; } = layerPath;
    public string SourcePath { get; } = sourcePath;
    public int XmlLineNumber { get; } = xmlLineNumber;

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

internal readonly struct ArchitectureDocumentationAttribute(string name, string value)
{
    public string Name { get; } = name;
    public string Value { get; } = value;
}
