using System.IO;
using System.Windows;
using AwesomeAssertions;
using Nodify;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.Graphing.Wpf.Selection;
using Xunit;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Tests.Controls;

public sealed partial class ArchitectureGraphEditorControlPersistenceTests
{
	[Fact]
	public void TextBoxEdit_PersistsXmlConfiguration()
	{
		RunOnStaThread(() =>
		{
			var path = WriteTempFile(
				"Architecture.anl",
				"""
				<ArchitecturalLevels description="Old root">
				  <Layer name="Customer"><Class typeName="CustomerType" /></Layer>
				  <Layer name="Waiter"><Class typeName="WaiterType" /></Layer>
				  <AllowedDependency from="Customer" to="Waiter" />
				</ArchitecturalLevels>
				""");
			var control = CreateControl(CreateSnapshot(path, ArchitectureConfigurationSourceKind.XmlFile));
			var description = FindTextBoxByText(control, "Old root");

			description.Text = "New root";
			description.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
			DrainDispatcher();

			File.ReadAllText(path).Should().Contain("description=\"New root\"");
		});
	}

	[Fact]
	public void CheckBoxEdit_PersistsXmlConfiguration()
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
			var snapshot = CreateSnapshot(path, ArchitectureConfigurationSourceKind.XmlFile);
			var control = CreateControl(snapshot);
			var rule = snapshot.Rules.Single();
			control.Select(ArchitectureGraphSelection.ForDependency(rule.EditHandle));
			DrainDispatcher();
			var cascade = FindCheckBoxByContent(control, "appliesToDescendants");

			cascade.IsChecked = true;
			DrainDispatcher();

			FindCheckBoxByContent(control, "appliesToDescendants").IsChecked.Should().BeTrue();
			File.ReadAllText(path).Should().Contain("appliesToDescendants=\"true\"");
		});
	}

	[Fact]
	public void SiteCheckBoxEdit_StaysCheckedAndPersistsXmlConfiguration()
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
			var snapshot = CreateSnapshot(path, ArchitectureConfigurationSourceKind.XmlFile);
			var control = CreateControl(snapshot);
			var rule = snapshot.Rules.Single();
			control.Select(ArchitectureGraphSelection.ForDependency(rule.EditHandle));
			DrainDispatcher();
			var constructorSite = FindCheckBoxByContent(control, "Constructor");

			constructorSite.IsChecked = true;
			DrainDispatcher();

			FindCheckBoxByContent(control, "Constructor").IsChecked.Should().BeTrue();
			File.ReadAllText(path).Should().Contain("allowedSites=\"Constructor\"");
		});
	}

	[Fact]
	public void TextBoxEdit_PersistsInlineAssemblyMetadataConfiguration()
	{
		RunOnStaThread(() =>
		{
			var path = WriteTempFile(
				"AnaalIJzerSettings.cs",
				""""
				using System.Reflection;

				[assembly: AssemblyMetadata("AnaalIJzerSettings", """
				<ArchitecturalLevels description="Old inline">
				  <Layer name="Customer"><Class typeName="CustomerType" /></Layer>
				  <Layer name="Waiter"><Class typeName="WaiterType" /></Layer>
				  <AllowedDependency from="Customer" to="Waiter" />
				</ArchitecturalLevels>
				""")]
				"""");
			var control = CreateControl(CreateSnapshot(path, ArchitectureConfigurationSourceKind.InlineAssemblyMetadata));
			var description = FindTextBoxByText(control, "Old inline");

			description.Text = "New inline";
			description.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
			DrainDispatcher();

			File.ReadAllText(path).Should().Contain("description=\"New inline\"");
		});
	}

	[Fact]
	public void GraphEditor_LoadsInlineAssemblyMetadataConfiguration()
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
			var control = CreateControl(LoadInlineSnapshot(path));

			FindVisualDescendants<Node>(control).Select(item => GetDataContextProperty(item, "Path")).Should().Contain(new[] { "Customer", "Waiter" });
			FindVisualDescendants<Connection>(control).Should().ContainSingle();
		});
	}

	[Fact]
	public void CheckBoxEdit_PersistsInlineAssemblyMetadataConfiguration()
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
			var snapshot = LoadInlineSnapshot(path);
			var control = CreateControl(snapshot);
			var rule = snapshot.Rules.Single();
			control.Select(ArchitectureGraphSelection.ForDependency(rule.EditHandle));
			DrainDispatcher();
			var cascade = FindCheckBoxByContent(control, "appliesToDescendants");

			cascade.IsChecked = true;
			DrainDispatcher();

			FindCheckBoxByContent(control, "appliesToDescendants").IsChecked.Should().BeTrue();
			File.ReadAllText(path).Should().Contain("appliesToDescendants=\"true\"");
		});
	}

	[Fact]
	public void SiteCheckBoxEdit_StaysCheckedAndPersistsInlineAssemblyMetadataConfiguration()
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
			var snapshot = LoadInlineSnapshot(path);
			var control = CreateControl(snapshot);
			var rule = snapshot.Rules.Single();
			control.Select(ArchitectureGraphSelection.ForDependency(rule.EditHandle));
			DrainDispatcher();
			var constructorSite = FindCheckBoxByContent(control, "Constructor");

			constructorSite.IsChecked = true;
			DrainDispatcher();

			FindCheckBoxByContent(control, "Constructor").IsChecked.Should().BeTrue();
			File.ReadAllText(path).Should().Contain("allowedSites=\"Constructor\"");
		});
	}

	[Fact]
	public void SiteCheckBoxEdit_StaysCheckedAndPreservesInterpolatedInlineAssemblyMetadataConfiguration()
	{
		RunOnStaThread(() =>
		{
			var path = WriteInterpolatedInlineConfigurationFile(
				"""
				<ArchitecturalLevels>
				  <Layer name="{nameof(Customer)}"><Class typeName="{nameof(Customer)}" /></Layer>
				  <Layer name="{nameof(Waiter)}"><Class typeName="{nameof(Waiter)}" /></Layer>
				  <AllowedDependency from="{nameof(Customer)}" to="{nameof(Waiter)}" />
				</ArchitecturalLevels>
				""",
				"""
				public class Customer { }
				public class Waiter { }
				""");
			var snapshot = LoadInlineSnapshot(path);
			var control = CreateControl(snapshot);
			var rule = snapshot.Rules.Single();
			control.Select(ArchitectureGraphSelection.ForDependency(rule.EditHandle));
			DrainDispatcher();
			var constructorSite = FindCheckBoxByContent(control, "Constructor");

			constructorSite.IsChecked = true;
			DrainDispatcher();

			FindCheckBoxByContent(control, "Constructor").IsChecked.Should().BeTrue();
			var content = File.ReadAllText(path);
			content.Should().Contain("from=\"{nameof(Customer)}\"");
			content.Should().Contain("to=\"{nameof(Waiter)}\"");
			content.Should().Contain("allowedSites=\"Constructor\"");
		});
	}

}
