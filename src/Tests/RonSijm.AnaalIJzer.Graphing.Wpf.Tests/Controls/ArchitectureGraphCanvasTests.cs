using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using AwesomeAssertions;
using Nodify;
using RonSijm.AnaalIJzer.Graphing.Building;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
using RonSijm.AnaalIJzer.Graphing.Wpf.Controls;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;
using Xunit;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Tests.Controls;

public sealed partial class ArchitectureGraphCanvasTests
{
	[Fact]
	public void NodifyCanvas_MaterializesSimpleGraph()
	{
		RunOnStaThread(() =>
		{
			var group = ArchitectureGraphViewModelBuilder.Build(CreateSimpleSnapshot(), ArchitectureGraphFocusMode.HighlightCurrent)[0];

			AssertCanvasMaterializes(group);
		});
	}

	[Fact]
	public void NodifyCanvas_MaterializesComplexNestedAndWildcardGraphs()
	{
		RunOnStaThread(() =>
		{
			var groups = ArchitectureGraphViewModelBuilder.Build(CreateComplexSnapshot(), ArchitectureGraphFocusMode.ShowAll)
				.Where(group => group.Nodes.Length > 0)
				.ToImmutableArray();

			groups.Should().NotBeEmpty();
			foreach (var group in groups)
			{
				AssertCanvasMaterializes(group);
			}
		});
	}

	[Fact]
	public void NodifyCanvas_CreatesDistinctConnectionContextMenus()
	{
		RunOnStaThread(() =>
		{
			var group = ArchitectureGraphViewModelBuilder.Build(CreateSimpleSnapshot(), ArchitectureGraphFocusMode.HighlightCurrent)[0];
			var control = new ArchitectureGraphCanvas(group);
			control.Measure(new Size(960, 640));
			control.Arrange(new Rect(0, 0, 960, 640));
			control.UpdateLayout();
			var connections = FindVisualDescendants<Connection>(control).ToImmutableArray();

			connections.Should().HaveCountGreaterThan(1);
			foreach (var connection in connections)
			{
				connection.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
			}

			connections.Select(connection => connection.ContextMenu).Should().NotContainNulls();
			connections.Select(connection => connection.ContextMenu).Should().OnlyHaveUniqueItems();
		});
	}

	[Fact]
	public void NodifyCanvas_OpensConnectionContextMenu()
	{
		RunOnStaThread(() =>
		{
			var group = ArchitectureGraphViewModelBuilder.Build(CreateSimpleSnapshot(), ArchitectureGraphFocusMode.HighlightCurrent)[0];
			var control = new ArchitectureGraphCanvas(group);
			control.Measure(new Size(960, 640));
			control.Arrange(new Rect(0, 0, 960, 640));
			control.UpdateLayout();
			var connection = FindVisualDescendants<Connection>(control).First();

			connection.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
			connection.ContextMenu.Should().NotBeNull();
			connection.ContextMenu!.PlacementTarget = connection;
			connection.ContextMenu.DataContext = connection.DataContext;
			var allSites = connection.ContextMenu.Items.OfType<MenuItem>().Single(item => string.Equals(item.Header?.ToString(), "Allow all sites", StringComparison.Ordinal));
			BindingOperations.GetBinding(allSites, MenuItem.IsCheckedProperty)!.Mode.Should().Be(BindingMode.OneWay);

			connection.ContextMenu.IsOpen = true;
			DrainDispatcher();

			connection.ContextMenu.IsOpen = false;
			DrainDispatcher();
		});
	}

	[Fact]
	public void GraphTheme_RegistersThemedPopupAndButtonStyles()
	{
		RunOnStaThread(() =>
		{
			var root = new Grid();
			ArchitectureGraphEditorTheme.Default.ApplyToRoot(root);

			root.Resources[typeof(Button)].Should().BeOfType<Style>()
				.Which.Setters.OfType<Setter>().Should().Contain(setter => setter.Property == Control.TemplateProperty);
			var comboBoxStyle = root.Resources[typeof(ComboBox)].Should().BeOfType<Style>().Subject;
			comboBoxStyle.Setters.OfType<Setter>().Should().Contain(setter => setter.Property == Control.TemplateProperty);
			comboBoxStyle.Setters.OfType<Setter>().Should().Contain(setter => setter.Property == ItemsControl.ItemContainerStyleProperty);
			root.Resources[typeof(ComboBoxItem)].Should().BeOfType<Style>()
				.Which.Setters.OfType<Setter>().Should().Contain(setter => setter.Property == Control.TemplateProperty);
			root.Resources[typeof(ContextMenu)].Should().BeOfType<Style>();
			root.Resources[typeof(MenuItem)].Should().BeOfType<Style>();
			root.Resources[typeof(Separator)].Should().BeOfType<Style>();
		});
	}

