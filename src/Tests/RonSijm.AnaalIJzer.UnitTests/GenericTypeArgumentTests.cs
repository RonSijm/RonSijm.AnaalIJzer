namespace RonSijm.AnaalIJzer.UnitTests;

public sealed class GenericTypeArgumentTests
{
	[Fact]
	public async Task GenericTypeArgument_LazyOfRepository_FromController_ReportsARCH001()
	{
		const string source = """
		                      using System;
		                      public interface IPatientRepository { }
		                      public class OrderController(Lazy<IPatientRepository> repo) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().ContainSingle()
			.Which.GetMessage(CultureInfo.InvariantCulture)
			.Should().Contain("IPatientRepository");
	}

	[Fact]
	public async Task GenericTypeArgument_EnumerableOfRepository_FromController_ReportsARCH001()
	{
		const string source = """
		                      using System.Collections.Generic;
		                      public interface IPatientRepository { }
		                      public class ReportController(IEnumerable<IPatientRepository> repos) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task GenericTypeArgument_NestedGenerics_AreUnwrappedRecursively()
	{
		const string source = """
		                      using System;
		                      using System.Collections.Generic;
		                      public interface IPatientRepository { }
		                      public class AuditController(Lazy<IEnumerable<IPatientRepository>> repos) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task GenericTypeArgument_SelfReference_IsAllowed()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Kitchen" />
		                          </Layer>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface ILogger<T> { }
		                      public class PizzaKitchen(ILogger<PizzaKitchen> log) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task GenericTypeArgument_DuplicateInnerType_ReportsOnlyOnce()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Controller">
		                              <Class endsWith="Controller" />
		                          </Layer>
		                          <Layer name="Repository">
		                              <Class endsWith="Repository" />
		                          </Layer>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class Pair<TFirst, TSecond> { }
		                      namespace VendorA { public class CheeseRepository { } }
		                      namespace VendorB { public class CheeseRepository { } }
		                      public class PizzaController(Pair<VendorA.CheeseRepository, VendorB.CheeseRepository> repositories) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task GenericTypeArgument_AllowedDependency_NoDiagnostic()
	{
		// Controller -> Lazy<IPatientManager> is allowed because the inner type maps to Application.
		const string source = """
		                      using System;
		                      public interface IPatientManager { }
		                      public class PatientController(Lazy<IPatientManager> manager) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task GenericTypeArgument_ForbiddenTypeInsideGeneric_ReportsARCH003()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Manager">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class endsWith="Store" comment="Use a Repository instead." />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      using System;
		                      public interface IPartnerStore { }
		                      public class PatientManager(Lazy<IPartnerStore> store) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle()
			.Which.GetMessage(CultureInfo.InvariantCulture)
			.Should().Contain("IPartnerStore");
	}

	[Fact]
	public async Task GenericTypeArgument_ForbiddenInsideGeneric_NoRenameProperties()
	{
		// A rename code-fix on the parameter would rewrite the outer Lazy, not the inner
		// IPartnerStore — so no rename metadata should be attached for nested matches.
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Manager">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class endsWith="Store" comment="Use a Repository instead.">
		                                  <Fix Rename="Repository" />
		                              </Class>
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      using System;
		                      public interface IPartnerStore { }
		                      public class PatientManager(Lazy<IPartnerStore> store) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var arch003 = diagnostics.First(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency);
		arch003.Properties.ContainsKey(ArchitecturalLevelAnalyzer.PropertyMatchedSuffix).Should().BeFalse();
		arch003.Properties.ContainsKey(ArchitecturalLevelAnalyzer.PropertyFixSuffix).Should().BeFalse();
	}

	[Fact]
	public async Task GenericTypeArgument_OuterAndInnerInDifferentLayers_BothReported()
	{
		// Outer 'OtherManager' matches Manager (same layer as caller -> ARCH005).
		// Inner 'IPatientController' matches Controller (wrong direction -> ARCH004).
		const string source = """
		                      using System.Collections.Generic;
		                      public interface IPatientController { }
		                      public class OtherManager<T> { }
		                      public class PatientManager(OtherManager<IPatientController> wrapped) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.SameLayerDependency);
		diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.WrongDirectionDependency);
	}

	[Fact]
	public async Task GenericTypeArgument_StrictMode_RecognisedThroughInner_NoARCH002()
	{
		// Outer 'Lazy' is not in any layer, but inner PatientConsentRepository is — strict mode
		// should not flag the parameter as unrecognised.
		const string source = """
		                      using System;
		                      public class PatientConsentRepository { }
		                      public class PatientManager(Lazy<PatientConsentRepository> repo) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.StrictConfig);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task GenericTypeArgument_StrictMode_AllUnrecognised_ReportsARCH002Once()
	{
		const string source = """
		                      using System;
		                      public interface IPartnerStore { }
		                      public class PatientManager(Lazy<IPartnerStore> store) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.StrictConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency)
			.Should().ContainSingle();
	}
}
