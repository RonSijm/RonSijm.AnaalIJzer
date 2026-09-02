using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using AwesomeAssertions;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.GraphApplication.Selection;
using RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;
using Xunit;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Tests.Controls;

public sealed partial class ArchitectureGraphEditorControlPersistenceTests
{
	[Fact]
	public void ConfigurationFixes_FilterToSelectedDependency()
	{
		RunOnStaThread(() =>
		{
			var path = WriteTempFile(
				"Architecture.anl",
				"""
				<ArchitecturalLevels>
				  <Layer name="Customer"><Class typeName="CustomerType" /></Layer>
				  <Layer name="Waiter"><Class typeName="WaiterType" /></Layer>
				</ArchitecturalLevels>
				""");
			var snapshot = CreateSnapshot(path, ArchitectureConfigurationSourceKind.XmlFile);
			var control = CreateControl(
				snapshot,
				configurationFixLoader: _ => Task.FromResult(
					new ArchitectureGraphConfigurationFixCollection(
						"Loaded 2 configuration fix proposal(s).",
						[
							new ArchitectureGraphConfigurationFixProposal(
								"fix-customer",
								"Add allowed dependency 'Customer' -> 'Waiter'",
								"Adds the missing dependency rule.",
								"Guided",
								"ARCH001",
								path,
								"+ <AllowedDependency from=\"Customer\" to=\"Waiter\" />",
								ImmutableDictionary<string, string>.Empty
									.Add("CallerLayerName", "Customer")
									.Add("DepLayerName", "Waiter")),
							new ArchitectureGraphConfigurationFixProposal(
								"fix-chef",
								"Add allowed dependency 'Chef' -> 'Pantry'",
								"Adds an unrelated dependency rule.",
								"Guided",
								"ARCH001",
								path,
								"+ <AllowedDependency from=\"Chef\" to=\"Pantry\" />",
								ImmutableDictionary<string, string>.Empty
									.Add("CallerLayerName", "Chef")
									.Add("DepLayerName", "Pantry"))
						])));

			var dependencyRule = snapshot.Rules.Single();
			control.Select(ArchitectureGraphSelection.ForDependency(dependencyRule.EditHandle));
			DrainDispatcher();

			var loadButton = FindButtonByContent(control, "Find fixes for this dependency");
			loadButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			DrainDispatcher();
			DrainDispatcher();

			var proposalBox = FindVisualDescendants<ComboBox>(control)
				.Single(comboBox => string.Equals(comboBox.SelectedValuePath, nameof(ArchitectureGraphConfigurationFixProposal.Id), StringComparison.Ordinal));
			proposalBox.Items.Count.Should().Be(1);
			proposalBox.SelectedValue.Should().Be("fix-customer");
			FindVisualDescendants<TextBlock>(control)
				.Select(textBlock => textBlock.Text)
				.Should()
				.Contain("Showing 1 of 2 loaded configuration fix proposal(s) for this selection.");
		});
	}
}
