using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.Graphing.Model;

namespace RonSijm.AnaalIJzer.Graphing.ViewModels;

public sealed class ArchitectureGraphNodeViewModel(
    string path,
    string displayName,
    string? description,
    int depth,
    int paletteSlot,
    bool isActive,
    double x,
    double y,
    ArchitectureLayerEditHandle? editHandle = null,
    ImmutableArray<ArchitectureGraphTypeEvidence> typeEvidence = default,
    int incomingDependencyCount = 0,
    int outgoingDependencyCount = 0,
    int incomingViolationCount = 0,
    int outgoingViolationCount = 0)
{
    public string Path { get; } = path;

    public string DisplayName { get; } = displayName;

    public string? Description { get; } = description;

    public int Depth { get; } = depth;

    public int PaletteSlot { get; } = paletteSlot;

    public bool IsActive { get; } = isActive;

    public double X { get; } = x;

    public double Y { get; } = y;

    public ArchitectureLayerEditHandle EditHandle { get; } = editHandle ?? ArchitectureLayerEditHandle.None;

    public ImmutableArray<ArchitectureGraphTypeEvidence> TypeEvidence { get; } = typeEvidence.IsDefault ? ImmutableArray<ArchitectureGraphTypeEvidence>.Empty : typeEvidence;

    public int TypeCount
    {
        get
        {
            var result = TypeEvidence.Length;

            return result;
        }
    }

    public int IncomingDependencyCount { get; } = incomingDependencyCount;

    public int OutgoingDependencyCount { get; } = outgoingDependencyCount;

    public int IncomingViolationCount { get; } = incomingViolationCount;

    public int OutgoingViolationCount { get; } = outgoingViolationCount;
}
