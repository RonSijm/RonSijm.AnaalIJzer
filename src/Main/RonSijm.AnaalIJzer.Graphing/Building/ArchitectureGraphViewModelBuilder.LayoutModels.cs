using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Graphing.ViewModels;

namespace RonSijm.AnaalIJzer.Graphing.Building;

public static partial class ArchitectureGraphViewModelBuilder
{
	private sealed class LayerTreeNode(ArchitectureGraphLayer layer, ImmutableArray<LayerTreeNode> children)
	{
		public ArchitectureGraphLayer Layer { get; } = layer;

		public ImmutableArray<LayerTreeNode> Children { get; } = children;
	}

	private sealed class LayoutRow
	{
		private readonly List<(double Left, double Right, int MinimumLevel)> spans = [];

		public List<LayoutItem> Items { get; } = [];

		public double Height { get; private set; }

		public bool TryPlace(LayoutItem item, out LayoutItem placed)
		{
			var shift = 0d;
			while (true)
			{
				placed = item.ShiftX(shift);
				var placedLeft = placed.Left;
				var placedRight = placed.Right;
				var overlaps = spans.Where(span => SpansOverlap(placedLeft, placedRight, span.Left, span.Right)).ToImmutableArray();
				if (overlaps.Length == 0)
				{
					return true;
				}

				if (overlaps.Any(span => span.MinimumLevel > item.MinimumLevel))
				{
					return false;
				}

				shift = overlaps.Max(span => span.Right + BlockHorizontalGap - item.Left);
			}
		}

		public LayoutItem Add(LayoutItem item)
		{
			if (!TryPlace(item, out var placed))
			{
				placed = item;
			}

			Items.Add(placed);
			spans.Add((placed.Left, placed.Right, placed.MinimumLevel));
			Height = Math.Max(Height, placed.Height);

			return placed;
		}
	}

	private readonly struct LayoutItem(LayoutResult result, int minimumLevel, int minimumOrder, int preferredLane)
	{
		public LayoutResult Result { get; } = result;

		public int MinimumLevel { get; } = minimumLevel;

		public int MinimumOrder { get; } = minimumOrder;

		public int PreferredLane { get; } = preferredLane;

		public double Left
		{
			get { return Result.Left; }
		}

		public double Right
		{
			get { return Result.Right; }
		}

		public double Height
		{
			get { return Result.Height; }
		}

		public static LayoutItem FromResult(LayoutResult result, int minimumLevel, int minimumOrder, int preferredLane)
		{
			var item = new LayoutItem(result, Math.Min(minimumLevel, result.MinimumLevel), Math.Min(minimumOrder, result.MinimumOrder), preferredLane);

			return item;
		}

		public LayoutItem ShiftX(double delta)
		{
			if (delta == 0)
			{
				return this;
			}

			var result = new LayoutItem(Result.ShiftX(delta), MinimumLevel, MinimumOrder, PreferredLane);

			return result;
		}
	}

	private readonly struct LayoutResult(
		ImmutableArray<NodePlacement> nodes,
		ImmutableArray<BoundaryPlacement> boundaries,
		double left,
		double top,
		double right,
		double bottom,
		int minimumLevel,
		int minimumOrder)
	{
		public ImmutableArray<NodePlacement> Nodes { get; } = nodes;

		public ImmutableArray<BoundaryPlacement> Boundaries { get; } = boundaries;

		public double Left { get; } = left;

		public double Top { get; } = top;

		public double Right { get; } = right;

		public double Bottom { get; } = bottom;

		public int MinimumLevel { get; } = minimumLevel;

		public int MinimumOrder { get; } = minimumOrder;

		public double Height
		{
			get { return Bottom - Top; }
		}

		public static LayoutResult FromNode(ArchitectureGraphLayer layer, double x)
		{
			var node = new NodePlacement(layer, x, 0);
			var result = new LayoutResult(ImmutableArray.Create(node), ImmutableArray<BoundaryPlacement>.Empty, x, 0, x + NodeVisualWidth, NodeVisualHeight, int.MaxValue, int.MaxValue);

			return result;
		}

		public LayoutResult ShiftY(double delta)
		{
			var shiftedNodes = Nodes.Select(node => node.ShiftY(delta)).ToImmutableArray();
			var shiftedBoundaries = Boundaries.Select(boundary => boundary.ShiftY(delta)).ToImmutableArray();
			var result = new LayoutResult(shiftedNodes, shiftedBoundaries, Left, Top + delta, Right, Bottom + delta, MinimumLevel, MinimumOrder);

			return result;
		}

		public LayoutResult ShiftX(double delta)
		{
			var shiftedNodes = Nodes.Select(node => node.ShiftX(delta)).ToImmutableArray();
			var shiftedBoundaries = Boundaries.Select(boundary => boundary.ShiftX(delta)).ToImmutableArray();
			var result = new LayoutResult(shiftedNodes, shiftedBoundaries, Left + delta, Top, Right + delta, Bottom, MinimumLevel, MinimumOrder);

			return result;
		}

		public LayoutResult WithBoundary(BoundaryPlacement boundary, double nextLeft, double nextTop, double nextRight, double nextBottom, int boundaryLevel, int boundaryOrder)
		{
			var result = new LayoutResult(
				Nodes,
				Boundaries.Add(boundary),
				nextLeft,
				nextTop,
				nextRight,
				nextBottom,
				Math.Min(MinimumLevel, boundaryLevel),
				Math.Min(MinimumOrder, boundaryOrder));

			return result;
		}
	}

	private readonly struct NodePlacement(ArchitectureGraphLayer layer, double x, double y)
	{
		public ArchitectureGraphLayer Layer { get; } = layer;

		public double X { get; } = x;

		public double Y { get; } = y;

		public NodePlacement ShiftY(double delta)
		{
			var result = new NodePlacement(Layer, X, Y + delta);

			return result;
		}

		public NodePlacement ShiftX(double delta)
		{
			var result = new NodePlacement(Layer, X + delta, Y);

			return result;
		}
	}

	private readonly struct BoundaryPlacement(ArchitectureGraphLayer layer, double x, double y, double width, double height, bool isActive)
	{
		public ArchitectureGraphLayer Layer { get; } = layer;

		public double X { get; } = x;

		public double Y { get; } = y;

		public double Width { get; } = width;

		public double Height { get; } = height;

		public bool IsActive { get; } = isActive;

		public BoundaryPlacement ShiftY(double delta)
		{
			var result = new BoundaryPlacement(Layer, X, Y + delta, Width, Height, IsActive);

			return result;
		}

		public BoundaryPlacement ShiftX(double delta)
		{
			var result = new BoundaryPlacement(Layer, X + delta, Y, Width, Height, IsActive);

			return result;
		}
	}

	private readonly struct GraphDiagram(ImmutableArray<ArchitectureGraphNodeViewModel> nodes, ImmutableArray<ArchitectureGraphEdgeViewModel> edges, ImmutableArray<ArchitectureGraphBoundaryViewModel> boundaries)
	{
		public ImmutableArray<ArchitectureGraphNodeViewModel> Nodes { get; } = nodes;

		public ImmutableArray<ArchitectureGraphEdgeViewModel> Edges { get; } = edges;

		public ImmutableArray<ArchitectureGraphBoundaryViewModel> Boundaries { get; } = boundaries;
	}
}
