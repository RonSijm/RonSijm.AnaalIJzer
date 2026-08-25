using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Graphing.ViewModels;

namespace RonSijm.AnaalIJzer.Graphing.Building;

internal static partial class ArchitectureGraphViewModelBuilder
{
	private readonly struct GraphDiagram(ImmutableArray<ArchitectureGraphNodeViewModel> nodes, ImmutableArray<ArchitectureGraphEdgeViewModel> edges, ImmutableArray<ArchitectureGraphBoundaryViewModel> boundaries)
	{
		public ImmutableArray<ArchitectureGraphNodeViewModel> Nodes { get; } = nodes;

		public ImmutableArray<ArchitectureGraphEdgeViewModel> Edges { get; } = edges;

		public ImmutableArray<ArchitectureGraphBoundaryViewModel> Boundaries { get; } = boundaries;
	}
}
