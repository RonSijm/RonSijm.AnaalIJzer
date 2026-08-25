using RonSijm.AnaalIJzer.Core.Editor.QuickInfo;

namespace RonSijm.AnaalIJzer.EditorRuntime.Tests.Editor.Snapshots;

public sealed partial class EditorSnapshotTests
{
	[Fact]
	public async Task Snapshot_ExposesDeclarationNameRuleEvidenceForQuickInfo()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Endpoints">
			    <Class endsWith="Controller" />
			    <NameRules>
			      <RequireDeclarationNameMatchesType allowedSites="Method">
			        <Type implements="IHonestType" />
			      </RequireDeclarationNameMatchesType>
			    </NameRules>
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			interface IHonestType { }
			sealed class DoctorId : IHonestType { }

			class PatientController
			{
				public void GetPatient(DoctorId patientId) { }
			}
			""";

		var snapshot = await CreateSnapshotAsync(source, config);
		var indicator = snapshot.NameRuleIndicators.Should().ContainSingle().Which;
		var content = ArchitectureQuickInfoContentBuilder.CreateNameRuleContent(indicator);

		indicator.Site.Should().Be("Method");
		indicator.RuleKind.Should().Be("RequireDeclarationNameMatchesType");
		indicator.SourceName.Should().Be("DoctorId");
		indicator.TargetName.Should().Be("patientId");
		content.ToString().Should().Contain("Diagnostic: ARCH008");
	}
}
