using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Graphing.Building;

internal static partial class ArchitectureGraphViewModelBuilder
{
	private static LayoutResult PackItems(ImmutableArray<LayoutItem> items, double startY)
	{
		var rowsByLane = new Dictionary<int, LayoutRow>();
		var placedSpans = new List<(double Left, double Right, int MinimumLevel)>();
		foreach (var item in items.OrderBy(item => item.MinimumLevel).ThenBy(item => item.PreferredLane).ThenBy(item => item.MinimumOrder))
		{
			if (!rowsByLane.TryGetValue(item.PreferredLane, out var row))
			{
				row = new LayoutRow();
				rowsByLane.Add(item.PreferredLane, row);
			}

			var shifted = ShiftPastEarlierLevels(item, placedSpans);
			var placed = row.Add(shifted);
			placedSpans.Add((placed.Left, placed.Right, placed.MinimumLevel));
		}

		var placedItems = rowsByLane.Values.SelectMany(row => row.Items).ToImmutableArray();
		var nodes = ImmutableArray.CreateBuilder<NodePlacement>();
		var boundaries = ImmutableArray.CreateBuilder<BoundaryPlacement>();
		var y = startY;
		foreach (var row in rowsByLane.OrderBy(item => item.Key).Select(item => item.Value))
		{
			foreach (var item in row.Items)
			{
				var shifted = item.Result.ShiftY(y);
				nodes.AddRange(shifted.Nodes);
				boundaries.AddRange(shifted.Boundaries);
			}

			y += row.Height + BlockRowGap;
		}

		var result = new LayoutResult(nodes.ToImmutable(), boundaries.ToImmutable(), placedItems.Min(item => item.Left), startY, placedItems.Max(item => item.Right), Math.Max(startY, y - BlockRowGap), placedItems.Min(item => item.MinimumLevel), placedItems.Min(item => item.MinimumOrder));

		return result;
	}

	private static LayoutItem ShiftPastEarlierLevels(LayoutItem item, List<(double Left, double Right, int MinimumLevel)> placedSpans)
	{
		var shift = 0d;
		while (true)
		{
			var shifted = item.ShiftX(shift);
			var overlaps = placedSpans
				.Where(span => span.MinimumLevel < item.MinimumLevel && SpansOverlap(shifted.Left, shifted.Right, span.Left, span.Right))
				.ToImmutableArray();
			if (overlaps.Length == 0)
			{
				return shifted;
			}

			shift = overlaps.Max(span => span.Right + BlockHorizontalGap - item.Left);
		}
	}

	private static bool SpansOverlap(double left, double right, double otherLeft, double otherRight)
	{
		var result = left < otherRight + BlockHorizontalGap && right + BlockHorizontalGap > otherLeft;

		return result;
	}
}
