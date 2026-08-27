using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.VisualStudio.Editor.Styling;
using RonSijm.AnaalIJzer.VisualStudio.Styling;

namespace RonSijm.AnaalIJzer.VisualStudio.Editor.LayerIndicators;

internal static class ArchitectureLayerCodeLensAdornment
{
	internal const double Height = 20;

	internal static UIElement Create(ArchitectureLayerIndicator indicator, ArchitectureEditorOptions options)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		var label = ArchitectureLayerCodeLensText.CreateSummary(indicator, options);
		var accentBrush = indicator.IsInLayer ? ArchitecturePalette.GetBrush(indicator.PaletteSlot) : ArchitecturePalette.GetUnclassifiedBrush();
		var accent = new Border
		{
			Width = 8,
			Height = 8,
			CornerRadius = new CornerRadius(4),
			Background = accentBrush,
			Margin = new Thickness(0, 0, 5, 0),
			VerticalAlignment = VerticalAlignment.Center
		};
		var text = new TextBlock
		{
			Text = label,
			FontSize = 10,
			VerticalAlignment = VerticalAlignment.Center
		};
		ArchitectureVisualStudioTheme.ApplyHintForeground(text);

		var content = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Children =
			{
				accent,
				text
			}
		};
		var result = new Border
		{
			Height = Height,
			Padding = new Thickness(0, 1, 8, 1),
			Background = Brushes.Transparent,
			ToolTip = "AnaalIJzer layer details",
			Child = content,
			Cursor = Cursors.Hand
		};
		result.MouseLeftButtonUp += (_, args) =>
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			args.Handled = true;
			ArchitectureLayerCodeLensPopout.Show(indicator, result);
		};

		return result;
	}
}
