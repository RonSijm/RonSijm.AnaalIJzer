using System.Globalization;
using System.Windows.Controls;
using AwesomeAssertions;
using RonSijm.AnaalIJzer.Graphing.Loading;
using RonSijm.AnaalIJzer.GraphApplication.Selection;
using Xunit;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Tests.Controls;

public sealed partial class ArchitectureGraphEditorControlPersistenceTests
{
	[Fact]
	public void LayerInspector_ShowsExceptionMatchersAndReviews()
	{
		RunOnStaThread(() =>
		{
			var path = WriteTempFile(
				"Architecture.anl",
				"""
				<ArchitecturalLevels>
				  <ExceptionPolicy requireReason="true" />
				  <Layer name="Kitchen">
				    <Class endsWith="Kitchen">
				      <Exceptions>
				        <Class typeName="OutdoorKitchen" />
				      </Exceptions>
				    </Class>
				  </Layer>
				</ArchitecturalLevels>
				""");
			var snapshot = ArchitectureGraphXmlSnapshotLoader.Load(path);
			var control = CreateControl(snapshot);

			control.Select(ArchitectureGraphSelection.ForLayer(snapshot.Layers.Single().EditHandle));
			DrainDispatcher();

			GetVisualText(control).Should().Contain("Exception matchers");
			GetVisualText(control).Should().Contain("OutdoorKitchen");
			GetVisualText(control).Should().Contain("Exception reviews");
			FindExpanderByHeader(control, "[Invalid] Class typeName=\"OutdoorKitchen\"");
		});
	}

	[Fact]
	public void RootInspector_ExceptionReviewFiltersHideEntriesWithoutClearingTheSection()
	{
		RunOnStaThread(() =>
		{
			var soonDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
			var path = WriteTempFile(
				"Architecture.anl",
				$"""
				<ArchitecturalLevels>
				  <ExceptionPolicy requireOwner="true" requireExpiresOn="true" />
				  <Layer name="Kitchen">
				    <Class endsWith="Kitchen">
				      <Exceptions>
				        <Class typeName="OutdoorKitchen" expiresOn="{soonDate}" />
				        <Class typeName="ExpiredKitchen" owner="Kitchen team" expiresOn="{soonDate}" />
				      </Exceptions>
				    </Class>
				  </Layer>
				</ArchitecturalLevels>
				""");
			var snapshot = ArchitectureGraphXmlSnapshotLoader.Load(path);
			var control = CreateControl(snapshot);

			FindVisualDescendants<Expander>(control).Select(expander => expander.Header?.ToString()).Should().Contain([
				"[Invalid] Class typeName=\"OutdoorKitchen\"",
				"[Expired] Class typeName=\"ExpiredKitchen\""]);

			FindCheckBoxByContent(control, "Invalid").IsChecked = false;
			FindCheckBoxByContent(control, "Expired").IsChecked = false;
			DrainDispatcher();

			FindVisualDescendants<Expander>(control).Select(expander => expander.Header?.ToString()).Should().NotContain([
				"[Invalid] Class typeName=\"OutdoorKitchen\"",
				"[Expired] Class typeName=\"ExpiredKitchen\""]);
			GetVisualText(control).Should().Contain("No exception reviews match the current filter.");
		});
	}
}
