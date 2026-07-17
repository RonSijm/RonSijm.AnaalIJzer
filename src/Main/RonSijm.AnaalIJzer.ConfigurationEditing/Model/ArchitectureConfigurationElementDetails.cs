using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Model;

public sealed class ArchitectureConfigurationElementDetails(
    ArchitectureConfigurationElementEditHandle handle,
    string elementKind,
    string containerKind,
    ImmutableDictionary<string, string> attributes,
    string summary,
    string childXml)
{
    public ArchitectureConfigurationElementEditHandle Handle { get; } = handle;

    public string ElementKind { get; } = elementKind;

    public string ContainerKind { get; } = containerKind;

    public ImmutableDictionary<string, string> Attributes { get; } = attributes;

    public string Summary { get; } = summary;

    public string ChildXml { get; } = childXml;
}
