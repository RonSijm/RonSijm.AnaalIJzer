using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.Graphing.ViewModels;

public sealed class ArchitectureGraphGroupViewModel(
    string title,
    bool isActive,
    bool isHighlighted,
    ImmutableArray<string> layers,
    ImmutableArray<string> rules,
    ImmutableArray<ArchitectureGraphNodeViewModel> nodes = default,
    ImmutableArray<ArchitectureGraphEdgeViewModel> edges = default,
    ArchitectureConfigurationSource? configurationSource = null,
    ImmutableArray<ArchitectureGraphBoundaryViewModel> boundaries = default)
{
    public string Title { get; } = title;

    public bool IsActive { get; } = isActive;

    public bool IsHighlighted { get; } = isHighlighted;

    public ImmutableArray<string> Layers { get; } = layers;

    public ImmutableArray<string> Rules { get; } = rules;

    public ImmutableArray<ArchitectureGraphNodeViewModel> Nodes { get; } = nodes.IsDefault ? ImmutableArray<ArchitectureGraphNodeViewModel>.Empty : nodes;

    public ImmutableArray<ArchitectureGraphEdgeViewModel> Edges { get; } = edges.IsDefault ? ImmutableArray<ArchitectureGraphEdgeViewModel>.Empty : edges;

    public ArchitectureConfigurationSource ConfigurationSource { get; } = configurationSource ?? ArchitectureConfigurationSource.None;

    public ImmutableArray<ArchitectureGraphBoundaryViewModel> Boundaries { get; } = boundaries.IsDefault ? ImmutableArray<ArchitectureGraphBoundaryViewModel>.Empty : boundaries;
}
