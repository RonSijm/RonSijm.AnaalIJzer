using System.Collections.Immutable;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AwesomeAssertions;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.GraphModel.Model;
using Xunit;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Tests.Controls;

public sealed partial class ArchitectureGraphEditorControlPersistenceTests
{
	[Fact]
	public void NoConfigurationCreationButton_CreatesConfigurationAndRegistersDirectoryBuildProps()
	{
		RunOnStaThread(() =>
		{
			var directory = Path.Combine(Path.GetTempPath(), "AnaalIJzerGraphEditorTests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(directory);
			var architecturePath = Path.Combine(directory, "Architecture.anl");
			var propsPath = Path.Combine(directory, "Directory.Build.props");
			var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, architecturePath);
			var target = new ArchitectureConfigurationCreationTarget(
				"Project folder",
				"Create folder settings.",
				source,
				ArchitectureConfigurationRegistrationKind.DirectoryBuildProps,
				propsPath);
			var snapshot = new ArchitectureGraphSnapshot(
				false,
				false,
				ImmutableArray<ArchitectureGraphLayer>.Empty,
				ImmutableArray<ArchitectureGraphRule>.Empty,
				ImmutableArray<string>.Empty,
				ImmutableArray<string>.Empty,
				ArchitectureConfigurationSource.None,
				ArchitectureGraphEvidence.Empty,
                [target]);
			var control = CreateControl(snapshot);
			var createButton = FindVisualDescendants<Button>(control)
				.First(button => GetVisualText(button).Contains("Create in Project folder", StringComparison.Ordinal));

			createButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			DrainDispatcher();

			File.Exists(architecturePath).Should().BeTrue();
			File.ReadAllText(propsPath).Should().Contain("<AdditionalFiles Include=\"Architecture.anl\" />");
		});
	}

	private static string GetVisualText(DependencyObject root)
	{
		if (root is TextBlock textBlock)
		{
			return textBlock.Text;
		}

		var result = string.Concat(Enumerable.Range(0, VisualTreeHelper.GetChildrenCount(root))
			.Select(index => GetVisualText(VisualTreeHelper.GetChild(root, index))));

		return result;
	}
}
