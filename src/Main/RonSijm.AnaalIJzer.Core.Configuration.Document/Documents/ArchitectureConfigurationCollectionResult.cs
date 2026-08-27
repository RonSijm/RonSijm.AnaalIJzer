using System.Collections.Immutable;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;

public readonly struct ArchitectureConfigurationCollectedDocument(XElement root, string path, bool isInlineConfiguration)
{
	public XElement Root { get; } = root;
	public string Path { get; } = path;
	public bool IsInlineConfiguration { get; } = isInlineConfiguration;
}

public readonly struct ArchitectureConfigurationCollectedElement(XElement element, string path, bool isInlineConfiguration)
{
	public XElement Element { get; } = element;
	public string Path { get; } = path;
	public bool IsInlineConfiguration { get; } = isInlineConfiguration;
}

public sealed class ArchitectureConfigurationCollectionResult(
	ImmutableArray<ArchitectureConfigurationCollectedDocument> documents,
	ImmutableArray<ArchitectureConfigurationCollectedElement> elements,
	ImmutableArray<ArchitectureDocumentationItem> documentationItems,
	ImmutableArray<ConfigurationIssue> issues)
{
	public ImmutableArray<ArchitectureConfigurationCollectedDocument> Documents { get; } = documents;

	public ImmutableArray<ArchitectureConfigurationCollectedElement> Elements { get; } = elements;

	public ImmutableArray<ArchitectureDocumentationItem> DocumentationItems { get; } = documentationItems;

	public ImmutableArray<ConfigurationIssue> Issues { get; } = issues;
}
