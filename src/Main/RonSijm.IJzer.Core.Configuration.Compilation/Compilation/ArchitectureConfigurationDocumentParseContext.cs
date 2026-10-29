using System.Collections.Immutable;
using RonSijm.IJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.IJzer.Core.Configuration.Document.Model;

namespace RonSijm.IJzer.Core.Configuration.Compilation.Compilation;

internal readonly struct ArchitectureConfigurationDocumentParseContext
{
    internal ArchitectureConfigurationDocumentParseContext(
        ImmutableArray<ArchitectureConfigurationDocumentInput> documents,
        ImmutableArray<ArchitectureConfigurationElementInput> elements,
        ImmutableArray<ArchitectureDocumentationItem> documentationItems)
    {
        Documents = documents;
        Elements = elements;
        DocumentationItems = documentationItems;
    }

    internal ImmutableArray<ArchitectureConfigurationDocumentInput> Documents { get; }

    internal ImmutableArray<ArchitectureConfigurationElementInput> Elements { get; }

    internal ImmutableArray<ArchitectureDocumentationItem> DocumentationItems { get; }
}