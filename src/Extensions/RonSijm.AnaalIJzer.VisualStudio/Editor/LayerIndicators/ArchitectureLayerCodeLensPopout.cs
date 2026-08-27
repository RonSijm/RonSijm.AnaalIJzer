using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Effects;
using Microsoft.VisualStudio.Shell;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.VisualStudio.Options;
using RonSijm.AnaalIJzer.VisualStudio.Styling;

namespace RonSijm.AnaalIJzer.VisualStudio.Editor.LayerIndicators;

internal static partial class ArchitectureLayerCodeLensPopout
{
	private static Popup? _currentPopup;

	internal static void Show(ArchitectureLayerIndicator indicator, FrameworkElement placementTarget)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		if (_currentPopup is not null)
		{
			_currentPopup.IsOpen = false;
		}

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
			if (ReferenceEquals(_currentPopup, popup))
			{
				_currentPopup = null;
			}
		};
		popup.Opened += (_, _) => root.Focus();
		_currentPopup = popup;
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
}
