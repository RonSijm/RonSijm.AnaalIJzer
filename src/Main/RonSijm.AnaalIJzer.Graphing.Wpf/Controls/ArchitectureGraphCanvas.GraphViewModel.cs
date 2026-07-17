using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
using RonSijm.AnaalIJzer.Graphing.Wpf.Layout;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Controls;

public sealed partial class ArchitectureGraphCanvas
{
	private sealed class NodifyGraphViewModel
	{
		private NodifyGraphViewModel(ImmutableArray<object> items, ImmutableArray<NodifyGraphNodeViewModel> nodes, ImmutableArray<NodifyGraphConnectionViewModel> connections, ImmutableArray<NodifyGraphBoundaryViewModel> boundaries)
		{
			Items = items;
			Nodes = nodes;
			Connections = connections;
			Boundaries = boundaries;
		}

		public ImmutableArray<object> Items { get; }

		public ImmutableArray<NodifyGraphNodeViewModel> Nodes { get; }

		public ImmutableArray<NodifyGraphConnectionViewModel> Connections { get; }

		public ImmutableArray<NodifyGraphBoundaryViewModel> Boundaries { get; }

		public static NodifyGraphViewModel Create(
			ArchitectureGraphGroupViewModel group,
			Action<ArchitectureConfigurationEditResult, bool>? editResultHandler,
			Func<string, bool>? confirmationHandler,
			Func<ArchitectureLayerCreationRequest?>? layerCreationHandler,
			ArchitectureGraphLayoutState layoutState,
			ArchitectureGraphCanvasTheme theme)
		{
			var boundaries = group.Boundaries.Select(boundary => NodifyGraphBoundaryViewModel.Create(boundary, editResultHandler, confirmationHandler, layerCreationHandler, layoutState, theme)).ToImmutableArray();
			var boundariesByPath = boundaries.ToDictionary(boundary => boundary.Path, StringComparer.Ordinal);
			var nodes = group.Nodes.Select(node => NodifyGraphNodeViewModel.Create(node, editResultHandler, confirmationHandler, layerCreationHandler, layoutState, theme)).ToImmutableArray();
			WireHierarchy(nodes, boundaries, boundariesByPath);
			var endpointsByPath = CreateEndpointsByPath(nodes, boundaries);
			var connections = group.Edges
				.Where(edge => endpointsByPath.ContainsKey(edge.From) && endpointsByPath.ContainsKey(edge.To))
				.Select(edge => NodifyGraphConnectionViewModel.Create(edge, endpointsByPath[edge.From].Output, endpointsByPath[edge.To].Input, editResultHandler, confirmationHandler, theme))
				.ToImmutableArray();
			var items = boundaries.Cast<object>().Concat(nodes).ToImmutableArray();

			return new NodifyGraphViewModel(items, nodes, connections, boundaries);
		}

		private static Dictionary<string, NodifyGraphEndpoint> CreateEndpointsByPath(ImmutableArray<NodifyGraphNodeViewModel> nodes, ImmutableArray<NodifyGraphBoundaryViewModel> boundaries)
		{
			var result = new Dictionary<string, NodifyGraphEndpoint>(StringComparer.Ordinal);
			foreach (var boundary in boundaries)
			{
				result[boundary.Path] = new NodifyGraphEndpoint(boundary.Input, boundary.Output);
			}

			foreach (var node in nodes)
			{
				result[node.Path] = new NodifyGraphEndpoint(node.Input, node.Output);
			}

			return result;
		}

		private static void WireHierarchy(ImmutableArray<NodifyGraphNodeViewModel> nodes, ImmutableArray<NodifyGraphBoundaryViewModel> boundaries, Dictionary<string, NodifyGraphBoundaryViewModel> boundariesByPath)
		{
			foreach (var boundary in boundaries)
			{
				var parentBoundary = FindParentBoundary(boundary.Path, boundariesByPath);
				var directNodes = nodes.Where(node => IsDirectNodeInsideBoundary(node.Path, boundary.Path, boundariesByPath)).ToImmutableArray();
				var directBoundaries = boundaries.Where(candidate => FindParentBoundary(candidate.Path, boundariesByPath) == boundary).ToImmutableArray();
				boundary.Attach(parentBoundary, directNodes, directBoundaries);
			}

			foreach (var node in nodes)
			{
				node.Attach(FindContainingBoundary(node.Path, boundariesByPath));
			}
		}

		private static NodifyGraphBoundaryViewModel? FindContainingBoundary(string path, Dictionary<string, NodifyGraphBoundaryViewModel> boundariesByPath)
		{
			if (boundariesByPath.TryGetValue(path, out var selfBoundary))
			{
				return selfBoundary;
			}

			var result = FindParentBoundary(path, boundariesByPath);

			return result;
		}

		private static NodifyGraphBoundaryViewModel? FindParentBoundary(string path, Dictionary<string, NodifyGraphBoundaryViewModel> boundariesByPath)
		{
			var parentPath = GetParentPath(path);
			while (parentPath.Length > 0)
			{
				if (boundariesByPath.TryGetValue(parentPath, out var boundary))
				{
					return boundary;
				}

				parentPath = GetParentPath(parentPath);
			}

			return null;
		}

		private static bool IsDirectNodeInsideBoundary(string nodePath, string boundaryPath, Dictionary<string, NodifyGraphBoundaryViewModel> boundariesByPath)
		{
			if (nodePath == boundaryPath)
			{
				return false;
			}

			if (boundariesByPath.ContainsKey(nodePath))
			{
				return false;
			}

			var containingBoundary = FindContainingBoundary(nodePath, boundariesByPath);
			var result = containingBoundary?.Path == boundaryPath;

			return result;
		}
	}

	private readonly struct NodifyGraphEndpoint(NodifyGraphConnectorViewModel input, NodifyGraphConnectorViewModel output)
	{
		public NodifyGraphConnectorViewModel Input { get; } = input;

		public NodifyGraphConnectorViewModel Output { get; } = output;
	}
}
