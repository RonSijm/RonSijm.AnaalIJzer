using System.IO;
using System.Windows;
using System.Windows.Controls;
using AwesomeAssertions;
using RonSijm.AnaalIJzer.Graphing.Loading;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Graphing.Wpf.Controls;
using RonSijm.AnaalIJzer.Graphing.Wpf.Selection;
using Xunit;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Tests.Controls;

public sealed partial class ArchitectureGraphEditorControlPersistenceTests
{
	[Fact]
	public void MatcherAddEditor_UsesAttributeDropdown()
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
			var snapshot = ArchitectureGraphXmlSnapshotLoader.Load(path);
			var control = CreateControl(snapshot);

			control.Select(ArchitectureGraphSelection.ForLayer(snapshot.Layers.Single().EditHandle));
			DrainDispatcher();

			FindVisualDescendants<TextBox>(control).Should().NotContain(textBox => string.Equals(textBox.Text, "endsWith=", StringComparison.Ordinal));
			FindVisualDescendants<ComboBox>(control)
				.Where(comboBox => comboBox.Items.Cast<object>().Any(item => string.Equals(item.ToString(), "endsWith", StringComparison.Ordinal)))
				.Should()
				.Contain(comboBox => comboBox.Items.Cast<object>().Any(item => string.Equals(item.ToString(), "typeKind", StringComparison.Ordinal)));
		});
	}

	[Fact]
	public void MatcherAddEditor_UsesAttributeDropdownForInlineAssemblyMetadata()
	{
		RunOnStaThread(() =>
		{
			var path = WriteInlineConfigurationFile(
				"""
				<ArchitecturalLevels>
				  <Layer name="Customer"><Class typeName="CustomerType" /></Layer>
				</ArchitecturalLevels>
				""");
			var snapshot = LoadInlineSnapshot(path);
			var control = CreateControl(snapshot);

			control.Select(ArchitectureGraphSelection.ForLayer(snapshot.Layers.Single().EditHandle));
			DrainDispatcher();

			FindVisualDescendants<TextBox>(control).Should().NotContain(textBox => string.Equals(textBox.Text, "endsWith=", StringComparison.Ordinal));
			FindVisualDescendants<ComboBox>(control)
				.Where(comboBox => comboBox.Items.Cast<object>().Any(item => string.Equals(item.ToString(), "endsWith", StringComparison.Ordinal)))
				.Should()
				.Contain(comboBox => comboBox.Items.Cast<object>().Any(item => string.Equals(item.ToString(), "typeKind", StringComparison.Ordinal)));
		});
	}

	[Fact]
	public void GraphLayoutRefresh_PreservesMovedNodePositionAndWritesUserSettings()
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
				_ => ArchitectureGraphXmlSnapshotLoader.Load(path));
			var customer = FindGraphItemDataContext(control, "Customer");
			var movedLocation = new Point(333, 177);

			SetObjectProperty(customer, "Location", movedLocation);
			control.UpdateSnapshot(ArchitectureGraphXmlSnapshotLoader.Load(path), ArchitectureGraphFocusMode.ShowAll);
			DrainDispatcher();

			GetObjectProperty(FindGraphItemDataContext(control, "Customer"), "Location").Should().Be(movedLocation);
			File.Exists(path + ".usersettings").Should().BeTrue();
			File.ReadAllText(path + ".usersettings").Should().Contain("path=\"Customer\"");
		});
	}

	[Fact]
	public void GraphLayoutRefresh_PreservesMovedInlineNodePositionAndWritesUserSettings()
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
				_ => LoadInlineSnapshot(path));
			var customer = FindGraphItemDataContext(control, "Customer");
			var movedLocation = new Point(333, 177);

			SetObjectProperty(customer, "Location", movedLocation);
			control.UpdateSnapshot(LoadInlineSnapshot(path), ArchitectureGraphFocusMode.ShowAll);
			DrainDispatcher();

			GetObjectProperty(FindGraphItemDataContext(control, "Customer"), "Location").Should().Be(movedLocation);
			File.Exists(path + ".usersettings").Should().BeTrue();
			File.ReadAllText(path + ".usersettings").Should().Contain("path=\"Customer\"");
		});
	}

	[Fact]
	public void GraphLayoutUserSettings_RehydrateMovedNodePositionInNewEditor()
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
			var firstControl = CreateControl(ArchitectureGraphXmlSnapshotLoader.Load(path));
			var movedLocation = new Point(410, 208);

			SetObjectProperty(FindGraphItemDataContext(firstControl, "Customer"), "Location", movedLocation);
			firstControl.UpdateSnapshot(ArchitectureGraphXmlSnapshotLoader.Load(path), ArchitectureGraphFocusMode.ShowAll);
			DrainDispatcher();
			var secondControl = CreateControl(ArchitectureGraphXmlSnapshotLoader.Load(path));

			GetObjectProperty(FindGraphItemDataContext(secondControl, "Customer"), "Location").Should().Be(movedLocation);
		});
	}

	[Fact]
	public void GraphLayoutUserSettings_RehydrateMovedInlineNodePositionInNewEditor()
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
			var firstControl = CreateControl(LoadInlineSnapshot(path));
			var movedLocation = new Point(410, 208);

			SetObjectProperty(FindGraphItemDataContext(firstControl, "Customer"), "Location", movedLocation);
			firstControl.UpdateSnapshot(LoadInlineSnapshot(path), ArchitectureGraphFocusMode.ShowAll);
			DrainDispatcher();
			var secondControl = CreateControl(LoadInlineSnapshot(path));

			GetObjectProperty(FindGraphItemDataContext(secondControl, "Customer"), "Location").Should().Be(movedLocation);
		});
	}

	[Fact]
	public void GraphGroupCollapse_PersistsAcrossRefreshAndUserSettings()
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
				_ => ArchitectureGraphXmlSnapshotLoader.Load(path));
			var group = FindExpanderByHeader(control, "Graph 1: Customer, Waiter");

			group.IsExpanded = false;
			DrainDispatcher();
			control.UpdateSnapshot(ArchitectureGraphXmlSnapshotLoader.Load(path), ArchitectureGraphFocusMode.ShowAll);
			DrainDispatcher();

			FindExpanderByHeader(control, "Graph 1: Customer, Waiter").IsExpanded.Should().BeFalse();
			File.ReadAllText(path + ".usersettings").Should().Contain("collapsed=\"true\"");
		});
	}

	[Fact]
	public void GraphGroupCollapse_PersistsAcrossInlineRefreshAndUserSettings()
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
				_ => LoadInlineSnapshot(path));
			var group = FindExpanderByHeader(control, "Graph 1: Customer, Waiter");

			group.IsExpanded = false;
			DrainDispatcher();
			control.UpdateSnapshot(LoadInlineSnapshot(path), ArchitectureGraphFocusMode.ShowAll);
			DrainDispatcher();

			FindExpanderByHeader(control, "Graph 1: Customer, Waiter").IsExpanded.Should().BeFalse();
			File.ReadAllText(path + ".usersettings").Should().Contain("collapsed=\"true\"");
		});
	}

	[Fact]
	public void GraphGroupUserSettings_RehydrateSavedGraphHeight()
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
			File.WriteAllText(
				path + ".usersettings",
				"""
				<AnaalIJzerGraphUserSettings version="1">
				  <GraphLayout />
				  <GraphGroups>
				    <Group key="Graph 1: Customer, Waiter" height="390" />
				  </GraphGroups>
				</AnaalIJzerGraphUserSettings>
				""");
			var control = CreateControl(ArchitectureGraphXmlSnapshotLoader.Load(path));

			FindVisualDescendant<ArchitectureGraphCanvas>(control)!.Height.Should().Be(390);
		});
	}

	[Fact]
	public void GraphGroupUserSettings_RehydrateSavedInlineGraphHeight()
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
			File.WriteAllText(
				path + ".usersettings",
				"""
				<AnaalIJzerGraphUserSettings version="1">
				  <GraphLayout />
				  <GraphGroups>
				    <Group key="Graph 1: Customer, Waiter" height="390" />
				  </GraphGroups>
				</AnaalIJzerGraphUserSettings>
				""");
			var control = CreateControl(LoadInlineSnapshot(path));

			FindVisualDescendant<ArchitectureGraphCanvas>(control)!.Height.Should().Be(390);
		});
	}
}
