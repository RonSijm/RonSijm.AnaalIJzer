using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.EditorRuntime.Tests.Editor.Snapshots;
public sealed partial class EditorSnapshotTests
{
	[Fact]
	public async Task SiteDiagnostics_LabelsEverySupportedDependencySite()
	{
		const string source = """
		                      using System;

		                      [SampleMarker]
		                      public class CallerType : TargetBase, ITargetDependency
		                      {
		                          private Lazy<TargetDependency>? lazy;
		                          private TargetDependency? field;

		                          public CallerType(TargetDependency constructorDependency)
		                          {
		                          }

		                          public TargetDependency? Property { get; set; }

		                          public TargetDependency Run(TargetDependency methodDependency)
		                          {
		                              TargetDependency local = new TargetDependency();
		                              GenericCall<TargetDependency>();
		                              _ = StaticTarget.Value;

		                              return local;
		                          }

		                          private void GenericCall<T>()
		                          {
		                          }
		                      }

		                      public class TargetBase { }
		                      public interface ITargetDependency { }
		                      public class TargetDependency { }
		                      public class StaticTarget { public static int Value; }
		                      public sealed class SampleMarkerAttribute : Attribute { }
		                      """;
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                        <Layer name="Dependency">
		                          <Class startsWith="Target" />
		                          <Class typeName="ITargetDependency" />
		                          <Class typeName="StaticTarget" />
		                          <Class typeName="SampleMarkerAttribute" />
		                        </Layer>
		                        <AllowedDependency from="Caller" to="Dependency" />
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);
		var callerIndicators = snapshot.SiteIndicators.Where(indicator => indicator.CallerTypeName == "CallerType").ToArray();
		var sites = callerIndicators.Select(indicator => indicator.Site).Distinct().OrderBy(site => site, StringComparer.Ordinal).ToArray();

