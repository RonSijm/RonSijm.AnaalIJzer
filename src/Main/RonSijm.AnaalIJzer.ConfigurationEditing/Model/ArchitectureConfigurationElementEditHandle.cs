using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Model;

public sealed class ArchitectureConfigurationElementEditHandle(
    ArchitectureConfigurationSourceKind sourceKind,
    string sourcePath,
    int xmlLineNumber,
    string layerPath,
    string containerKind,
    string elementKind,
    ImmutableDictionary<string, string>? attributes = null)
{
    public ArchitectureConfigurationSourceKind SourceKind { get; } = sourceKind;

    public string SourcePath { get; } = sourcePath;

    public int XmlLineNumber { get; } = xmlLineNumber;

    public string LayerPath { get; } = layerPath;

    public string ContainerKind { get; } = containerKind;

    public string ElementKind { get; } = elementKind;

    public ImmutableDictionary<string, string> Attributes { get; } = attributes ?? ImmutableDictionary<string, string>.Empty;

    public bool CanEdit
	{
		get
		{
			var result = SourceKind != ArchitectureConfigurationSourceKind.None
			             && !string.IsNullOrWhiteSpace(SourcePath)
			             && !string.IsNullOrWhiteSpace(ElementKind);

			return result;
		}
	}
}
