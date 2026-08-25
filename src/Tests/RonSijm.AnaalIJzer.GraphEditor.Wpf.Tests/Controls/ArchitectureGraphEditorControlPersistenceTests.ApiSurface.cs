using System.IO;
using System.Windows;
using AwesomeAssertions;
using RonSijm.AnaalIJzer.Graphing.Loading;
using RonSijm.AnaalIJzer.GraphApplication.Selection;
using Xunit;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Tests.Controls;

public sealed partial class ArchitectureGraphEditorControlPersistenceTests
{
	[Fact]
	public void ApiSurfacePath_PersistsXmlAndReloadsInspector()
	{
		RunOnStaThread(() =>
		{
			var path = WriteTempFile(
				"Architecture.anl",
				"""
				<ArchitecturalLevels>
				  <Layer name="Application">
				    <Class endsWith="Service" />
				    <ApiSurface>
				      <BlockedLayer path="/QuerySurface" />
				    </ApiSurface>
				  </Layer>
				  <Layer name="QuerySurface"><Class endsWith="Queryable" /></Layer>
				  <Layer name="Contracts"><Class endsWith="Projection" /></Layer>
				</ArchitecturalLevels>
				""");
			var snapshot = ArchitectureGraphXmlSnapshotLoader.Load(path);
			var control = CreateControl(snapshot, _ => ArchitectureGraphXmlSnapshotLoader.Load(path));
			var application = snapshot.Layers.Single(layer => layer.Path == "Application");
			control.Select(ArchitectureGraphSelection.ForLayer(application.EditHandle));
			var policy = FindVisualDescendants<System.Windows.Controls.Expander>(control).Single(expander => expander.Header?.ToString()?.StartsWith("<ApiSurface", StringComparison.Ordinal) == true);
			policy.IsExpanded = true;
			DrainDispatcher();

			var layerPath = FindTextBoxByText(control, "/QuerySurface");
			layerPath.Text = "/Contracts";
			layerPath.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
			DrainDispatcher();

			File.ReadAllText(path).Should().Contain("<BlockedLayer path=\"/Contracts\" />");
		});
	}

	[Fact]
	public void ApiSurfaceRecognitionCheckbox_PersistsInlineMetadataAndPreservesInterpolation()
	{
		RunOnStaThread(() =>
		{
			var path = WriteInterpolatedInlineConfigurationFile(
				"""
				<ArchitecturalLevels>
				  <Layer name="{nameof(CandyService)}">
				    <Class typeName="{nameof(CandyService)}" />
				    <ApiSurface>
				      <BlockedLayer path="/LollyQueryable" />
				    </ApiSurface>
				  </Layer>
				  <Layer name="{nameof(LollyQueryable)}">
				    <Class typeName="{nameof(LollyQueryable)}" />
				  </Layer>
				</ArchitecturalLevels>
				""",
				"public class CandyService { } public class LollyQueryable { }");
			var snapshot = LoadInlineSnapshot(path);
			var control = CreateControl(snapshot, _ => LoadInlineSnapshot(path));
			var application = snapshot.Layers.Single(layer => layer.Path == "CandyService");
			control.Select(ArchitectureGraphSelection.ForLayer(application.EditHandle));
			var policy = FindVisualDescendants<System.Windows.Controls.Expander>(control).Single(expander => expander.Header?.ToString()?.StartsWith("<ApiSurface", StringComparison.Ordinal) == true);
			policy.IsExpanded = true;
			DrainDispatcher();

			FindCheckBoxByContent(control, "Require exposed types to belong to a configured layer").IsChecked = true;
			DrainDispatcher();

			var content = File.ReadAllText(path);
			content.Should().Contain("requireRecognizedTypes=\"true\"");
			content.Should().Contain("{nameof(CandyService)}");
			content.Should().Contain("{nameof(LollyQueryable)}");
		});
	}

	[Fact]
	public void TransitiveExposureControls_AutoSaveInlineMetadataAndPreserveInterpolation()
	{
		RunOnStaThread(() =>
		{
			var path = WriteInterpolatedInlineConfigurationFile(
				"""
				<ArchitecturalLevels>
				  <Layer name="{nameof(CandyService)}">
				    <Class typeName="{nameof(CandyService)}" />
				    <ApiSurface>
				      <TransitiveExposure maxDepth="3" />
				      <BlockedLayer path="/LollyQueryable" />
				    </ApiSurface>
				  </Layer>
				  <Layer name="{nameof(LollyQueryable)}">
				    <Class typeName="{nameof(LollyQueryable)}" />
				  </Layer>
				</ArchitecturalLevels>
				""",
				"public class CandyService { } public class LollyQueryable { }");
			var snapshot = LoadInlineSnapshot(path);
			var control = CreateControl(snapshot, _ => LoadInlineSnapshot(path));
			var application = snapshot.Layers.Single(layer => layer.Path == "CandyService");
			control.Select(ArchitectureGraphSelection.ForLayer(application.EditHandle));
			var policy = FindVisualDescendants<System.Windows.Controls.Expander>(control).Single(expander => expander.Header?.ToString()?.StartsWith("<ApiSurface", StringComparison.Ordinal) == true);
			policy.IsExpanded = true;
			DrainDispatcher();

			FindCheckBoxByContent(control, "Inspect the public object graph of exposed types").IsChecked.Should().BeTrue();
			var depth = FindTextBoxByText(control, "3");
			depth.Text = "5";
			depth.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
			DrainDispatcher();

			var content = File.ReadAllText(path);
			content.Should().Contain("<TransitiveExposure maxDepth=\"5\" />");
			content.Should().Contain("{nameof(CandyService)}");
			content.Should().Contain("{nameof(LollyQueryable)}");
		});
	}
}
