using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

internal readonly struct ArchitectureConfigurationDocumentInput
{
    internal ArchitectureConfigurationDocumentInput(XElement root, string path, bool isInlineConfiguration)
    {
        Root = root;
        Path = path;
        IsInlineConfiguration = isInlineConfiguration;
    }

    internal XElement Root { get; }

    internal string Path { get; }

    internal bool IsInlineConfiguration { get; }
}

internal readonly struct ArchitectureConfigurationElementInput
{
    internal ArchitectureConfigurationElementInput(XElement element, string path, bool isInlineConfiguration)
    {
        Element = element;
        Path = path;
        IsInlineConfiguration = isInlineConfiguration;
    }

    internal XElement Element { get; }

    internal string Path { get; }

    internal bool IsInlineConfiguration { get; }
}
