using System.Collections.Immutable;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AwesomeAssertions;
using Nodify;
using RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;
using RonSijm.AnaalIJzer.GraphModel.Loading;
using Xunit;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Tests.Controls;

public sealed partial class ArchitectureGraphEditorControlPersistenceTests
{
	[Fact]
	public void ConnectionContextMenuRemove_PersistsXmlAndReloadsDiagram()
	{
		RunOnStaThread(() =>
		{
			var path = WriteTempFile(
				"Architecture.anl",
				"""
				<ArchitecturalLevels>
				  <Layer name="Customer"><Class typeName="CustomerType" /></Layer>
				  <Layer name="Waiter"><Class typeName="WaiterType" /></Layer>
				  <AllowedDependency from="Customer" to="Waiter" />
				</ArchitecturalLevels>
				""");
			var control = CreateControl(
				ArchitectureGraphXmlSnapshotLoader.Load(path),
				_ => ArchitectureGraphXmlSnapshotLoader.Load(path),
				_ => true);
			var connection = FindVisualDescendants<Connection>(control).Single();
			connection.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
			var menu = connection.ContextMenu!;
			menu.PlacementTarget = connection;
			menu.DataContext = connection.DataContext;
			DrainDispatcher();
			var remove = FindMenuItemByHeader(menu.Items, "Remove connection");

			remove.Command.Should().NotBeNull();
			remove.Command!.Execute(null);
			DrainDispatcher();

			File.ReadAllText(path).Should().NotContain("AllowedDependency");
			FindVisualDescendants<Connection>(control).Should().BeEmpty();
		});
	}

	[Fact]
	public void ConnectionContextMenuRemove_PersistsInlineAssemblyMetadataAndReloadsDiagram()
	{
		RunOnStaThread(() =>
		{
			var path = WriteInlineConfigurationFile(
				"""
				<ArchitecturalLevels>
				  <Layer name="Customer"><Class typeName="CustomerType" /></Layer>
				  <Layer name="Waiter"><Class typeName="WaiterType" /></Layer>
				  <AllowedDependency from="Customer" to="Waiter" />
				</ArchitecturalLevels>
				""");
			var control = CreateControl(
				LoadInlineSnapshot(path),
				_ => LoadInlineSnapshot(path),
				_ => true);
			var connection = FindVisualDescendants<Connection>(control).Single();
			connection.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
			var menu = connection.ContextMenu!;
			menu.PlacementTarget = connection;
			menu.DataContext = connection.DataContext;
			DrainDispatcher();
			var remove = FindMenuItemByHeader(menu.Items, "Remove connection");

			remove.Command.Should().NotBeNull();
			remove.Command!.Execute(null);
			DrainDispatcher();

			File.ReadAllText(path).Should().NotContain("AllowedDependency");
			FindVisualDescendants<Connection>(control).Should().BeEmpty();
		});
	}

	[Fact]
	public void LayerContextMenuRemove_PersistsXmlAndReloadsDiagram()
	{
		RunOnStaThread(() =>
		{
			var path = WriteTempFile(
				"Architecture.anl",
				"""
				<ArchitecturalLevels>
				  <Layer name="Customer"><Class typeName="CustomerType" /></Layer>
				  <Layer name="Waiter"><Class typeName="WaiterType" /></Layer>
				  <AllowedDependency from="Customer" to="Waiter" />
				</ArchitecturalLevels>
				""");
			var control = CreateControl(
				ArchitectureGraphXmlSnapshotLoader.Load(path),
				_ => ArchitectureGraphXmlSnapshotLoader.Load(path),
				_ => true);
			var node = FindVisualDescendants<Node>(control).Single(item => string.Equals(GetDataContextProperty(item, "Path"), "Customer", StringComparison.Ordinal));
			node.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
			var menu = node.ContextMenu!;
			menu.PlacementTarget = node;
			menu.DataContext = node.DataContext;
			DrainDispatcher();
			var remove = FindMenuItemByHeader(menu.Items, "Remove layer");

			remove.Command.Should().NotBeNull();
			remove.Command!.Execute(null);
			DrainDispatcher();

			File.ReadAllText(path).Should().NotContain("Layer name=\"Customer\"");
			FindVisualDescendants<Node>(control).Select(item => GetDataContextProperty(item, "Path")).Should().NotContain("Customer");
		});
	}

