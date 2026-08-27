using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis;

public sealed class SourceLocationAnalyzerTests
{
	[Fact]
	public async Task ProjectRelativeSourceLocation_AllowsMatchingFile()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Ordering">
		                          <Class endsWith="Service" />
		                          <SourceLocations>
		                            <Source startsWith="Ordering/" />
		                          </SourceLocations>
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
			[(@"D:\repo\Shop\Ordering\CandyService.cs", "public class CandyService { }")],
			ImmutableDictionary<string, string>.Empty.Add("build_property.MSBuildProjectDirectory", @"D:\repo\Shop"),
			("Architecture.anl", config));

		diagnostics.Should().NotContain(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.SourceLocationViolation);
	}

	[Fact]
	public async Task ProjectRelativeSourceLocation_ReportsARCH015ForMisplacedFile()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Ordering">
		                          <Class endsWith="Service" />
		                          <SourceLocations>
		                            <Source startsWith="Ordering/" />
		                          </SourceLocations>
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
			[(@"D:\repo\Shop\Infrastructure\CandyService.cs", "public class CandyService { }")],
			ImmutableDictionary<string, string>.Empty.Add("build_property.MSBuildProjectDirectory", @"D:\repo\Shop"),
			("Architecture.anl", config));

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.SourceLocationViolation).Subject;
		diagnostic.Properties[ArchitecturalDiagnostics.PropertyCallerLayerName].Should().Be("Ordering");
		diagnostic.Properties[ArchitecturalDiagnostics.PropertyNormalizedSourcePath].Should().Be("Infrastructure/CandyService.cs");
	}

	[Fact]
	public async Task ConfigurationRelativeSourceLocation_UsesOwningConfigDirectory()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Ordering">
		                          <Class endsWith="Service" />
		                          <SourceLocations relativeTo="Configuration">
		                            <Source startsWith="Features/Ordering/" />
		                          </SourceLocations>
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
			[(@"D:\repo\config\Features\Ordering\CandyService.cs", "public class CandyService { }")],
			ImmutableDictionary<string, string>.Empty.Add("build_property.MSBuildProjectDirectory", @"D:\repo\project"),
			(@"D:\repo\config\Architecture.anl", config));

		diagnostics.Should().NotContain(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.SourceLocationViolation);
	}

	[Fact]
	public async Task PartialType_WithOneMisplacedDeclaration_ReportsOnlyThatDeclaration()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Ordering">
		                          <Class exactName="CandyService" />
		                          <SourceLocations>
		                            <Source startsWith="Ordering/" />
		                          </SourceLocations>
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
			[
				(@"D:\repo\Shop\Ordering\CandyService.cs", "public partial class CandyService { }"),
				(@"D:\repo\Shop\Infrastructure\CandyService.Partial.cs", "public partial class CandyService { }")
			],
			ImmutableDictionary<string, string>.Empty.Add("build_property.MSBuildProjectDirectory", @"D:\repo\Shop"),
			("Architecture.anl", config));

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.SourceLocationViolation).Subject;
		diagnostic.Location.SourceTree?.FilePath.Should().Be(@"D:\repo\Shop\Infrastructure\CandyService.Partial.cs");
	}

	[Fact]
	public async Task InlineConfiguration_MayNotUseConfigurationRelativeBase()
	{
		const string source = """
		                      using System.Reflection;
		                      [assembly: AssemblyMetadata("AnaalIJzerSettings", "<ArchitecturalLevels><Layer name=\"Ordering\"><Class endsWith=\"Service\" /><SourceLocations relativeTo=\"Configuration\"><Source startsWith=\"Ordering/\" /></SourceLocations></Layer></ArchitecturalLevels>")]
		                      public class CandyService { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration)
			.Which.GetMessage().Should().Contain("relativeTo='Configuration'");
	}

	[Fact]
	public async Task EmptyFilePath_WithAbsoluteBase_ReportsARCH015()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Ordering">
		                          <Class endsWith="Service" />
		                          <SourceLocations relativeTo="Absolute">
		                            <Source contains="/Ordering/" />
		                          </SourceLocations>
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
			[("", "public class CandyService { }")],
			ImmutableDictionary<string, string>.Empty.Add("build_property.MSBuildProjectDirectory", @"D:\repo\Shop"),
			("Architecture.anl", config));

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.SourceLocationViolation)
			.Which.Properties[ArchitecturalDiagnostics.PropertyViolationReason].Should().Contain("cannot be evaluated");
	}
}