	[Fact]
	public void GraphTheme_ThemedComboBoxTemplateCanOpenDropdown()
	{
		RunOnStaThread(() =>
		{
			var root = new Grid();
			ArchitectureGraphEditorTheme.Default.ApplyToRoot(root);
			var comboBox = new ComboBox { ItemsSource = new[] { "Class", "Namespace", "Assembly" }, SelectedIndex = 0 };
			root.Children.Add(comboBox);
			var window = new Window { Content = root, Width = 320, Height = 120, WindowStyle = WindowStyle.None, ShowInTaskbar = false };
			window.Show();
			window.UpdateLayout();
			comboBox.ApplyTemplate();
			DrainDispatcher();
			var toggle = FindVisualDescendant<ToggleButton>(comboBox);

			toggle.Should().NotBeNull("the custom ComboBox template needs a clickable dropdown toggle");
			toggle!.TemplatedParent.Should().BeSameAs(comboBox);
			BindingOperations.GetBinding(toggle, ToggleButton.IsCheckedProperty).Should().NotBeNull();
			RaiseComboBoxClick(comboBox);

			comboBox.IsDropDownOpen.Should().BeTrue();

			RaiseComboBoxClick(comboBox);
			comboBox.IsDropDownOpen.Should().BeFalse();
			window.Close();
		});
	}

	[Fact]
	public void NodifyCanvas_ContextMenusUseThemedMenuItemStyles()
	{
		RunOnStaThread(() =>
		{
			var group = ArchitectureGraphViewModelBuilder.Build(CreateSimpleSnapshot(), ArchitectureGraphFocusMode.HighlightCurrent)[0];
			var control = new ArchitectureGraphCanvas(group);
			control.Measure(new Size(960, 640));
			control.Arrange(new Rect(0, 0, 960, 640));
			control.UpdateLayout();
			var editor = FindVisualDescendant<NodifyEditor>(control);

			editor!.ContextMenu.Should().NotBeNull();
			editor.ContextMenu!.ItemContainerStyle.Should().NotBeNull();
			editor.ContextMenu.Resources[typeof(MenuItem)].Should().BeOfType<Style>();
			editor.ContextMenu.Resources[typeof(Separator)].Should().BeOfType<Style>();
		});
	}

	private static void AssertCanvasMaterializes(ArchitectureGraphGroupViewModel group)
	{
		var control = new ArchitectureGraphCanvas(group);
		control.Measure(new Size(960, 640));
		control.Arrange(new Rect(0, 0, 960, 640));
		control.UpdateLayout();

		var editor = FindVisualDescendant<NodifyEditor>(control);
		editor.Should().NotBeNull("the graph surface should contain a Nodify editor");
		editor!.ItemsSource.Should().NotBeNull();
		editor.Connections.Should().NotBeNull();
		editor.ItemTemplateSelector.Should().NotBeNull();
		editor.ConnectionTemplate.Should().NotBeNull();
		editor.PendingConnection.Should().NotBeNull();
		editor.PendingConnectionTemplate.Should().NotBeNull();

		Count(editor.ItemsSource).Should().Be(group.Nodes.Length + group.Boundaries.Length);
		Count(editor.Connections).Should().Be(CountRenderableEdges(group));
		FindVisualDescendant<Minimap>(control).Should().NotBeNull("the graph surface should include the Nodify minimap");
		AssertConnectionsHaveResolvedAnchors(editor);
	}

	[Fact]
	public void NodifyCanvas_MovingBoundaryMovesChildrenAndChildrenStayInsideBoundary()
	{
		RunOnStaThread(() =>
		{
			var group = ArchitectureGraphViewModelBuilder.Build(CreateComplexSnapshot(), ArchitectureGraphFocusMode.ShowAll)
				.Single(group => group.Boundaries.Any(boundary => boundary.Path == "Application"));
			var control = new ArchitectureGraphCanvas(group);
			control.Measure(new Size(960, 640));
			control.Arrange(new Rect(0, 0, 960, 640));
			control.UpdateLayout();
			var editor = FindVisualDescendant<NodifyEditor>(control)!;
			var items = editor.ItemsSource.Cast<object>().ToImmutableArray();
			var boundary = FindGraphItem(items, "Application", "Boundary");
			var child = FindGraphItem(items, "Application/Contracts", "Node");
			var originalBoundaryLocation = GetProperty<Point>(boundary, "Location");
			var originalChildLocation = GetProperty<Point>(child, "Location");

			SetProperty(boundary, "Location", new Point(originalBoundaryLocation.X + 35, originalBoundaryLocation.Y + 25));

			var movedBoundaryLocation = GetProperty<Point>(boundary, "Location");
			var movedChildLocation = GetProperty<Point>(child, "Location");
			movedChildLocation.Should().Be(new Point(originalChildLocation.X + 35, originalChildLocation.Y + 25));

			SetProperty(child, "Location", new Point(movedBoundaryLocation.X - 500, movedBoundaryLocation.Y - 500));

			var clampedChildLocation = GetProperty<Point>(child, "Location");
			var boundarySize = GetProperty<Size>(boundary, "ActualSize");
			clampedChildLocation.X.Should().BeGreaterThan(movedBoundaryLocation.X);
			clampedChildLocation.Y.Should().BeGreaterThan(movedBoundaryLocation.Y);
			clampedChildLocation.X.Should().BeLessThan(movedBoundaryLocation.X + boundarySize.Width);
			clampedChildLocation.Y.Should().BeLessThan(movedBoundaryLocation.Y + boundarySize.Height);
		});
	}

