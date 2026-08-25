using RonSijm.AnaalIJzer.Testing;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis;

public sealed class DeclarationNameMatchesTypeTests
{
	private const string AllSitesConfig = """
		<ArchitecturalLevels>
		  <Layer name="Application">
		    <Class endsWith="Service" />
		    <NameRules>
		      <RequireDeclarationNameMatchesType allowedSites="Constructor, Method, MethodReturn, Field, Property, Local">
		        <Type implements="IHonestType" />
		      </RequireDeclarationNameMatchesType>
		    </NameRules>
		  </Layer>
		</ArchitecturalLevels>
		""";

	[Fact]
	public async Task MatchingDeclarationNames_AreAllowedAtEverySupportedSite()
	{
		const string source = """
			interface IHonestType { }
			sealed class PatientId : IHonestType { }

			class PatientService(PatientId patientId)
			{
				private PatientId _patientId = patientId;
				public PatientId PatientId { get; } = patientId;

				public void Load(PatientId patientId) { }

				public PatientId GetPatientId()
				{
					PatientId patientId = _patientId;
					return patientId;
				}
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, AllSitesConfig);

		diagnostics.Where(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.NameRuleViolation).Should().BeEmpty();
	}

	[Fact]
	public async Task MismatchedDeclarationNames_ReportAtEverySupportedSite()
	{
		const string source = """
			interface IHonestType { }
			sealed class PatientId : IHonestType { }
			sealed class DoctorId : IHonestType { }

			class PatientService(DoctorId patientId)
			{
				private DoctorId _patientId;
				public DoctorId PatientId { get; }

				public void Load(DoctorId patientId) { }

				public DoctorId GetPatientId()
				{
					DoctorId patientId = _patientId;
					return patientId;
				}
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, AllSitesConfig);
		var nameDiagnostics = diagnostics.Where(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.NameRuleViolation).ToArray();

		nameDiagnostics.Should().HaveCount(6);
		nameDiagnostics.Select(diagnostic => diagnostic.Properties["Site"]).Should().BeEquivalentTo("Constructor", "Method", "MethodReturn", "Field", "Property", "Local");
		nameDiagnostics.Should().OnlyContain(diagnostic => diagnostic.Properties["NameRuleKind"] == "RequireDeclarationNameMatchesType");
		nameDiagnostics.Should().OnlyContain(diagnostic => diagnostic.Properties["TypeName"] == "DoctorId");
	}

	[Fact]
	public async Task SwappedMethodParameters_ReportBothDeclarationNames()
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
			sealed class DoctorId : IHonestType { }

			class PatientController
			{
				public void GetPatient(DoctorId patientId, PatientId doctorId) { }
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);
		var nameDiagnostics = diagnostics.Where(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.NameRuleViolation).ToArray();

		nameDiagnostics.Should().HaveCount(2);
		nameDiagnostics.Select(diagnostic => diagnostic.Properties["DeclaredName"]).Should().BeEquivalentTo("patientId", "doctorId");
		nameDiagnostics.Select(diagnostic => diagnostic.Properties["TypeName"]).Should().BeEquivalentTo("DoctorId", "PatientId");
		nameDiagnostics.Should().OnlyContain(diagnostic => diagnostic.GetMessage().Contains("violates name rule 'RequireDeclarationNameMatchesType' at Method", StringComparison.Ordinal));
	}

	[Fact]
	public async Task TypeAndNameMatchers_AreConjunctiveWithinElementsAndAlternativesBetweenElements()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <NameRules>
			      <RequireDeclarationNameMatchesType allowedSites="Property">
			        <Type endsWith="Id" typeKind="Class" />
			        <Type exactName="FallbackValue" />
			        <Name endsWith="Id" />
			      </RequireDeclarationNameMatchesType>
			    </NameRules>
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			sealed class DoctorId { }
			struct FallbackValue { }
			struct PatientId { }

			class PatientService
			{
				public DoctorId PatientId { get; set; }
			}

			class FallbackService
			{
				public FallbackValue PatientId { get; set; }
			}

			class UnselectedService
			{
				public PatientId DoctorId { get; set; }
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Where(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.NameRuleViolation).Should().HaveCount(2);
	}

	[Fact]
	public async Task AllowMapping_CanPermitIntentionalTypeToNameTranslation()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <NameRules>
			      <RequireDeclarationNameMatchesType allowedSites="Property">
			        <Type endsWith="Identifier" />
			        <Allow from="LegacyPatientIdentifier" to="PatientId" />
			      </RequireDeclarationNameMatchesType>
			    </NameRules>
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			sealed class LegacyPatientIdentifier { }

			class PatientService
			{
				public LegacyPatientIdentifier PatientId { get; set; }
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Where(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.NameRuleViolation).Should().BeEmpty();
	}

	[Fact]
	public async Task AllowMapping_WithWrongSite_ExplainsSiteDenial()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <NameRules>
			      <RequireDeclarationNameMatchesType allowedSites="Property">
			        <Type endsWith="Identifier" />
			        <Allow from="LegacyPatientIdentifier" to="PatientId" allowedSites="Method" />
			      </RequireDeclarationNameMatchesType>
			    </NameRules>
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			sealed class LegacyPatientIdentifier { }

			class PatientService
			{
				public LegacyPatientIdentifier PatientId { get; set; }
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);
		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.NameRuleViolation).Which;

		diagnostic.GetMessage().Should().Contain("allowedSites does not include Property");
	}

	[Fact]
	public async Task DeclarationRule_DoesNotRunForValueMovement()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <NameRules>
			      <RequireDeclarationNameMatchesType allowedSites="Local">
			        <Type typeKind="Struct" />
			      </RequireDeclarationNameMatchesType>
			    </NameRules>
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			readonly struct PatientId { }
			readonly struct DoctorId { }

			class OrderService
			{
				public void Run(DoctorId doctorId)
				{
					PatientId patientId = doctorId;
				}
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Where(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.NameRuleViolation).Should().BeEmpty();
	}

	[Fact]
	public async Task ParentLayerRule_AppliesToDescendantLayer()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop" />
			    <NameRules>
			      <RequireDeclarationNameMatchesType allowedSites="Method">
			        <Type endsWith="Id" />
			      </RequireDeclarationNameMatchesType>
			    </NameRules>
			    <Layer name="Endpoints">
			      <Class endsWith="Controller" />
			    </Layer>
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			namespace Shop
			{
				sealed class DoctorId { }

				class PatientController
				{
					public void Get(DoctorId patientId) { }
				}
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.NameRuleViolation);
	}
}
