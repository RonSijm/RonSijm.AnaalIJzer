using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RonSijm.AnaalIJzer.Indicators;
using RonSijm.AnaalIJzer.QuickInfo;
using RonSijm.AnaalIJzer.VisualStudio.Options;
using RonSijm.AnaalIJzer.VisualStudio.Styling;

namespace RonSijm.AnaalIJzer.VisualStudio.Tags;

internal static class ArchitectureAdornmentFactory
{
	internal static UIElement CreateLayerBadge(ArchitectureLayerIndicator indicator)
	{
		var slotColor = indicator.IsInLayer ? ArchitecturePalette.GetBrush(indicator.PaletteSlot) : ArchitecturePalette.GetUnclassifiedBrush();
		var content = ArchitectureQuickInfoContentBuilder.CreateLayerContent(indicator, ArchitectureVisualStudioOptions.Current);
		var label = new Border
		{
			Background = slotColor,
			CornerRadius = new CornerRadius(3),
			Margin = new Thickness(4, 0, 0, 0),
			Padding = new Thickness(4, 0, 4, 1),
			ToolTip = content.ToString(),
			Child = new TextBlock
			{
				Text = indicator.LayerPath,
				Foreground = Brushes.White,
				FontSize = 10,
				FontWeight = FontWeights.SemiBold
			}
		};

		return label;
	}

	internal static UIElement CreateSiteBadge(ArchitectureDependencySiteIndicator indicator)
	{
		var brush = ArchitecturePalette.GetStatusBrush(indicator.Status);
		var label = new Border
		{
			Background = brush,
			CornerRadius = new CornerRadius(3),
			Margin = new Thickness(4, 0, 0, 0),
			Padding = new Thickness(4, 0, 4, 1),
			ToolTip = ArchitectureQuickInfoContentBuilder.CreateSiteContent(indicator).ToString(),
			Child = new TextBlock
			{
				Text = indicator.Site,
				Foreground = Brushes.White,
				FontSize = 10
			}
		};

		return label;
	}

	internal static UIElement CreateSiteLayerBadge(ArchitectureDependencySiteIndicator indicator)
	{
		var brush = ArchitecturePalette.GetBrush(indicator.DependencyLayerPaletteSlot);
		var dependencyLayer = indicator.DependencyLayerPath ?? "not in layer";
		var label = new Border
		{
			Background = brush,
			CornerRadius = new CornerRadius(3),
			Margin = new Thickness(4, 0, 0, 0),
			Padding = new Thickness(4, 0, 4, 1),
			ToolTip = ArchitectureQuickInfoContentBuilder.CreateSiteContent(indicator).ToString(),
			Child = new TextBlock
			{
				Text = dependencyLayer,
				Foreground = Brushes.White,
				FontSize = 10,
				FontWeight = FontWeights.SemiBold
			}
		};

		return label;
	}

	internal static UIElement CreateLayerGlyph(ArchitectureLayerIndicator indicator)
	{
		var brush = ArchitecturePalette.GetBrush(indicator.PaletteSlot);
		var content = ArchitectureQuickInfoContentBuilder.CreateLayerContent(indicator, ArchitectureVisualStudioOptions.Current);
		var glyph = new Border
		{
			Width = 10,
			Height = 10,
			CornerRadius = new CornerRadius(5),
			Background = brush,
			ToolTip = content.ToString()
		};

		return glyph;
	}
}
