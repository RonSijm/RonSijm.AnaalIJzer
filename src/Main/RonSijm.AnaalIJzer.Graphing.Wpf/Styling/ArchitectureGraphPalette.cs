using System.Windows.Media;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

internal static class ArchitectureGraphPalette
{
	private static readonly Color[] Colors =
	[
		Color.FromRgb(38, 111, 201),
		Color.FromRgb(26, 136, 94),
		Color.FromRgb(189, 116, 28),
		Color.FromRgb(154, 85, 188),
		Color.FromRgb(192, 64, 92),
		Color.FromRgb(52, 138, 151),
		Color.FromRgb(122, 116, 33),
		Color.FromRgb(88, 117, 178),
		Color.FromRgb(64, 148, 79),
		Color.FromRgb(176, 77, 42),
		Color.FromRgb(135, 94, 58),
		Color.FromRgb(84, 128, 130),
		Color.FromRgb(177, 83, 139),
		Color.FromRgb(91, 121, 57),
		Color.FromRgb(55, 132, 192),
		Color.FromRgb(128, 100, 194),
	];

	public static SolidColorBrush GetBrush(int slot)
	{
		var color = Colors[Math.Max(0, Math.Min(Colors.Length - 1, slot - 1))];
		var brush = new SolidColorBrush(color);
		brush.Freeze();

		return brush;
	}

	public static SolidColorBrush GetUnclassifiedBrush()
	{
		var brush = new SolidColorBrush(Color.FromRgb(106, 115, 125));
		brush.Freeze();

		return brush;
	}
}
