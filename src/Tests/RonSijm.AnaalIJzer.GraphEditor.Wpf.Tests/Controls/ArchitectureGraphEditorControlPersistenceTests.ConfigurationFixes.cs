using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using AwesomeAssertions;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;
using Xunit;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Tests.Controls;

public sealed partial class ArchitectureGraphEditorControlPersistenceTests
{
	[Fact]
	public void ConfigurationFixes_CanBeLoadedPreviewedAndApplied()
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
			var reloadedSnapshots = 0;
			var appliedFixId = string.Empty;
			var control = CreateControl(
				snapshot,
				snapshotReloader: current =>
				{
					reloadedSnapshots++;
					return current;
				},
				configurationFixLoader: _ => Task.FromResult(
					new ArchitectureGraphConfigurationFixCollection(
						"Found 1 configuration fix proposal.",
						[
							new ArchitectureGraphConfigurationFixProposal(
								"fix-1",
								"Add allowed dependency 'Customer' -> 'Waiter'",
								"Adds the missing dependency rule.",
								"Guided",
								"ARCH001",
								path,
								"+ <AllowedDependency from=\"Customer\" to=\"Waiter\" />")
						])),
				configurationFixApplier: (fixId, _) =>
				{
					appliedFixId = fixId;
					return Task.FromResult(new ArchitectureGraphConfigurationFixApplyResult("Applied fix-1"));
				});

			var loadButton = FindButtonByContent(control, "Find config fixes");
			loadButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			DrainDispatcher();
			DrainDispatcher();

			var comboBox = FindVisualDescendant<ComboBox>(control);
			comboBox.Items.Count.Should().Be(1);
			comboBox.SelectedValue.Should().Be("fix-1");
			FindVisualDescendants<TextBox>(control).Select(textBox => textBox.Text).Should().Contain("+ <AllowedDependency from=\"Customer\" to=\"Waiter\" />");

			var applyButton = FindButtonByContent(control, "Apply selected fix");
			applyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			DrainDispatcher();
			DrainDispatcher();

			appliedFixId.Should().Be("fix-1");
			reloadedSnapshots.Should().BeGreaterThan(0);
			FindVisualDescendants<TextBlock>(control).Select(text => text.Text).Should().Contain("Found 1 configuration fix proposal.");
		});
	}
}
