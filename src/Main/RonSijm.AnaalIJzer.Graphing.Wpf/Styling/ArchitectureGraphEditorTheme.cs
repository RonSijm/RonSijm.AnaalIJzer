using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

public sealed class ArchitectureGraphEditorTheme(
    Brush background,
    Brush foreground,
    Brush hintForeground,
    Brush border,
    Brush highlightBorder,
    Brush successForeground,
    Brush errorForeground,
    ArchitectureGraphCanvasTheme canvasTheme)
{
	public static ArchitectureGraphEditorTheme Default
	{
		get
		{
			var result = new ArchitectureGraphEditorTheme(
				SystemColors.WindowBrush,
				SystemColors.ControlTextBrush,
				SystemColors.GrayTextBrush,
				SystemColors.ActiveBorderBrush,
				Brushes.DarkOrange,
				Brushes.ForestGreen,
				Brushes.IndianRed,
				ArchitectureGraphCanvasTheme.Default);

			return result;
		}
	}

    public Brush Background { get; } = background;

    public Brush Foreground { get; } = foreground;

    public Brush HintForeground { get; } = hintForeground;

    public Brush Border { get; } = border;

    public Brush HighlightBorder { get; } = highlightBorder;

    public Brush SuccessForeground { get; } = successForeground;

    public Brush ErrorForeground { get; } = errorForeground;

    public ArchitectureGraphCanvasTheme CanvasTheme { get; } = canvasTheme;

    public void ApplyToRoot(FrameworkElement root)
	{
		var hoverBackground = CreateOpacityBrush(Foreground, 0.12);
		var pressedBackground = CreateOpacityBrush(Foreground, 0.18);
		ArchitectureGraphThemeStyles.ApplyPopupResources(root, Background, Foreground, Border, hoverBackground, pressedBackground, HintForeground, Brushes.DodgerBlue);
	}

	public void ApplyBackground(Panel panel)
	{
		panel.Background = Background;
	}

	public void ApplyBackground(Border border)
	{
		border.Background = Background;
		border.BorderBrush = Border;
	}

	public void ApplyBorder(Border border, bool highlighted)
	{
		border.BorderBrush = highlighted ? HighlightBorder : Border;
	}

	public void ApplyHintForeground(TextBlock textBlock)
	{
		textBlock.Foreground = HintForeground;
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
