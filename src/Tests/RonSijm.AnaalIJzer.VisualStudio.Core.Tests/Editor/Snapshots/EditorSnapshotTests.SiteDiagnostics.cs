using RonSijm.AnaalIJzer.Indicators;

namespace RonSijm.AnaalIJzer.VisualStudio.Core.Tests.Editor.Snapshots;
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

}
