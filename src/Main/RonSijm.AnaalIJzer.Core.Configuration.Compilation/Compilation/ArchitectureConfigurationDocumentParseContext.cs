using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Config.Parsing;
using RonSijm.AnaalIJzer.Model;

namespace RonSijm.AnaalIJzer.Config.Compilation;

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
