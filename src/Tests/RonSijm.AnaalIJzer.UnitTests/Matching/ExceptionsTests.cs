using RonSijm.AnaalIJzer.UnitTests.TestSupport;

namespace RonSijm.AnaalIJzer.UnitTests.Matching;

public sealed class ExceptionsTests
{
	// ---- <Exceptions>: ratchet pattern ----

	[Fact]
	public async Task Exceptions_Forbidden_StartsWithLegacy_ExemptsMatchingType()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class endsWith="Store">
		                                  <Fix Rename="Repository" />
		                                  <Exceptions>
		                                      <Class startsWith="Legacy" />
		                                  </Exceptions>
		                              </Class>
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class LegacyPatientStore { }
		                      public class OrderHistoryManager(LegacyPatientStore store) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task Exceptions_Forbidden_NewOffender_StillReportsARCH003()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class endsWith="Store">
		                                  <Fix Rename="Repository" />
		                                  <Exceptions>
		                                      <Class startsWith="Legacy" />
		                                  </Exceptions>
		                              </Class>
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IPaymentStore { }
		                      public class PaymentManager(IPaymentStore store) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task Exceptions_Forbidden_TypeNameMatcher_ExemptsExactName()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class endsWith="Store">
		                                  <Exceptions>
		                                      <Class typeName="ThirdPartyStore" />
		                                  </Exceptions>
		                              </Class>
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class ThirdPartyStore { }
		                      public class PartnerStore { }
		                      public class PartnerManager(ThirdPartyStore allowed, PartnerStore blocked) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var forbidden = diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.ToList();

		forbidden.Should().ContainSingle();
		forbidden[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain("PartnerStore");
	}

	[Fact]
	public async Task Exceptions_Forbidden_ExactNameMatcher_ExemptsExactName()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class endsWith="Store">
		                                  <Exceptions>
		                                      <Class exactName="ThirdPartyStore" />
		                                  </Exceptions>
		                              </Class>
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class ThirdPartyStore { }
		                      public class PartnerStore { }
		                      public class PartnerManager(ThirdPartyStore allowed, PartnerStore blocked) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var forbidden = diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.ToList();

		forbidden.Should().ContainSingle();
		forbidden[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain("PartnerStore");
	}

	[Fact]
	public async Task Exceptions_Forbidden_NamespaceException_ExemptsTypesInNamespace()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class endsWith="Store">
		                                  <Exceptions>
		                                      <Namespace contains="Vendor" />
		                                  </Exceptions>
		                              </Class>
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      namespace App.Vendor.Sdk { public class VendorStore { } }
		                      namespace App.Core { public class OrderStore { } }
		                      namespace App
		                      {
		                          using App.Vendor.Sdk;
		                          using App.Core;
		                          public class OrderManager(VendorStore allowed, OrderStore blocked) { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var forbidden = diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.ToList();

		forbidden.Should().ContainSingle();
		forbidden[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain("OrderStore");
	}

