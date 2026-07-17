using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

public sealed class ArchitectureGraphCanvasTheme(
    Brush surfaceBackground,
    Brush gridLine,
    Brush foreground,
    Brush nodeForeground,
    Brush border,
    Brush connection,
    Brush activeConnection,
    Brush errorConnection,
    Brush connectionText)
{
	public static ArchitectureGraphCanvasTheme Default
	{
		get
		{
			var result = new ArchitectureGraphCanvasTheme(
				CreateFrozenBrush(SystemColors.WindowColor),
				CreateFrozenBrush(Color.FromArgb(42, 96, 108, 128)),
				CreateFrozenBrush(SystemColors.ControlTextColor),
				Brushes.White,
				CreateFrozenBrush(SystemColors.ActiveBorderColor),
				CreateFrozenBrush(Color.FromRgb(86, 96, 112)),
				Brushes.DarkOrange,
				Brushes.IndianRed,
				Brushes.White);

			return result;
		}
	}

    public Brush SurfaceBackground { get; } = surfaceBackground;

    public Brush GridLine { get; } = gridLine;

    public Brush Foreground { get; } = foreground;

    public Brush NodeForeground { get; } = nodeForeground;

    public Brush Border { get; } = border;

    public Brush Connection { get; } = connection;

    public Brush ActiveConnection { get; } = activeConnection;

    public Brush ErrorConnection { get; } = errorConnection;

    public Brush ConnectionText { get; } = connectionText;

    public void ApplyToRoot(FrameworkElement root)
	{
		var hoverBackground = CreateOpacityBrush(Foreground, 0.12);
		var pressedBackground = CreateOpacityBrush(Foreground, 0.18);
		ArchitectureGraphThemeStyles.ApplyPopupResources(root, SurfaceBackground, Foreground, Border, hoverBackground, pressedBackground, Border, Brushes.DodgerBlue);
	}

	public void ApplyToContextMenu(ContextMenu menu)
	{
		ApplyToRoot(menu);
		menu.Background = SurfaceBackground;
		menu.Foreground = Foreground;
		menu.BorderBrush = Border;
		menu.ItemContainerStyle = CreateMenuItemStyle();
	}

	public Style CreateMenuItemStyle()
	{
		var result = ArchitectureGraphThemeStyles.CreateMenuItemStyle(SurfaceBackground, Foreground, Border, CreateOpacityBrush(Foreground, 0.12), Border);

		return result;
	}

	private static Brush CreateFrozenBrush(Color color)
	{
		var result = new SolidColorBrush(color);
		result.Freeze();

		return result;
	}

	private static Brush CreateOpacityBrush(Brush source, double opacity)
	{
		if (source is not SolidColorBrush solid)
		{
			return source;
		}

		var result = new SolidColorBrush(Color.FromArgb((byte)Math.Round(byte.MaxValue * opacity), solid.Color.R, solid.Color.G, solid.Color.B));
		result.Freeze();

		return result;
	}
}
