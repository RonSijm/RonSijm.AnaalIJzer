using RonSijm.AnaalIJzer.UnitTests.TestSupport;

namespace RonSijm.AnaalIJzer.UnitTests.Analysis;

public sealed class RecognizedDependencyRequirementTests
{
	public static TheoryData<string, string> RecognitionSiteCases => new()
	{
		{ DependencySites.Constructor, "public class CallerType(TargetDependency dependency) { }" },
		{ DependencySites.Method, "public class CallerType { public void Use(TargetDependency dependency) { } }" },
		{ DependencySites.MethodReturn, "public class CallerType { public TargetDependency Get() => null!; }" },
		{ DependencySites.Field, "public class CallerType { private readonly TargetDependency _dependency = null!; }" },
		{ DependencySites.Property, "public class CallerType { public TargetDependency Dependency { get; set; } = null!; }" },
		{ DependencySites.Local, "public class CallerType { public void Run() { TargetDependency dependency = null!; _ = dependency; } }" },
		{ DependencySites.New, "public class CallerType { public void Run() => _ = new TargetDependency(); }" },
		{ DependencySites.GenericInvocation, "public class CallerType { public void Run() => _ = Resolve<TargetDependency>(); private static T Resolve<T>() where T : class => null!; }" },
		{ DependencySites.GenericArgument, "using System; public class CallerType(Lazy<TargetDependency> dependency) { }" },
		{ DependencySites.Inheritance, "public class CallerType : TargetDependency { }" },
		{ DependencySites.InterfaceImplementation, "public interface TargetDependency { } public class CallerType : TargetDependency { }" },
		{ DependencySites.Attribute, "using System; [TargetDependency] public class CallerType { } public class TargetDependency : Attribute { }" },
		{ DependencySites.StaticMember, "public class CallerType { public int Get() => TargetDependency.Value; } public class TargetDependency { public static int Value => 1; }" }
	};