	[Fact]
	public async Task Exceptions_Layer_ExceptedType_FallsThroughToNextRule()
	{
		// A Repository-suffixed type that is exempted from the Repository layer falls
		// through to the next matcher, which classifies it as Infrastructure instead.
		// The Manager -> Infrastructure edge is allowed, so no diagnostic fires.
		// PatientRepository (not exempted) stays in Repository — also allowed.
		const string config = """
		                      <ArchitecturalLevels requireRecognizedDependencies="Constructor">
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Layer name="Repository">
		                              <Class endsWith="Repository">
		                                  <Exceptions>
		                                      <Class typeName="InMemoryFakeRepository" />
		                                  </Exceptions>
		                              </Class>
		                          </Layer>
		                          <Layer name="Infrastructure">
		                              <Class endsWith="Repository" />
		                          </Layer>
		                          <AllowedDependency from="Application" to="Repository" />
		                          <AllowedDependency from="Application" to="Infrastructure" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class InMemoryFakeRepository { }
		                      public class PatientRepository { }
		                      public class PatientManager(PatientRepository repo, InMemoryFakeRepository fake) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task Exceptions_Layer_NestedExceptionsAlternateExclusionByDepth()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Controller">
		                              <Class endsWith="Controller" />
		                          </Layer>
		                          <Layer name="Repository">
		                              <Class endsWith="Repository">
		                                  <Exceptions>
		                                      <Class typeName="InMemoryRepository">
		                                          <Exceptions>
		                                              <Class typeName="CheeseInMemoryRepository">
		                                                  <Exceptions>
		                                                      <Class typeName="CreamCheeseInMemoryRepository" />
		                                                  </Exceptions>
		                                              </Class>
		                                          </Exceptions>
		                                      </Class>
		                                  </Exceptions>
		                              </Class>
		                          </Layer>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class PatientRepository { }
		                      public class InMemoryRepository { }
		                      public class CheeseInMemoryRepository { }
		                      public class CreamCheeseInMemoryRepository { }
		                      public class PizzaController(PatientRepository patient, InMemoryRepository memory, CheeseInMemoryRepository cheese, CreamCheeseInMemoryRepository cream) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var arch001Messages = diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Select(d => d.GetMessage(CultureInfo.InvariantCulture))
			.ToList();

		arch001Messages.Should().HaveCount(2);
		arch001Messages.Should().Contain(message => message.Contains("PatientRepository", StringComparison.Ordinal));
		arch001Messages.Should().Contain(message => message.Contains("CheeseInMemoryRepository", StringComparison.Ordinal));
		arch001Messages.Should().NotContain(message => message.Contains("InMemoryRepository", StringComparison.Ordinal) && !message.Contains("CheeseInMemoryRepository", StringComparison.Ordinal));
		arch001Messages.Should().NotContain(message => message.Contains("CreamCheeseInMemoryRepository", StringComparison.Ordinal));
	}

	// ---- One exception-bypass test per ARCH violation kind ----

	[Fact]
	public async Task Exceptions_ARCH001_ExceptedTargetType_DoesNotFireIllegalDependency()
	{
		// Controller -> Repository normally fires ARCH001 (no AllowedDependency edge).
		// The exception removes BootstrapRepository from the Repository layer so it's
		// unlayered, and ARCH001 only fires for type pairs where both sides have layers.
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Controller">
		                              <Class endsWith="Controller" />
		                          </Layer>
		                          <Layer name="Repository">
		                              <Class endsWith="Repository">
		                                  <Exceptions>
		                                      <Class typeName="BootstrapRepository" />
		                                  </Exceptions>
		                              </Class>
		                          </Layer>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class BootstrapRepository { }
		                      public class PatientRepository { }
		                      public class HomeController(BootstrapRepository ok, PatientRepository bad) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var arch001 = diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.ToList();

		arch001.Should().ContainSingle();
		arch001[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain("PatientRepository");
	}

	[Fact]
	public async Task Exceptions_ARCH002_CannotBeBypassedByExceptions_ByDesign()
	{
		// ARCH002 fires precisely BECAUSE no layer rule matched. An <Exceptions>
		// block makes a matched type fall through to "no layer" — which is exactly
		// the ARCH002 condition. Excepting an unknown type from a non-matching rule
		// changes nothing. The supported bypass is positive classification (add the
		// type to a layer via <Class typeName="..." />). The code-fix surface reflects
		// this: ARCH002 is not in FixableDiagnosticIds.
		const string config = """
		                      <ArchitecturalLevels requireRecognizedDependencies="Constructor">
		                          <Layer name="Application">
		                              <Class endsWith="Manager">
		                                  <Exceptions>
		                                      <Class typeName="ISomeUnknownHelper" />
		                                  </Exceptions>
		                              </Class>
		                          </Layer>
		                          <Layer name="Repository">
		                              <Class endsWith="Repository" />
		                          </Layer>
		                          <AllowedDependency from="Application" to="Repository" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface ISomeUnknownHelper { }
		                      public class OrderManager(ISomeUnknownHelper helper) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency)
			.Should().ContainSingle();

		new ArchitecturalLevelCodeFixProvider()
			.FixableDiagnosticIds
			.Should().NotContain(ArchitecturalDiagnosticIds.UnrecognizedDependency);
	}

	[Fact]
	public async Task Exceptions_ARCH003_ExceptedType_DoesNotFireForbiddenDependency()
	{
		// Single-rule restatement of the broader Exceptions_Forbidden_* coverage,
		// kept here so the per-ARCH grid is symmetric and obvious.
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class endsWith="Store">
		                                  <Exceptions>
		                                      <Class typeName="ThirdPartyPatientStore" />
		                                  </Exceptions>
		                              </Class>
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class ThirdPartyPatientStore { }
		                      public class OrderHistoryManager(ThirdPartyPatientStore store) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().BeEmpty();
	}

	[Fact]
	public async Task Exceptions_ARCH004_ExceptedTargetType_DoesNotFireWrongDirection()
	{
		// Application -> Controller normally fires ARCH004 (reverse of configured
		// Controller -> Application edge). Excepting IBootstrapController demotes it
		// to unlayered, so the wrong-direction check no longer applies.
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Controller">
		                              <Class endsWith="Controller">
		                                  <Exceptions>
		                                      <Class typeName="IBootstrapController" />
		                                  </Exceptions>
		                              </Class>
		                          </Layer>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <AllowedDependency from="Controller" to="Application" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IBootstrapController { }
		                      public interface IPatientController { }
		                      public class OrderManager(IBootstrapController ok, IPatientController bad) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var arch004 = diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.WrongDirectionDependency)
			.ToList();

		arch004.Should().ContainSingle();
		arch004[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain("IPatientController");
	}

	[Fact]
	public async Task Exceptions_ARCH005_ExceptedTargetType_DoesNotFireSameLayer()
	{
		// Manager -> Manager normally fires ARCH005. Excepting IDispatcherManager
		// from the Application layer removes it from the same-layer comparison.
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager">
		                                  <Exceptions>
		                                      <Class typeName="IDispatcherManager" />
		                                  </Exceptions>
		                              </Class>
		                          </Layer>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IDispatcherManager { }
		                      public interface IPatientManager { }
		                      public class OrderManager(IDispatcherManager ok, IPatientManager bad) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var arch005 = diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.SameLayerDependency)
			.ToList();

		arch005.Should().ContainSingle();
		arch005[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain("IPatientManager");
	}
}
