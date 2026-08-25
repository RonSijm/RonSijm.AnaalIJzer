using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Nodify;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private DataTemplate CreateConnectionTemplate()
	{
		var factory = new FrameworkElementFactory(typeof(Connection));
		factory.SetValue(BaseConnection.HasCustomContextMenuProperty, true);
		factory.SetValue(BaseConnection.ArrowSizeProperty, new Size(9, 9));
		factory.SetValue(BaseConnection.TextPaddingProperty, new Thickness(4, 1, 4, 2));
		factory.SetValue(BaseConnection.TextCornerRadiusProperty, 3d);
		factory.SetValue(BaseConnection.FontSizeProperty, 10d);
		factory.SetValue(BaseConnection.ForegroundProperty, theme.ConnectionText);
		factory.SetValue(BaseConnection.SourceOffsetProperty, new Size(10, 0));
		factory.SetValue(BaseConnection.TargetOffsetProperty, new Size(10, 0));
		factory.SetBinding(BaseConnection.SourceProperty, new Binding(nameof(NodifyGraphConnectionViewModel.Output) + "." + nameof(NodifyGraphConnectorViewModel.Anchor)));
		factory.SetBinding(BaseConnection.TargetProperty, new Binding(nameof(NodifyGraphConnectionViewModel.Input) + "." + nameof(NodifyGraphConnectorViewModel.Anchor)));
		factory.SetBinding(Shape.StrokeProperty, new Binding(nameof(NodifyGraphConnectionViewModel.Stroke)));
		factory.SetBinding(Shape.StrokeThicknessProperty, new Binding(nameof(NodifyGraphConnectionViewModel.StrokeThickness)));
		factory.SetBinding(Shape.StrokeDashArrayProperty, new Binding(nameof(NodifyGraphConnectionViewModel.StrokeDashArray)));
		factory.SetBinding(BaseConnection.TextProperty, new Binding(nameof(NodifyGraphConnectionViewModel.LabelText)));
		factory.SetBinding(BaseConnection.TextBackgroundProperty, new Binding(nameof(NodifyGraphConnectionViewModel.TextBackground)));
		factory.SetBinding(FrameworkElement.ToolTipProperty, new Binding(nameof(NodifyGraphConnectionViewModel.ToolTip)));
		factory.AddHandler(FrameworkElement.LoadedEvent, new RoutedEventHandler(ConnectionLoaded));
		factory.AddHandler(UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(ConnectionMouseLeftButtonDown));

		return new DataTemplate(typeof(NodifyGraphConnectionViewModel)) { VisualTree = factory };
	}

	private DataTemplate CreatePendingConnectionTemplate()
	{
		PendingConnection.EnableHitTesting = true;
		var factory = new FrameworkElementFactory(typeof(PendingConnection));
		factory.SetValue(PendingConnection.StrokeProperty, theme.ActiveConnection);
		factory.SetValue(PendingConnection.StrokeThicknessProperty, 2.3d);
		factory.SetValue(PendingConnection.StrokeDashArrayProperty, new DoubleCollection([5, 3]));
		factory.SetValue(PendingConnection.AllowOnlyConnectorsProperty, true);
		factory.SetValue(PendingConnection.EnablePreviewProperty, true);
		factory.SetValue(PendingConnection.EnableSnappingProperty, true);
		factory.AddHandler(FrameworkElement.LoadedEvent, new RoutedEventHandler(PendingConnectionLoaded));

		return new DataTemplate { VisualTree = factory };
	}

	private static void PendingConnectionLoaded(object sender, RoutedEventArgs e)
	{
		if (sender is not PendingConnection pendingConnection)
		{
			return;
		}

		PendingConnection.EnableHitTesting = true;
		pendingConnection.EnablePreview = true;
		pendingConnection.EnableSnapping = true;
		pendingConnection.AllowOnlyConnectors = true;
	}
}
