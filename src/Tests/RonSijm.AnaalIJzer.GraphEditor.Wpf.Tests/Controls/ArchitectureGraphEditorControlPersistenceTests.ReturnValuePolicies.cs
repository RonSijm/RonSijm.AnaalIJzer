using System.IO;
using System.Windows;
using System.Windows.Controls;
using AwesomeAssertions;
using RonSijm.AnaalIJzer.GraphApplication.Selection;
using RonSijm.AnaalIJzer.GraphModel.Loading;
using Xunit;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Tests.Controls;

public sealed partial class ArchitectureGraphEditorControlPersistenceTests
{
	[Fact]
	public void ReturnValuePolicyMatcherXml_PersistsXmlAndReloadsInspector()
	{
		RunOnStaThread(() =>
		{
			var path = WriteTempFile(
				"Architecture.anl",
				"""
				<ArchitecturalLevels>
				  <Layer name="Kitchen">
				    <Class endsWith="Kitchen" />
				    <ReturnValuePolicy>
				      <Literal value="null" />
				    </ReturnValuePolicy>
				  </Layer>
				</ArchitecturalLevels>
				""");
			var snapshot = ArchitectureGraphXmlSnapshotLoader.Load(path);
			var control = CreateControl(snapshot, _ => ArchitectureGraphXmlSnapshotLoader.Load(path));
			control.Select(ArchitectureGraphSelection.ForLayer(snapshot.Layers.Single().EditHandle));
			var policy = FindVisualDescendants<Expander>(control).Single(expander => GetText(expander.Header)?.StartsWith("<ReturnValuePolicy", StringComparison.Ordinal) == true);
			policy.IsExpanded = true;
			DrainDispatcher();

			var matcherXml = FindTextBoxByText(control, "<Literal value=\"null\" />");
			matcherXml.Text = "<Literal value=\"\" />\n<Literal value=\"42\" />";
			matcherXml.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
			DrainDispatcher();

			var content = File.ReadAllText(path);
			content.Should().Contain("<Literal value=\"\" />");
			content.Should().Contain("<Literal value=\"42\" />");
			var reloadedSnapshot = ArchitectureGraphXmlSnapshotLoader.Load(path);
			control.Select(ArchitectureGraphSelection.ForLayer(reloadedSnapshot.Layers.Single().EditHandle));
			var hasReloadedPolicy = FindVisualDescendants<Expander>(control).Any(expander =>
			{
				var header = GetText(expander.Header);
				var result = header is not null && header.StartsWith("<ReturnValuePolicy", StringComparison.Ordinal);

				return result;
			});

			hasReloadedPolicy.Should().BeTrue();
		});
	}
}
