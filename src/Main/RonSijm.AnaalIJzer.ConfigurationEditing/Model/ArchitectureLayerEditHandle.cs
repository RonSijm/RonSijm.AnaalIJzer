namespace RonSijm.AnaalIJzer.ConfigurationEditing.Model;

public sealed class ArchitectureLayerEditHandle(
    ArchitectureConfigurationSourceKind sourceKind,
    string sourcePath,
    int xmlLineNumber,
    string layerPath,
    string configuredName,
    string parentPath,
    string? description)
{
    public ArchitectureConfigurationSourceKind SourceKind { get; } = sourceKind;

    public string SourcePath { get; } = sourcePath;

    public int XmlLineNumber { get; } = xmlLineNumber;

    public string LayerPath { get; } = layerPath;

    public string ConfiguredName { get; } = configuredName;

    public string ParentPath { get; } = parentPath;

    public string? Description { get; } = description;

    public bool CanEdit
	{
		get
		{
			var result = SourceKind != ArchitectureConfigurationSourceKind.None
			             && !string.IsNullOrWhiteSpace(SourcePath)
			             && !string.IsNullOrWhiteSpace(LayerPath);

			return result;
		}
	}

	public static ArchitectureLayerEditHandle None { get; } = new(
		ArchitectureConfigurationSourceKind.None,
		string.Empty,
		0,
		string.Empty,
		string.Empty,
		string.Empty,
		null);
}
