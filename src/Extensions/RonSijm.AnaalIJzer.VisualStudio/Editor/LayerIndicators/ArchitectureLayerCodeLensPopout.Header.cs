using System.Windows;
using System.Windows.Controls;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.VisualStudio.Editor.Styling;
using RonSijm.AnaalIJzer.VisualStudio.Styling;

namespace RonSijm.AnaalIJzer.VisualStudio.Editor.LayerIndicators;

internal static partial class ArchitectureLayerCodeLensPopout
{
	private static UIElement CreateHeader(ArchitectureLayerIndicator indicator)
	{
		var accentBrush = indicator.IsInLayer ? ArchitecturePalette.GetBrush(indicator.PaletteSlot) : ArchitecturePalette.GetUnclassifiedBrush();
		var accent = new Border
		{
			Width = 4,
			Background = accentBrush
		};
		var title = new TextBlock
		{
			Text = indicator.TypeName,
			FontSize = 14,
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(0, 0, 0, 2)
		};
		var subtitle = new TextBlock
		{
			Text = indicator.IsInLayer ? indicator.LayerPath : "not in a configured layer",
			FontSize = 11
		};
		ArchitectureVisualStudioTheme.ApplyHintForeground(subtitle);

		var text = new StackPanel
		{
			Margin = new Thickness(12, 10, 12, 10),
			Children =
			{
				title,
				subtitle
			}
		};
		var result = new DockPanel
		{
			LastChildFill = true,
			Children =
			{
				accent,
				text
			}
		};

		return result;
	}
}
