using RonSijm.IJzer.ConfigurationEditing.Model;
using RonSijm.IJzer.Core.Configuration.Document.Model;
using RonSijm.IJzer.GraphModel.Model;

namespace RonSijm.IJzer.GraphModel.Building;

public sealed class ArchitectureGraphLayerInput(
    string path,
    string displayName,
    string? description,
    int depth,
    int paletteSlot,
    bool isActive,
    string sourcePath = "",
    ArchitectureConfigurationSourceKind sourceKind = ArchitectureConfigurationSourceKind.None,
    int xmlLineNumber = 0,
    ArchitectureGraphNodeKind kind = ArchitectureGraphNodeKind.Layer,
    string? readOnlyDetails = null)
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

    public ArchitectureGraphNodeKind Kind { get; } = kind;

    public string? ReadOnlyDetails { get; } = readOnlyDetails;
}