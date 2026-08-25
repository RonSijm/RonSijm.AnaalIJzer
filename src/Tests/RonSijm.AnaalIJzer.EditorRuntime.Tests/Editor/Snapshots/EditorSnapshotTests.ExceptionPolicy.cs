using RonSijm.AnaalIJzer.Core.Editor.QuickInfo;

namespace RonSijm.AnaalIJzer.EditorRuntime.Tests.Editor.Snapshots;

public sealed partial class EditorSnapshotTests
{
	[Fact]
	public async Task QuickInfoContentBuilder_IncludesLayerExceptionReviewSummaries()
	{
		const string source = "public class PizzaKitchen { }";
		const string config = """
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
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);
		var indicator = snapshot.LayerIndicators.Should().ContainSingle().Which;
		var content = ArchitectureQuickInfoContentBuilder.CreateLayerContent(indicator);

		indicator.ExceptionReviewCount.Should().Be(1);
		indicator.ExceptionReviewSummaries.Should().ContainSingle().Which.Should().Be("[Invalid] Class typeName=\"OutdoorKitchen\"");
		content.Lines.Should().Contain("Exception reviews: 1");
		content.Lines.Should().Contain("  - [Invalid] Class typeName=\"OutdoorKitchen\"");
	}
}