	[Fact]
	public void LayerContextMenuRemove_PersistsInlineAssemblyMetadataAndReloadsDiagram()
	{
		RunOnStaThread(() =>
		{
			var path = WriteInlineConfigurationFile(
				"""
				<ArchitecturalLevels>
				  <Layer name="Customer"><Class typeName="CustomerType" /></Layer>
				  <Layer name="Waiter"><Class typeName="WaiterType" /></Layer>
				  <AllowedDependency from="Customer" to="Waiter" />
				</ArchitecturalLevels>
				""");
			var control = CreateControl(
				LoadInlineSnapshot(path),
				_ => LoadInlineSnapshot(path),
				_ => true);
			var node = FindVisualDescendants<Node>(control).Single(item => string.Equals(GetDataContextProperty(item, "Path"), "Customer", StringComparison.Ordinal));
			node.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
			var menu = node.ContextMenu!;
			menu.PlacementTarget = node;
			menu.DataContext = node.DataContext;
			DrainDispatcher();
			var remove = FindMenuItemByHeader(menu.Items, "Remove layer");

			remove.Command.Should().NotBeNull();
			remove.Command!.Execute(null);
			DrainDispatcher();

			File.ReadAllText(path).Should().NotContain("Layer name=\"Customer\"");
			FindVisualDescendants<Node>(control).Select(item => GetDataContextProperty(item, "Path")).Should().NotContain("Customer");
		});
	}

	[Fact]
	public void CanvasContextMenuAddRootLayer_PersistsXmlAndReloadsDiagram()
	{
		RunOnStaThread(() =>
		{
			var path = WriteTempFile(
				"Architecture.anl",
				"""
				<ArchitecturalLevels>
				  <Layer name="Customer"><Class typeName="CustomerType" /></Layer>
				  <Layer name="Waiter"><Class typeName="WaiterType" /></Layer>
				  <AllowedDependency from="Customer" to="Waiter" />
				</ArchitecturalLevels>
				""");
			var control = CreateControl(
				ArchitectureGraphXmlSnapshotLoader.Load(path),
				_ => ArchitectureGraphXmlSnapshotLoader.Load(path),
				_ => true,
				() => new ArchitectureLayerCreationRequest("Chef", "Class", ImmutableDictionary<string, string>.Empty.Add("endsWith", "Chef")));
			var editor = FindVisualDescendant<NodifyEditor>(control);
			var menu = editor.ContextMenu!;
			var addLayer = FindMenuItemByHeader(menu.Items, "Add root layer...");

			addLayer.Command.Should().NotBeNull();
			addLayer.Command!.Execute(null);
			DrainDispatcher();

			File.ReadAllText(path).Should().Contain("<Layer name=\"Chef\">");
			File.ReadAllText(path).Should().Contain("<Class endsWith=\"Chef\" />");
			FindVisualDescendants<Node>(control).Select(item => GetDataContextProperty(item, "Path")).Should().Contain("Chef");
		});
	}

	[Fact]
	public void CanvasContextMenuAddRootLayer_PersistsInlineAssemblyMetadataAndReloadsDiagram()
	{
		RunOnStaThread(() =>
		{
			var path = WriteInlineConfigurationFile(
				"""
				<ArchitecturalLevels>
				  <Layer name="Customer"><Class typeName="CustomerType" /></Layer>
				  <Layer name="Waiter"><Class typeName="WaiterType" /></Layer>
				  <AllowedDependency from="Customer" to="Waiter" />
				</ArchitecturalLevels>
				""");
			var control = CreateControl(
				LoadInlineSnapshot(path),
				_ => LoadInlineSnapshot(path),
				_ => true,
				() => new ArchitectureLayerCreationRequest("Chef", "Class", ImmutableDictionary<string, string>.Empty.Add("endsWith", "Chef")));
			var editor = FindVisualDescendant<NodifyEditor>(control);
			var menu = editor.ContextMenu!;
			var addLayer = FindMenuItemByHeader(menu.Items, "Add root layer...");

			addLayer.Command.Should().NotBeNull();
			addLayer.Command!.Execute(null);
			DrainDispatcher();

			File.ReadAllText(path).Should().Contain("<Layer name=\"Chef\">");
			File.ReadAllText(path).Should().Contain("<Class endsWith=\"Chef\" />");
			FindVisualDescendants<Node>(control).Select(item => GetDataContextProperty(item, "Path")).Should().Contain("Chef");
		});
	}

	[Fact]
	public void LayerContextMenuAddChildLayer_PersistsXmlAndReloadsDiagram()
	{
		RunOnStaThread(() =>
		{
			var path = WriteTempFile(
				"Architecture.anl",
				"""
				<ArchitecturalLevels>
				  <Layer name="Customer"><Class typeName="CustomerType" /></Layer>
				</ArchitecturalLevels>
				""");
			var control = CreateControl(
				ArchitectureGraphXmlSnapshotLoader.Load(path),
				_ => ArchitectureGraphXmlSnapshotLoader.Load(path),
				_ => true,
				() => new ArchitectureLayerCreationRequest("Vip", "Class", ImmutableDictionary<string, string>.Empty.Add("endsWith", "Vip")));
			var node = FindVisualDescendants<Node>(control).Single(item => string.Equals(GetDataContextProperty(item, "Path"), "Customer", StringComparison.Ordinal));
			node.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
			var menu = node.ContextMenu!;
			menu.PlacementTarget = node;
			menu.DataContext = node.DataContext;
			DrainDispatcher();
			var addChild = FindMenuItemByHeader(menu.Items, "Add child layer...");

			addChild.Command.Should().NotBeNull();
			addChild.Command!.Execute(null);
			DrainDispatcher();

			var xml = File.ReadAllText(path);
			xml.Should().Contain("<Layer name=\"Customer\">");
			xml.Should().Contain("<Layer name=\"Vip\">");
			FindVisualDescendants<Node>(control).Select(item => GetDataContextProperty(item, "Path")).Should().Contain("Customer/Vip");
		});
	}

