using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Graphing.Model;

namespace RonSijm.AnaalIJzer.Graphing.Building;

internal static partial class ArchitectureGraphViewModelBuilder
{
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
}