	[Theory]
	[MemberData(nameof(RecognitionSiteCases))]
	public async Task EveryConfiguredSite_ReportsARCH002(string site, string callerSource)
	{
		var declaresTargetDependency = callerSource.Contains("class TargetDependency", StringComparison.Ordinal) || callerSource.Contains("interface TargetDependency", StringComparison.Ordinal);
		var source = declaresTargetDependency
			? callerSource
			: callerSource + Environment.NewLine + "public class TargetDependency { }";
		var config = $$"""
		               <ArchitecturalLevels requireRecognizedDependencies="{{site}}">
		                 <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		               </ArchitecturalLevels>
		               """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency).Which;
		diagnostic.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(site);
		diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Contain("TargetDependency");
	}

	[Fact]
	public async Task RecognitionRequirement_UnrecognizedDependency_ReportsARCH002()
	{
		const string source = """
		                      public interface IPartnerStore { }
		                      public class PatientConsentRepository { }
		                      public class ActivatePatientConsentManager(PatientConsentRepository repo, IPartnerStore store) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.RequireRecognizedDependenciesConfig);

		var arch002 = diagnostics.Where(d => d.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency).ToList();
		arch002.Count.Should().Be(1);
		arch002[0].GetMessage(CultureInfo.InvariantCulture)
			.Should().Contain("IPartnerStore");
	}

	[Fact]
	public async Task RecognitionRequirement_RecognizedDependency_NoDiagnostic()
	{
		const string source = """
		                      public class PatientConsentRepository { }
		                      public class ActivatePatientConsentManager(PatientConsentRepository repo) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.RequireRecognizedDependenciesConfig);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task RecognitionRequirementDisabled_UnrecognizedDependency_NoDiagnostic()
	{
		const string source = """
		                      public interface IPartnerStore { }
		                      public class PatientConsentRepository { }
		                      public class ActivatePatientConsentManager(PatientConsentRepository repo, IPartnerStore store) { }
		                      """;

		const string requirementDisabledConfig = """
		                                         <ArchitecturalLevels>
		                                             <Layer name="Manager">
		                                                 <Class endsWith="Manager" />
		                                             </Layer>
		                                             <Layer name="Repository">
		                                                 <Class endsWith="Repository" />
		                                             </Layer>
		                                             <AllowedDependency from="Manager" to="Repository" />
		                                         </ArchitecturalLevels>
		                                         """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, requirementDisabledConfig);

		diagnostics.Should().NotContain(d => d.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency);
	}

	[Fact]
	public async Task RecognitionRequirement_LayerScopedRequirement_ReportsOnlyForThatLayer()
	{
		const string source = """
		                      public class MysteryBox { }
		                      public class LegacyChef(MysteryBox box) { }
		                      public class AuditedChef(MysteryBox box) { }
		                      """;
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Legacy"><Class typeName="LegacyChef" /></Layer>
		                        <Layer name="Audited" requireRecognizedDependencies="Constructor"><Class typeName="AuditedChef" /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency).Which;
		diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Contain("AuditedChef").And.Contain("MysteryBox");
	}

	[Fact]
	public async Task RecognitionRequirement_ParentLayerRequirement_AppliesToChildLayer()
	{
		const string source = """
		                      public class MysteryBox { }
		                      namespace Ordering
		                      {
		                          public class ChefService(MysteryBox box) { }
		                      }
		                      """;
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Ordering" requireRecognizedDependencies="Constructor">
		                          <Namespace exactName="Ordering" />
		                          <Layer name="Chef"><Class endsWith="Service" /></Layer>
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency).Which;
		diagnostic.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(DependencySites.Constructor);
		diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Contain("ChefService");
	}

	[Fact]
	public async Task RecognitionRequirement_ChildLayerRequirement_AddsToParentLayerRequirement()
	{
		const string source = """
		                      public class ConstructorMystery { }
		                      public class LocalMystery { }
		                      namespace Ordering
		                      {
		                          public class ChefService(ConstructorMystery box)
		                          {
		                              public void Run()
		                              {
		                                  LocalMystery local = null!;
		                                  _ = local;
		                              }
		                          }
		                      }
		                      """;
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Ordering" requireRecognizedDependencies="Constructor">
		                          <Namespace exactName="Ordering" />
		                          <Layer name="Chef" requireRecognizedDependencies="Local"><Class endsWith="Service" /></Layer>
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Where(item => item.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency)
			.Select(item => item.Properties[ArchitecturalDiagnostics.PropertySite])
			.Should().BeEquivalentTo(DependencySites.Constructor, DependencySites.Local);
	}

	[Fact]
	public async Task RecognitionRequirement_UnknownLayerScopedSiteReportsARCH006()
	{
		const string source = "public class CallerType { }";
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Caller" requireRecognizedDependencies="Somewhere"><Class typeName="CallerType" /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration)
			.Which.GetMessage(CultureInfo.InvariantCulture).Should().Contain("Layer 'Caller'").And.Contain("requireRecognizedDependencies");
	}

	[Fact]
	public async Task RecognitionRequirement_UnlayeredCallerWithUnrecognizedDependency_NoDiagnostic()
	{
		const string source = """
		                      public interface IPartnerStore { }
		                      public class SomeHelper(IPartnerStore store) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.RequireRecognizedDependenciesConfig);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task RecognitionRequirement_SiteListIsTrimmedAndCaseInsensitive()
	{
		const string source = """
		                      public class ConstructorDependency { }
		                      public class LocalDependency { }
		                      public class CallerType(ConstructorDependency dependency)
		                      {
		                          public void Run()
		                          {
		                              LocalDependency local = null!;
		                              _ = local;
		                          }
		                      }
		                      """;
		const string config = """
		                      <ArchitecturalLevels requireRecognizedDependencies=" constructor, LOCAL ">
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Where(item => item.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency)
			.Select(item => item.Properties[ArchitecturalDiagnostics.PropertySite])
			.Should().BeEquivalentTo(DependencySites.Constructor, DependencySites.Local);
	}

	[Fact]
	public async Task RecognitionRequirement_UnknownSiteReportsARCH006()
	{
		const string source = "public class CallerType { }";
		const string config = """
		                      <ArchitecturalLevels requireRecognizedDependencies="Somewhere">
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration)
			.Which.GetMessage(CultureInfo.InvariantCulture).Should().Contain("requireRecognizedDependencies");
	}

	[Fact]
	public async Task RecognitionRequirement_ConstructorAppliesToRecordPrimaryConstructor()
	{
		const string source = """
		                      public class TargetDependency { }
		                      public record CallerType(TargetDependency Dependency);
		                      """;
		const string config = """
		                      <ArchitecturalLevels requireRecognizedDependencies="Constructor">
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency);
	}

	[Fact]
	public async Task RecognitionRequirement_GenericArgumentAppliesInsideRecognizedWrapper()
	{
		const string source = """
		                      using System;
		                      public class TargetDependency { }
		                      public class CallerType(Lazy<TargetDependency> dependency) { }
		                      """;
		const string config = """
		                      <ArchitecturalLevels requireRecognizedDependencies="GenericArgument">
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                        <Layer name="Wrapper"><Class typeName="Lazy" /></Layer>
		                        <AllowedDependency from="Caller" to="Wrapper" />
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency).Which;
		diagnostic.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(DependencySites.GenericArgument);
		diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Contain("TargetDependency");
	}

	[Fact]
	public async Task RecognitionRequirement_IncludedSiteListsAreCombined()
	{
		const string source = """
		                      public class ConstructorDependency { }
		                      public class LocalDependency { }
		                      public class CallerType(ConstructorDependency dependency)
		                      {
		                          public void Run()
		                          {
		                              LocalDependency local = null!;
		                              _ = local;
		                          }
		                      }
		                      """;
		const string rootConfig = """
		                          <ArchitecturalLevels requireRecognizedDependencies="Constructor">
		                            <Include path="Shared.anl" />
		                            <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                          </ArchitecturalLevels>
		                          """;
		const string sharedConfig = """
		                            <ArchitecturalLevels requireRecognizedDependencies="Local" />
		                            """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
			source,
			("Architecture.anl", rootConfig),
			("Shared.anl", sharedConfig));

		diagnostics.Where(item => item.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency)
			.Select(item => item.Properties[ArchitecturalDiagnostics.PropertySite])
			.Should().BeEquivalentTo(DependencySites.Constructor, DependencySites.Local);
	}

	[Fact]
	public async Task RecognitionRequirement_IgnoresLanguageTypesAndSelfReferences()
	{
		const string source = """
		                      public class CallerType
		                      {
		                          public int Value => 1;
		                          public void Run()
		                          {
		                              CallerType self = this;
		                              _ = Resolve<int>();
		                              _ = self;
		                          }
		                          private static T Resolve<T>() => default!;
		                      }
		                      """;
		const string config = """
		                      <ArchitecturalLevels requireRecognizedDependencies="Method, MethodReturn, Property, Local, GenericInvocation, StaticMember">
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency);
	}
}
