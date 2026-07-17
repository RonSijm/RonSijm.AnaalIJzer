using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.VisualStudio.Shell;
using RonSijm.AnaalIJzer.Indicators;
using RonSijm.AnaalIJzer.Snapshots;
using RonSijm.AnaalIJzer.VisualStudio.Options;
using RonSijm.AnaalIJzer.VisualStudio.Styling;

namespace RonSijm.AnaalIJzer.VisualStudio.LayerIndicators;

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

internal static class ArchitectureLayerCodeLensText
{
	internal static string CreateSummary(ArchitectureLayerIndicator indicator, ArchitectureEditorOptions options)
	{
		if (!indicator.IsInLayer)
		{
			var unclassifiedResult = "AnaalIJzer layer: not in a configured layer";

			return unclassifiedResult;
		}

		var inboundCount = CountLayers(indicator.LayersThatCanCallThisLayer, options);
		var outboundCount = CountLayers(indicator.LayersThisLayerCanCall, options);
		var result = "AnaalIJzer layer: "
		             + indicator.LayerPath
		             + " | called by "
		             + FormatLayerCount(inboundCount)
		             + " | can call "
		             + FormatLayerCount(outboundCount);

		return result;
	}

	private static int CountLayers(ImmutableArray<string> layers, ArchitectureEditorOptions options)
	{
		var count = layers.Count(layer => options.ShowGlobalLayerRulesInBadges || !IsGlobalLayerRule(layer));
		var result = count;

		return result;
	}

	private static bool IsGlobalLayerRule(string layer)
	{
		var result = layer == "*" || layer.StartsWith("* ", StringComparison.Ordinal);

		return result;
	}

	private static string FormatLayerCount(int count)
	{
		var result = count == 1 ? "1 layer" : count + " layers";

		return result;
	}
}

internal static class ArchitectureLayerCodeLensPopout
{
	private static Popup? currentPopup;

	internal static void Show(ArchitectureLayerIndicator indicator, FrameworkElement placementTarget)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		currentPopup?.IsOpen = false;
		var options = ArchitectureVisualStudioOptions.Current;
		var popup = new Popup
		{
			AllowsTransparency = true,
			Placement = PlacementMode.Bottom,
			PlacementTarget = placementTarget,
			StaysOpen = false,
			VerticalOffset = 2
		};
		var root = CreateRoot(indicator, options, popup);
		popup.Child = root;
		popup.Closed += (_, _) =>
		{
			if (ReferenceEquals(currentPopup, popup))
			{
				currentPopup = null;
			}
		};
		popup.Opened += (_, _) => root.Focus();
		currentPopup = popup;
		popup.IsOpen = true;
	}

	private static Border CreateRoot(ArchitectureLayerIndicator indicator, ArchitectureEditorOptions options, Popup popup)
	{
		var panel = new StackPanel
		{
			Width = 520
		};
		panel.Children.Add(CreateHeader(indicator));
		panel.Children.Add(CreateDetails(indicator, options));

		var root = new Border
		{
			Child = panel,
			CornerRadius = new CornerRadius(3),
			Focusable = true,
			MaxHeight = 460,
			MinWidth = 420,
			Padding = new Thickness(0),
			SnapsToDevicePixels = true,
			BorderThickness = new Thickness(1),
			Effect = new DropShadowEffect
			{
				BlurRadius = 18,
				Opacity = 0.28,
				ShadowDepth = 3
			}
		};
		ArchitectureVisualStudioTheme.ApplyToToolWindow(root);
		ArchitectureVisualStudioTheme.ApplyBackground(root);
		root.KeyDown += (_, args) =>
		{
			if (args.Key == Key.Escape)
			{
				popup.IsOpen = false;
				args.Handled = true;
			}
		};

		return root;
	}

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

	private static UIElement CreateDetails(ArchitectureLayerIndicator indicator, ArchitectureEditorOptions options)
	{
		var body = new StackPanel
		{
			Margin = new Thickness(12, 0, 12, 12)
		};
		if (!indicator.IsInLayer)
		{
			var description = string.IsNullOrWhiteSpace(indicator.Description)
				? "This type is not matched by the current AnaalIJzer settings."
				: indicator.Description!;
			AddSection(body, "Status", [description]);
		}
		else
		{
			if (indicator.LayerAncestry.Length > 0)
			{
				AddSection(body, "Ancestry", [string.Join(" > ", indicator.LayerAncestry)]);
			}

			if (options.ShowLinearCallChainInBadges && indicator.LinearCallChain.Length > 1)
			{
				AddSection(body, "Call Chain", [string.Join(" -> ", indicator.LinearCallChain)]);
			}

			AddSection(body, "Can Be Called By", FilterGlobalLayerRules(indicator.LayersThatCanCallThisLayer, options));
			AddSection(body, "Can Call", FilterGlobalLayerRules(indicator.LayersThisLayerCanCall, options));
			if (!string.IsNullOrWhiteSpace(indicator.Description))
			{
				AddSection(body, "Description", [indicator.Description!]);
			}
		}

		var scroller = new ScrollViewer
		{
			Content = body,
			MaxHeight = 390,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};

		return scroller;
	}

	private static void AddSection(Panel parent, string title, ImmutableArray<string> values)
	{
		var section = new StackPanel
		{
			Margin = new Thickness(0, 8, 0, 0)
		};
		section.Children.Add(new TextBlock
		{
			Text = title,
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(0, 0, 0, 4)
		});
		if (values.Length == 0)
		{
			var empty = new TextBlock
			{
				Text = "none configured",
				TextWrapping = TextWrapping.Wrap
			};
			ArchitectureVisualStudioTheme.ApplyHintForeground(empty);
			section.Children.Add(empty);
		}
		else
		{
			foreach (var value in values)
			{
				section.Children.Add(new TextBlock
				{
					Text = value,
					TextWrapping = TextWrapping.Wrap,
					Margin = new Thickness(0, 0, 0, 2)
				});
			}
		}

		parent.Children.Add(section);
	}

	private static ImmutableArray<string> FilterGlobalLayerRules(ImmutableArray<string> layers, ArchitectureEditorOptions options)
	{
		if (options.ShowGlobalLayerRulesInBadges)
		{
			return layers;
		}

		var result = layers
			.Where(layer => !IsGlobalLayerRule(layer))
			.ToImmutableArray();

		return result;
	}

	private static bool IsGlobalLayerRule(string layer)
	{
		var result = layer == "*" || layer.StartsWith("* ", StringComparison.Ordinal);

		return result;
	}
}
