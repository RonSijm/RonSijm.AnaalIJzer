using System.IO;
using System.Windows.Controls;
using AwesomeAssertions;
using RonSijm.AnaalIJzer.GraphApplication.Selection;
using RonSijm.AnaalIJzer.GraphModel.Loading;
using Xunit;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Tests.Controls;

public sealed partial class ArchitectureGraphEditorControlPersistenceTests
{
	[Fact]
	public void VisibilityPolicyCheckbox_PersistsXmlAndReloadsInspector()
	{
		RunOnStaThread(() =>
		{
			var path = WriteTempFile(
				"Architecture.anl",
				"""
				<ArchitecturalLevels>
				  <Layer name="QuerySurface">
				    <Class endsWith="Queryable" />
				    <VisibilityPolicy targets="Type" allowedAccessibilities="Internal, File" />
				  </Layer>
				</ArchitecturalLevels>
				""");
			var snapshot = ArchitectureGraphXmlSnapshotLoader.Load(path);
			var control = CreateControl(snapshot, _ => ArchitectureGraphXmlSnapshotLoader.Load(path));
			control.Select(ArchitectureGraphSelection.ForLayer(snapshot.Layers.Single().EditHandle));
			var policy = FindExpanderByHeader(control, "<VisibilityPolicy allowedAccessibilities=\"Internal, File\" targets=\"Type\" />");
			policy.IsExpanded = true;
			DrainDispatcher();

			var publicAccessibility = FindCheckBoxByContent(control, "Public");
			publicAccessibility.IsChecked = true;
			DrainDispatcher();

			File.ReadAllText(path).Should().Contain("allowedAccessibilities=\"Public, Internal, File\"");
			var reloadedSnapshot = ArchitectureGraphXmlSnapshotLoader.Load(path);
			control.Select(ArchitectureGraphSelection.ForLayer(reloadedSnapshot.Layers.Single().EditHandle));
			var reloadedPolicy = FindVisualDescendants<Expander>(control).Single(expander => GetText(expander.Header)?.StartsWith("<VisibilityPolicy", StringComparison.Ordinal) == true);
			GetText(reloadedPolicy.Header).Should().Contain("Public, Internal, File");
		});
	}

	[Fact]
	public void VisibilityPolicyCheckbox_PersistsInlineMetadataAndPreservesInterpolation()
	{
		RunOnStaThread(() =>
		{
			var path = WriteInterpolatedInlineConfigurationFile(
				"""
				<ArchitecturalLevels>
				  <Layer name="{nameof(LollyQueryable)}">
				    <Class typeName="{nameof(LollyQueryable)}" />
				    <VisibilityPolicy targets="Type" allowedAccessibilities="Internal" />
				  </Layer>
				</ArchitecturalLevels>
				""",
				"internal class LollyQueryable { }");
			var snapshot = LoadInlineSnapshot(path);
			var control = CreateControl(snapshot, _ => LoadInlineSnapshot(path));
			control.Select(ArchitectureGraphSelection.ForLayer(snapshot.Layers.Single().EditHandle));
			var policy = FindExpanderByHeader(control, "<VisibilityPolicy allowedAccessibilities=\"Internal\" targets=\"Type\" />");
			policy.IsExpanded = true;
			DrainDispatcher();

			FindCheckBoxByContent(control, "File").IsChecked = true;
			DrainDispatcher();

			var content = File.ReadAllText(path);
			content.Should().Contain("allowedAccessibilities=\"Internal, File\"");
			content.Should().Contain("{nameof(LollyQueryable)}");
		});
	}
}