		sites.Should().Equal(ArchitectureDependencySites.All.OrderBy(site => site, StringComparer.Ordinal));
		callerIndicators.Should().OnlyContain(indicator => indicator.Status == ArchitectureDependencySiteStatus.Allowed);
	}

	[Fact]
	public async Task SiteDiagnostics_CaptureDependencyLayerPaletteSlot()
	{
		const string source = """
		                      public class CallerType
		                      {
		                          private TargetDependency? field;
		                      }

		                      public class TargetDependency { }
		                      """;
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                        <Layer name="CrossCutting"><Class typeName="TargetDependency" /></Layer>
		                        <AllowedDependency from="Caller" to="CrossCutting" />
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);
		var indicator = snapshot.SiteIndicators.Should().ContainSingle(indicator => indicator.Site == ArchitectureDependencySites.Field).Subject;

		indicator.DependencyLayerPath.Should().Be("CrossCutting");
		indicator.DependencyLayerPaletteSlot.Should().Be(2);
	}

	[Theory]
	[InlineData(ArchitectureDependencySiteStatus.Allowed, ArchitecturalDiagnosticIds.IllegalLevelDependency, """
		                                                                                                     <ArchitecturalLevels>
		                                                                                                       <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                                                                                                       <Layer name="Dependency"><Class typeName="TargetDependency" /></Layer>
		                                                                                                       <AllowedDependency from="Caller" to="Dependency" />
		                                                                                                     </ArchitecturalLevels>
		                                                                                                     """)]
	[InlineData(ArchitectureDependencySiteStatus.MissingAllowedDependency, ArchitecturalDiagnosticIds.IllegalLevelDependency, """
		                                                                                                                        <ArchitecturalLevels>
		                                                                                                                          <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                                                                                                                          <Layer name="Dependency"><Class typeName="TargetDependency" /></Layer>
		                                                                                                                        </ArchitecturalLevels>
		                                                                                                                        """)]
	[InlineData(ArchitectureDependencySiteStatus.SiteFiltered, ArchitecturalDiagnosticIds.IllegalLevelDependency, """
		                                                                                                              <ArchitecturalLevels>
		                                                                                                                <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                                                                                                                <Layer name="Dependency"><Class typeName="TargetDependency" /></Layer>
		                                                                                                                <AllowedDependency from="Caller" to="Dependency" allowedSites="Local" />
		                                                                                                              </ArchitecturalLevels>
		                                                                                                              """)]
	[InlineData(ArchitectureDependencySiteStatus.Blocked, ArchitecturalDiagnosticIds.IllegalLevelDependency, """
		                                                                                                      <ArchitecturalLevels>
		                                                                                                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                                                                                                        <Layer name="Dependency"><Class typeName="TargetDependency" /></Layer>
		                                                                                                        <AllowedDependency from="Caller" to="Dependency" />
		                                                                                                        <BlockedDependency from="Caller" to="Dependency" />
		                                                                                                      </ArchitecturalLevels>
		                                                                                                      """)]
	[InlineData(ArchitectureDependencySiteStatus.WrongDirection, ArchitecturalDiagnosticIds.WrongDirectionDependency, """
		                                                                                                                <ArchitecturalLevels>
		                                                                                                                  <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                                                                                                                  <Layer name="Dependency"><Class typeName="TargetDependency" /></Layer>
		                                                                                                                  <AllowedDependency from="Dependency" to="Caller" />
		                                                                                                                </ArchitecturalLevels>
		                                                                                                                """)]
	[InlineData(ArchitectureDependencySiteStatus.SameLayer, ArchitecturalDiagnosticIds.SameLayerDependency, """
		                                                                                                      <ArchitecturalLevels>
		                                                                                                        <Layer name="Same">
		                                                                                                          <Class typeName="CallerType" />
		                                                                                                          <Class typeName="TargetDependency" />
		                                                                                                        </Layer>
		                                                                                                      </ArchitecturalLevels>
		                                                                                                      """)]
	[InlineData(ArchitectureDependencySiteStatus.TypePolicyViolation, ArchitecturalDiagnosticIds.ForbiddenDependency, """
		                                                                                                                <ArchitecturalLevels>
		                                                                                                                  <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                                                                                                                  <Layer name="Dependency"><Class typeName="TargetDependency" /></Layer>
		                                                                                                                  <Forbidden><Class typeName="TargetDependency" /></Forbidden>
		                                                                                                                  <AllowedDependency from="Caller" to="Dependency" />
		                                                                                                                </ArchitecturalLevels>
		                                                                                                                """)]
	public async Task SiteDiagnostics_ReportExpectedStatus(ArchitectureDependencySiteStatus expectedStatus, string expectedDiagnosticId, string config)
	{
		const string source = """
		                      public class CallerType
		                      {
		                          public CallerType(TargetDependency dependency)
		                          {
		                          }
		                      }

		                      public class TargetDependency { }
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);
		var indicator = snapshot.SiteIndicators.Should().ContainSingle(indicator => indicator.Site == ArchitectureDependencySites.Constructor).Subject;

		indicator.Status.Should().Be(expectedStatus);
		if (expectedStatus == ArchitectureDependencySiteStatus.Allowed)
		{
			indicator.DiagnosticId.Should().BeNull();
		}
		else
		{
			indicator.DiagnosticId.Should().Be(expectedDiagnosticId);
		}
	}

	[Fact]
	public async Task SiteDiagnostics_ReportUnrecognizedRequiredDependency()
	{
		const string source = """
		                      public class CallerType
		                      {
		                          public CallerType(MysteryDependency dependency)
		                          {
		                          }
		                      }

		                      public class MysteryDependency { }
		                      """;
		const string config = """
		                      <ArchitecturalLevels requireRecognizedDependencies="Constructor">
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);
		var indicator = snapshot.SiteIndicators.Should().ContainSingle(indicator => indicator.Site == ArchitectureDependencySites.Constructor).Subject;

		indicator.Status.Should().Be(ArchitectureDependencySiteStatus.Unrecognized);
		indicator.DiagnosticId.Should().Be(ArchitecturalDiagnosticIds.UnrecognizedDependency);
	}

	[Fact]
	public async Task SiteDiagnostics_ShowForbiddenOperationPolicyStatus()
	{
		const string source = """
			using System;

			public sealed class PizzaKitchen
			{
				public DateTime Prepare() => DateTime.UtcNow;
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Kitchen">
			    <Class endsWith="Kitchen" />
			    <ForbiddenOperations>
			      <ForbiddenOperation allowedSites="StaticMember">
			        <OperationMatcher kind="PropertyRead" staticAccess="true">
			          <ContainingType exactFullName="System.DateTime" />
			          <Member exactName="UtcNow" memberKind="Property" />
			        </OperationMatcher>
			      </ForbiddenOperation>
			    </ForbiddenOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var snapshot = await CreateSnapshotAsync(source, config);
		var indicator = snapshot.SiteIndicators.Should().ContainSingle(item => item.DiagnosticId == ArchitecturalDiagnosticIds.ForbiddenOperationPolicyViolation).Subject;

		indicator.Site.Should().Be(ArchitectureDependencySites.StaticMember);
		indicator.Status.Should().Be(ArchitectureDependencySiteStatus.TypePolicyViolation);
		indicator.DependencyTypeName.Should().Contain("DateTime.UtcNow");
		indicator.Reason.Should().Contain("ForbiddenOperations policy");
	}

	[Fact]
	public async Task SiteDiagnostics_ShowBehavioralOperationPolicyStatus()
	{
		const string source = """
			namespace Shop.Application;

			public sealed class PizzaKitchen
			{
				public void Submit()
				{
					PizzaRepository.Save();
				}
			}

			public static class PizzaValidator { public static void Validate() { } }
			public static class PizzaRepository { public static void Save() { } }
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <BehavioralOperations>
			      <RequiredOperationBefore>
			        <DeclarationMatcher>
			          <Member exactName="Submit" memberKind="Method" />
			        </DeclarationMatcher>
			        <OperationMatcher kind="Invocation">
			          <ContainingType typeName="PizzaValidator" />
			          <Member exactName="Validate" memberKind="Method" />
			        </OperationMatcher>
			        <BeforeOperation>
			          <OperationMatcher kind="Invocation">
			            <ContainingType typeName="PizzaRepository" />
			            <Member exactName="Save" memberKind="Method" />
			          </OperationMatcher>
			        </BeforeOperation>
			      </RequiredOperationBefore>
			    </BehavioralOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var snapshot = await CreateSnapshotAsync(source, config);
		var indicator = snapshot.SiteIndicators.Should().ContainSingle(item => item.DiagnosticId == ArchitecturalDiagnosticIds.BehavioralOperationPolicyViolation).Subject;

		indicator.Site.Should().Be(ArchitectureDependencySites.StaticMember);
		indicator.Status.Should().Be(ArchitectureDependencySiteStatus.TypePolicyViolation);
		indicator.DependencyTypeName.Should().Contain("PizzaRepository.Save");
		indicator.Reason.Should().Contain("requires");
		indicator.Reason.Should().Contain("before");
	}

	[Fact]
	public async Task SiteDiagnostics_ShowMissingBehavioralOperationAtTheOwningDeclaration()
	{
		const string source = """
			namespace Shop.Application;

			public sealed class PizzaKitchen
			{
				public void Submit()
				{
				}
			}

			public static class PizzaValidator { public static void Validate() { } }
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <BehavioralOperations>
			      <RequiredOperation ordering="Lexical">
			        <DeclarationMatcher>
			          <Member exactName="Submit" memberKind="Method" />
			        </DeclarationMatcher>
			        <OperationMatcher kind="Invocation">
			          <ContainingType typeName="PizzaValidator" />
			          <Member exactName="Validate" memberKind="Method" />
			        </OperationMatcher>
			      </RequiredOperation>
			    </BehavioralOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var snapshot = await CreateSnapshotAsync(source, config);
		var indicator = snapshot.SiteIndicators.Should().ContainSingle(item => item.DiagnosticId == ArchitecturalDiagnosticIds.BehavioralOperationPolicyViolation).Subject;

		indicator.Site.Should().Be(ArchitectureDependencySites.Method);
		indicator.DependencyTypeName.Should().Contain("RequiredOperation");
		indicator.Reason.Should().Contain("requires");
	}

	[Fact]
	public async Task SiteDiagnostics_ShowBehavioralOperationPolicyOnExpressionBodiedProperty()
	{
		const string source = """
			namespace Shop.Application;

			public sealed class PizzaKitchen
			{
				public string MenuPizza => PizzaMenu.Lookup();
			}

			public static class PizzaSafetyCheck { public static void Validate() { } }
			public static class PizzaMenu { public static string Lookup() => "Margherita"; }
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <BehavioralOperations>
			      <RequiredOperation ordering="Lexical">
			        <DeclarationMatcher>
			          <Member exactName="MenuPizza" memberKind="Property" />
			        </DeclarationMatcher>
			        <OperationMatcher kind="Invocation">
			          <ContainingType typeName="PizzaSafetyCheck" />
			          <Member exactName="Validate" memberKind="Method" />
			        </OperationMatcher>
			      </RequiredOperation>
			    </BehavioralOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var snapshot = await CreateSnapshotAsync(source, config);
		var indicator = snapshot.SiteIndicators.Should().ContainSingle(item => item.DiagnosticId == ArchitecturalDiagnosticIds.BehavioralOperationPolicyViolation).Subject;

		indicator.Site.Should().Be(ArchitectureDependencySites.Property);
		indicator.CallerTypeName.Should().Be("PizzaKitchen");
		indicator.Reason.Should().Contain("requires");
	}

}
