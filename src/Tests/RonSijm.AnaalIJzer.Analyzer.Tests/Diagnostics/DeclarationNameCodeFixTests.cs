using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

public sealed class DeclarationNameCodeFixTests
{
	[Fact]
	public async Task DeclarationNameMismatch_RenamesDeclarationToTypeName()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="AspEndpoints">
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
			sealed class PatientId : IHonestType { }

			class PatientController
			{
				public void GetPatient(PatientId patient) { }
			}
			""";

		var newSource = await AnalyzerTestHelper.ApplyCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.NameRuleViolation,
			"Rename 'patient' to 'PatientId'");

		newSource.Should().Contain("GetPatient(PatientId PatientId)");
	}

	[Fact]
	public async Task RequireMatchingNames_DoesNotOfferDeclarationRenameFix()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <NameRules>
			      <RequireMatchingNames allowedSites="Method">
			        <Source />
			        <Target />
			      </RequireMatchingNames>
			    </NameRules>
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			class OrderService
			{
				public void Run(int fruitId, int animalId)
				{
					Log(animalId, fruitId);
				}

				private void Log(int fruitId, int animalId)
				{
				}
			}
			""";

		var titles = await AnalyzerTestHelper.GetCodeFixTitlesAsync(source, config, ArchitecturalDiagnosticIds.NameRuleViolation);

		titles.Should().NotContain(title => title.StartsWith("Rename '", StringComparison.Ordinal));
	}
}
