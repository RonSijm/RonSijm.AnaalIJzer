using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.BuildMetadata;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis;

public sealed class ProjectReferenceBoundaryAnalyzerTests
{
	[Fact]
	public async Task ProjectArchitecture_WithoutManifest_ReportsARCH006()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <ProjectArchitecture>
		                          <ProjectGroup name="Presentation">
		                            <Project endsWith=".Web" />
		                          </ProjectGroup>
		                          <ProjectGroup name="Application">
		                            <Project endsWith=".Application" />
		                          </ProjectGroup>
		                          <AllowedProjectReference from="Presentation" to="Application" />
		                        </ProjectArchitecture>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public class Placeholder { }", ("Architecture.anl", config));

		diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.InvalidConfiguration)
			.Which.GetMessage().Should().Contain("no project-reference manifest");
	}

	[Fact]
	public async Task IllegalProjectReference_ReportsARCH010()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <ProjectArchitecture requireRecognizedProjects="true">
		                          <ProjectGroup name="Presentation">
		                            <Project endsWith=".Web" />
		                          </ProjectGroup>
		                          <ProjectGroup name="Application">
		                            <Project endsWith=".Application" />
		                          </ProjectGroup>
		                          <ProjectGroup name="Domain">
		                            <Project endsWith=".Domain" />
		                          </ProjectGroup>
		                          <AllowedProjectReference from="Presentation" to="Application" />
		                        </ProjectArchitecture>
		                      </ArchitecturalLevels>
		                      """;
		var manifest = string.Join(
			Environment.NewLine,
			ArchitectureReferenceManifest.Header,
			"Project\tD:\\src\\Shop.Web.csproj\tD:\\src\\Shop.Domain.csproj");

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
			"public class Placeholder { }",
			("Architecture.anl", config),
			(ArchitectureReferenceManifest.FileName, manifest));

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ProjectReferenceViolation).Subject;
		diagnostic.GetMessage().Should().Contain("Shop.Web");
		diagnostic.GetMessage().Should().Contain("Shop.Domain");
		diagnostic.Properties[ArchitecturalDiagnostics.PropertySourceProjectGroup].Should().Be("Presentation");
		diagnostic.Properties[ArchitecturalDiagnostics.PropertyTargetProjectGroup].Should().Be("Domain");
	}

	[Fact]
	public async Task LegalProjectReference_DoesNotReportARCH010()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <ProjectArchitecture requireRecognizedProjects="true">
		                          <ProjectGroup name="Presentation">
		                            <Project endsWith=".Web" />
		                          </ProjectGroup>
		                          <ProjectGroup name="Application">
		                            <Project endsWith=".Application" />
		                          </ProjectGroup>
		                          <AllowedProjectReference from="Presentation" to="Application" />
		                        </ProjectArchitecture>
		                      </ArchitecturalLevels>
		                      """;
		var manifest = string.Join(
			Environment.NewLine,
			ArchitectureReferenceManifest.Header,
			"Project\tD:\\src\\Shop.Web.csproj\tD:\\src\\Shop.Application.csproj");

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
			"public class Placeholder { }",
			("Architecture.anl", config),
			(ArchitectureReferenceManifest.FileName, manifest));

		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ProjectReferenceViolation);
		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration);
	}

	[Fact]
	public async Task ForbiddenPackageReference_ReportsARCH011()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <ProjectArchitecture requireRecognizedProjects="true">
		                          <ProjectGroup name="Domain">
		                            <Project endsWith=".Domain" />
		                          </ProjectGroup>
		                          <PackagePolicy projectGroup="Domain">
		                            <Forbidden>
		                              <Package exactName="Microsoft.Extensions.Logging" />
		                            </Forbidden>
		                          </PackagePolicy>
		                        </ProjectArchitecture>
		                      </ArchitecturalLevels>
		                      """;
		var manifest = string.Join(
			Environment.NewLine,
			ArchitectureReferenceManifest.Header,
			"Package\tD:\\src\\Shop.Domain.csproj\tMicrosoft.Extensions.Logging\t9.0.0\tDirect");

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
			"public class Placeholder { }",
			("Architecture.anl", config),
			(ArchitectureReferenceManifest.FileName, manifest));

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.PackageReferenceViolation).Subject;
		diagnostic.GetMessage().Should().Contain("Microsoft.Extensions.Logging");
		diagnostic.Properties[ArchitecturalDiagnostics.PropertyPackageReferenceKind].Should().Be("Direct");
	}

	[Fact]
	public async Task TransitivePackageReference_IsIgnoredByDefault()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <ProjectArchitecture requireRecognizedProjects="true">
		                          <ProjectGroup name="Domain">
		                            <Project endsWith=".Domain" />
		                          </ProjectGroup>
		                          <PackagePolicy projectGroup="Domain">
		                            <Forbidden>
		                              <Package exactName="Microsoft.Extensions.Logging.Abstractions" />
		                            </Forbidden>
		                          </PackagePolicy>
		                        </ProjectArchitecture>
		                      </ArchitecturalLevels>
		                      """;
		var manifest = string.Join(
			Environment.NewLine,
			ArchitectureReferenceManifest.Header,
			"Package\tD:\\src\\Shop.Domain.csproj\tMicrosoft.Extensions.Logging.Abstractions\t9.0.0\tTransitive");

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
			"public class Placeholder { }",
			("Architecture.anl", config),
			(ArchitectureReferenceManifest.FileName, manifest));

		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.PackageReferenceViolation);
	}
}
