using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Graphing.Model;

namespace RonSijm.AnaalIJzer.Graphing.Building;

internal static partial class ArchitectureGraphViewModelBuilder
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
}
