using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Nodify;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private static Minimap CreateMinimap(NodifyEditor editor)
	{
		var minimap = new Minimap
		{
			Width = 156,
			Height = 104,
			Margin = new Thickness(8),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			IsReadOnly = false,
			ResizeToViewport = true,
			ItemContainerStyle = CreateMinimapItemContainerStyle(),
			ItemTemplate = CreateMinimapNodeTemplate()
		};
		minimap.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(NodifyEditor.ItemsSource)) { Source = editor });
		minimap.SetBinding(Minimap.ViewportLocationProperty, new Binding(nameof(NodifyEditor.ViewportLocation)) { Source = editor, Mode = BindingMode.TwoWay });
		minimap.SetBinding(Minimap.ViewportSizeProperty, new Binding(nameof(NodifyEditor.ViewportSize)) { Source = editor });

		return minimap;
	}

	private static Style CreateMinimapItemContainerStyle()
	{
		var style = new Style(typeof(MinimapItem));
		style.Setters.Add(new Setter(MinimapItem.LocationProperty, new Binding(nameof(NodifyGraphNodeViewModel.Location))));

		return style;
	}

	private static DataTemplate CreateMinimapNodeTemplate()
	{
		var factory = new FrameworkElementFactory(typeof(Border));
		factory.SetValue(FrameworkElement.WidthProperty, 40d);
		factory.SetValue(FrameworkElement.HeightProperty, 18d);
		factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(2));
		factory.SetBinding(Border.BackgroundProperty, new Binding(nameof(NodifyGraphNodeViewModel.HeaderBrush)));

		return new DataTemplate(typeof(NodifyGraphNodeViewModel)) { VisualTree = factory };
	}
}