	[Fact]
	public void LayerContextMenuAddChildLayer_PersistsInlineAssemblyMetadataAndReloadsDiagram()
	{
		RunOnStaThread(() =>
		{
			var path = WriteInlineConfigurationFile(
				"""
				<ArchitecturalLevels>
				  <Layer name="Customer"><Class typeName="CustomerType" /></Layer>
				</ArchitecturalLevels>
				""");
			var control = CreateControl(
				LoadInlineSnapshot(path),
				_ => LoadInlineSnapshot(path),
				_ => true,
				() => new ArchitectureLayerCreationRequest("Vip", "Class", ImmutableDictionary<string, string>.Empty.Add("endsWith", "Vip")));
			var node = FindVisualDescendants<Node>(control).Single(item => string.Equals(GetDataContextProperty(item, "Path"), "Customer", StringComparison.Ordinal));
			node.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
			var menu = node.ContextMenu!;
			menu.PlacementTarget = node;
			menu.DataContext = node.DataContext;
			DrainDispatcher();
			var addChild = FindMenuItemByHeader(menu.Items, "Add child layer...");

			addChild.Command.Should().NotBeNull();
			addChild.Command!.Execute(null);
			DrainDispatcher();

			var xml = File.ReadAllText(path);
			xml.Should().Contain("<Layer name=\"Customer\">");
			xml.Should().Contain("<Layer name=\"Vip\">");
			FindVisualDescendants<Node>(control).Select(item => GetDataContextProperty(item, "Path")).Should().Contain("Customer/Vip");
		});
	}

	[Fact]
	public void ConnectionCompletedCommand_PersistsXmlAndReloadsDiagram()
	{
		RunOnStaThread(() =>
		{
			var path = WriteTempFile(
				"Architecture.anl",
				"""
				<ArchitecturalLevels>
				  <Layer name="Customer"><Class typeName="CustomerType" /></Layer>
				  <Layer name="Waiter"><Class typeName="WaiterType" /></Layer>
				  <AllowedDependency from="Customer" to="Waiter" />
				</ArchitecturalLevels>
				""");
			var control = CreateControl(
				ArchitectureGraphXmlSnapshotLoader.Load(path),
				_ => ArchitectureGraphXmlSnapshotLoader.Load(path),
				_ => true);
			var editor = FindVisualDescendant<NodifyEditor>(control);
			var nodes = editor.ItemsSource!.Cast<object>().ToDictionary(item => GetText(GetObjectProperty(item, "Path"))!, StringComparer.Ordinal);
			var waiterOutput = GetObjectProperty(nodes["Waiter"], "Output");
			var customerInput = GetObjectProperty(nodes["Customer"], "Input");

			editor.ConnectionCompletedCommand.Should().NotBeNull();
			editor.ConnectionCompletedCommand!.Execute(Tuple.Create(waiterOutput, customerInput));
			DrainDispatcher();

			File.ReadAllText(path).Should().Contain("<AllowedDependency from=\"Waiter\" to=\"Customer\"");
			FindVisualDescendants<Connection>(control).Should().HaveCount(2);
		});
	}

	[Fact]
	public void ConnectionCompletedCommand_PersistsInlineAssemblyMetadataAndReloadsDiagram()
	{
		RunOnStaThread(() =>
		{
			var path = WriteInlineConfigurationFile(
				"""
				<ArchitecturalLevels>
				  <Layer name="Customer"><Class typeName="CustomerType" /></Layer>
				  <Layer name="Waiter"><Class typeName="WaiterType" /></Layer>
				  <AllowedDependency from="Customer" to="Waiter" />
				</ArchitecturalLevels>
				""");
			var control = CreateControl(
				LoadInlineSnapshot(path),
				_ => LoadInlineSnapshot(path),
				_ => true);
			var editor = FindVisualDescendant<NodifyEditor>(control);
			var nodes = editor.ItemsSource!.Cast<object>().ToDictionary(item => GetText(GetObjectProperty(item, "Path"))!, StringComparer.Ordinal);
			var waiterOutput = GetObjectProperty(nodes["Waiter"], "Output");
			var customerInput = GetObjectProperty(nodes["Customer"], "Input");

			editor.ConnectionCompletedCommand.Should().NotBeNull();
			editor.ConnectionCompletedCommand!.Execute(Tuple.Create(waiterOutput, customerInput));
			DrainDispatcher();

			File.ReadAllText(path).Should().Contain("<AllowedDependency from=\"Waiter\" to=\"Customer\"");
			FindVisualDescendants<Connection>(control).Should().HaveCount(2);
		});
	}

}
