using RonSijm.AnaalIJzer.Core.Editor.QuickInfo;

namespace RonSijm.AnaalIJzer.EditorRuntime.Tests.Editor.Snapshots;

public sealed partial class EditorSnapshotTests
{
	[Fact]
	public async Task Snapshot_ExposesVisibilityPolicyEvidenceForQuickInfo()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="QuerySurface">
			    <Class endsWith="Queryable" />
			    <VisibilityPolicy targets="Type, Property" allowedAccessibilities="Internal, Private" description="Keep query state private." />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public class LollyQueryable
			{
				public string CurrentQuery { get; } = "";
			}
			""";

		var snapshot = await CreateSnapshotAsync(source, config);
		var indicators = snapshot.VisibilityPolicyIndicators;

		indicators.Should().HaveCount(2);
		indicators.Select(indicator => indicator.DeclarationTarget).Should().BeEquivalentTo("Type", "Property");
		indicators.Should().OnlyContain(indicator => indicator.DeclaredAccessibility == "Public");
		indicators.Should().OnlyContain(indicator => indicator.LayerPath == "QuerySurface");
		indicators.Should().OnlyContain(indicator => indicator.DiagnosticId == ArchitecturalDiagnosticIds.VisibilityPolicyViolation);
		var content = ArchitectureQuickInfoContentBuilder.CreateVisibilityPolicyContent(indicators[0]);
		content.ToString().Should().Contain("Diagnostic: ARCH012");
		content.ToString().Should().Contain("Keep query state private");
	}

	[Fact]
	public async Task Snapshot_ReportsEffectiveVisibilityThroughContainingTypes()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Policy">
			    <Assembly exactName="TestProject" />
			    <VisibilityPolicy targets="NestedType" blockedAccessibilities="Public" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			internal class Container
			{
				public class PublicNested { }
			}
			""";

		var snapshot = await CreateSnapshotAsync(source, config);
		var indicator = snapshot.VisibilityPolicyIndicators.Should().ContainSingle().Which;

		indicator.DeclaredAccessibility.Should().Be("Public");
		indicator.IsEffectivelyExternallyVisible.Should().BeFalse();
	}
}
