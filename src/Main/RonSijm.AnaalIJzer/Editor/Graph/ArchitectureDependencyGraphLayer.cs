using RonSijm.AnaalIJzer.Configuration;

namespace RonSijm.AnaalIJzer.Graph;

public sealed class ArchitectureDependencyGraphLayer(
    string path,
    string displayName,
    string? description,
    int depth,
    int paletteSlot,
    bool isActive,
    string sourcePath = "",
    ArchitectureConfigurationSourceKind sourceKind = ArchitectureConfigurationSourceKind.None,
    int xmlLineNumber = 0)
{
    public string Path { get; } = path;

    public string DisplayName { get; } = displayName;

    public string? Description { get; } = description;

    public int Depth { get; } = depth;

    public int PaletteSlot { get; } = paletteSlot;

    public bool IsActive { get; } = isActive;

    public string SourcePath { get; } = sourcePath;

    public ArchitectureConfigurationSourceKind SourceKind { get; } = sourceKind;

    public int XmlLineNumber { get; } = xmlLineNumber;
}