	[Fact]
	public void NodifyCanvas_BoundariesAreResizableAndStayLargeEnoughForChildren()
	{
		RunOnStaThread(() =>
		{
			var group = ArchitectureGraphViewModelBuilder.Build(CreateComplexSnapshot(), ArchitectureGraphFocusMode.ShowAll)
				.Single(group => group.Boundaries.Any(boundary => boundary.Path == "Application"));
			var control = new ArchitectureGraphCanvas(group);
			control.Measure(new Size(960, 640));
			control.Arrange(new Rect(0, 0, 960, 640));
			control.UpdateLayout();
			var editor = FindVisualDescendant<NodifyEditor>(control)!;
			var items = editor.ItemsSource.Cast<object>().ToImmutableArray();
			var boundary = FindGraphItem(items, "Application", "Boundary");
			var child = FindGraphItem(items, "Application/Contracts", "Node");
			var groupingNode = FindVisualDescendants<GroupingNode>(control)
				.Single(node => ReferenceEquals(node.DataContext, boundary));

			groupingNode.CanResize.Should().BeTrue();

			var initialSize = GetProperty<Size>(boundary, "ActualSize");
			var expandedSize = new Size(initialSize.Width + 120, initialSize.Height + 80);
			SetProperty(boundary, "ActualSize", expandedSize);

			GetProperty<Size>(boundary, "ActualSize").Should().Be(expandedSize);

			SetProperty(boundary, "ActualSize", new Size(1, 1));

			var clampedSize = GetProperty<Size>(boundary, "ActualSize");
			var minimumWidth = GetProperty<double>(boundary, "MinimumWidth");
			var minimumHeight = GetProperty<double>(boundary, "MinimumHeight");
			clampedSize.Width.Should().BeGreaterThanOrEqualTo(minimumWidth);
			clampedSize.Height.Should().BeGreaterThanOrEqualTo(minimumHeight);

			var boundaryLocation = GetProperty<Point>(boundary, "Location");
			var largerSize = new Size(minimumWidth + 420, minimumHeight + 320);
			SetProperty(boundary, "ActualSize", largerSize);
			SetProperty(child, "Location", new Point(boundaryLocation.X + largerSize.Width - 180, boundaryLocation.Y + largerSize.Height - 140));

			GetProperty<double>(boundary, "MinimumWidth").Should().BeGreaterThan(minimumWidth);
			GetProperty<double>(boundary, "MinimumHeight").Should().BeGreaterThan(minimumHeight);
		});
	}

	[Fact]
	public void NodifyCanvas_RendersParentBoundaryConnectionsWithoutDuplicateParentNode()
	{
		RunOnStaThread(() =>
		{
			var group = ArchitectureGraphViewModelBuilder.Build(CreateParentEndpointSnapshot(), ArchitectureGraphFocusMode.ShowAll).Single();
			var control = new ArchitectureGraphCanvas(group);
			control.Measure(new Size(960, 640));
			control.Arrange(new Rect(0, 0, 960, 640));
			control.UpdateLayout();
			var editor = FindVisualDescendant<NodifyEditor>(control)!;
			var items = editor.ItemsSource!.Cast<object>().ToImmutableArray();

			items.Where(item => string.Equals(GetProperty<string>(item, "Path"), "Application", StringComparison.Ordinal)).Should().ContainSingle();
			FindGraphItem(items, "Application", "Boundary").Should().NotBeNull();
			items.Should().NotContain(item => item.GetType().Name.Contains("Node", StringComparison.Ordinal) && string.Equals(GetProperty<string>(item, "Path"), "Application", StringComparison.Ordinal));
			editor.Connections.Cast<object>().Should().ContainSingle();
			AssertConnectionsHaveResolvedAnchors(editor);
		});
	}

}
