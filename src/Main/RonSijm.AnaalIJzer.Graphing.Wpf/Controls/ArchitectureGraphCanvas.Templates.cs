using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Nodify;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Controls;

public sealed partial class ArchitectureGraphCanvas
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

	private DataTemplate CreateBoundaryTemplate()
	{
		var factory = new FrameworkElementFactory(typeof(GroupingNode));
		factory.SetBinding(HeaderedContentControl.HeaderProperty, new Binding(nameof(NodifyGraphBoundaryViewModel.HeaderText)));
		factory.SetBinding(ContentControl.ContentProperty, new Binding("."));
		factory.SetValue(ContentControl.ContentTemplateProperty, CreateBoundaryConnectorTemplate());
		factory.SetBinding(GroupingNode.ActualSizeProperty, new Binding(nameof(NodifyGraphBoundaryViewModel.ActualSize)) { Mode = BindingMode.TwoWay });
		factory.SetBinding(GroupingNode.HeaderBrushProperty, new Binding(nameof(NodifyGraphBoundaryViewModel.HeaderBrush)));
		factory.SetBinding(Control.ForegroundProperty, new Binding(nameof(NodifyGraphBoundaryViewModel.Foreground)));
		factory.SetBinding(Control.BackgroundProperty, new Binding(nameof(NodifyGraphBoundaryViewModel.Background)));
		factory.SetBinding(Control.BorderBrushProperty, new Binding(nameof(NodifyGraphBoundaryViewModel.BorderBrush)));
		factory.SetBinding(Control.BorderThicknessProperty, new Binding(nameof(NodifyGraphBoundaryViewModel.BorderThickness)));
		factory.SetBinding(FrameworkElement.MinWidthProperty, new Binding(nameof(NodifyGraphBoundaryViewModel.MinimumWidth)));
		factory.SetBinding(FrameworkElement.MinHeightProperty, new Binding(nameof(NodifyGraphBoundaryViewModel.MinimumHeight)));
		factory.SetValue(GroupingNode.CanResizeProperty, true);
		factory.SetValue(GroupingNode.IsContentHitTestVisibleProperty, true);
		factory.SetValue(GroupingNode.MovementModeProperty, GroupingMovementMode.Self);
		factory.SetBinding(FrameworkElement.ToolTipProperty, new Binding(nameof(NodifyGraphBoundaryViewModel.ToolTip)));
		factory.AddHandler(FrameworkElement.LoadedEvent, new RoutedEventHandler(BoundaryLoaded));
		factory.AddHandler(UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(BoundaryMouseLeftButtonDown));

		return new DataTemplate(typeof(NodifyGraphBoundaryViewModel)) { VisualTree = factory };
	}

	private DataTemplate CreateBoundaryConnectorTemplate()
	{
		var grid = new FrameworkElementFactory(typeof(Grid));
		grid.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
		grid.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);
		grid.SetValue(FrameworkElement.MinHeightProperty, 28d);

		var input = CreateConnectorFactory(typeof(NodeInput), NodeInput.HeaderProperty, nameof(NodifyGraphBoundaryViewModel.Input));
		input.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
		input.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Top);
		input.SetValue(FrameworkElement.MarginProperty, new Thickness(2, 4, 0, 0));
		grid.AppendChild(input);

		var output = CreateConnectorFactory(typeof(NodeOutput), NodeOutput.HeaderProperty, nameof(NodifyGraphBoundaryViewModel.Output));
		output.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
		output.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Top);
		output.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 4, 2, 0));
		grid.AppendChild(output);

		var result = new DataTemplate(typeof(NodifyGraphBoundaryViewModel)) { VisualTree = grid };

		return result;
	}

	private static Style CreateItemContainerStyle()
	{
		var style = new Style(typeof(ItemContainer));
		style.Setters.Add(new Setter(ItemContainer.LocationProperty, new Binding("Location") { Mode = BindingMode.TwoWay }));
		style.Setters.Add(new Setter(ItemContainer.IsSelectedProperty, new Binding("IsActive") { Mode = BindingMode.OneWay }));
		style.Setters.Add(new Setter(ItemContainer.IsDraggableProperty, true));
		style.Setters.Add(new Setter(ItemContainer.SelectedBorderThicknessProperty, new Thickness(4)));
		style.Setters.Add(new Setter(Control.BorderBrushProperty, new Binding("BorderBrush")));
		style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Binding("BorderThickness")));

		return style;
	}

	private DataTemplate CreateNodeTemplate()
	{
		var factory = new FrameworkElementFactory(typeof(Node));
		factory.SetValue(FrameworkElement.WidthProperty, NodeWidth);
		factory.SetValue(FrameworkElement.HeightProperty, NodeHeight);
		factory.SetValue(Node.ContentPaddingProperty, new Thickness(8, 2, 8, 2));
		factory.SetBinding(HeaderedContentControl.HeaderProperty, new Binding(nameof(NodifyGraphNodeViewModel.DisplayName)));
		factory.SetBinding(ContentControl.ContentProperty, new Binding(nameof(NodifyGraphNodeViewModel.ContentText)));
		factory.SetBinding(Node.InputProperty, new Binding(nameof(NodifyGraphNodeViewModel.Inputs)));
		factory.SetBinding(Node.OutputProperty, new Binding(nameof(NodifyGraphNodeViewModel.Outputs)));
		factory.SetValue(Node.InputConnectorTemplateProperty, CreateInputConnectorTemplate());
		factory.SetValue(Node.OutputConnectorTemplateProperty, CreateOutputConnectorTemplate());
		factory.SetBinding(Node.HeaderBrushProperty, new Binding(nameof(NodifyGraphNodeViewModel.HeaderBrush)));
		factory.SetBinding(Node.ContentBrushProperty, new Binding(nameof(NodifyGraphNodeViewModel.ContentBrush)));
		factory.SetBinding(Control.ForegroundProperty, new Binding(nameof(NodifyGraphNodeViewModel.Foreground)));
		factory.SetBinding(FrameworkElement.ToolTipProperty, new Binding(nameof(NodifyGraphNodeViewModel.ToolTip)));
		factory.AddHandler(FrameworkElement.LoadedEvent, new RoutedEventHandler(NodeLoaded));
		factory.AddHandler(UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(NodeMouseLeftButtonDown));

		return new DataTemplate(typeof(NodifyGraphNodeViewModel)) { VisualTree = factory };
	}

	private void NodeLoaded(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement element && element.ContextMenu is null)
		{
			element.ContextMenu = CreateNodeContextMenu();
		}
	}

	private void BoundaryLoaded(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement element && element.ContextMenu is null)
		{
			element.ContextMenu = CreateNodeContextMenu();
		}
	}

	private DataTemplate CreateInputConnectorTemplate()
	{
		var factory = CreateConnectorFactory(typeof(NodeInput), NodeInput.HeaderProperty);
		var result = new DataTemplate(typeof(NodifyGraphConnectorViewModel)) { VisualTree = factory };

		return result;
	}

	private DataTemplate CreateOutputConnectorTemplate()
	{
		var factory = CreateConnectorFactory(typeof(NodeOutput), NodeOutput.HeaderProperty);
		var result = new DataTemplate(typeof(NodifyGraphConnectorViewModel)) { VisualTree = factory };

		return result;
	}

	private FrameworkElementFactory CreateConnectorFactory(Type connectorType, DependencyProperty headerProperty)
	{
		return CreateConnectorFactory(connectorType, headerProperty, string.Empty);
	}

	private FrameworkElementFactory CreateConnectorFactory(Type connectorType, DependencyProperty headerProperty, string bindingPrefix)
	{
		var factory = new FrameworkElementFactory(connectorType);
		var prefix = string.IsNullOrWhiteSpace(bindingPrefix) ? string.Empty : bindingPrefix + ".";
		factory.SetBinding(headerProperty, new Binding(prefix + nameof(NodifyGraphConnectorViewModel.Title)));
		factory.SetBinding(Connector.AnchorProperty, new Binding(prefix + nameof(NodifyGraphConnectorViewModel.Anchor)) { Mode = BindingMode.OneWayToSource });
		factory.SetValue(Connector.IsConnectedProperty, true);
		factory.SetValue(Control.ForegroundProperty, theme.Foreground);
		factory.SetBinding(FrameworkElement.ToolTipProperty, new Binding(prefix + nameof(NodifyGraphConnectorViewModel.ToolTip)));

		return factory;
	}

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
