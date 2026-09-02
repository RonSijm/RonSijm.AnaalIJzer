using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.BuildMetadata;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

public sealed class ProjectArchitectureCodeFixTests
{
	[Fact]
	public void ProjectReferenceViolation_IsListedAsFixable()
	{
		new ArchitecturalLevelCodeFixProvider()
			.FixableDiagnosticIds
			.Should().Contain(ArchitecturalDiagnosticIds.ProjectReferenceViolation);
	}

	[Fact]
	public async Task MissingAllowedProjectReference_AddsAllowedProjectReferenceToConfiguration()
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
			    <AllowedProjectReference from="Application" to="Domain" />
			  </ProjectArchitecture>
			</ArchitecturalLevels>
			""";
		var manifest = string.Join(
			"\n",
			ArchitectureReferenceManifest.Header,
			@"Project	D:\repo\Shop.Web.csproj	D:\repo\Shop.Domain.csproj");
		const string source = "public sealed class Placeholder { }";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			[("Architecture.anl", config), (ArchitectureReferenceManifest.FileName, manifest)],
			ArchitecturalDiagnosticIds.ProjectReferenceViolation,
			"Allow project group 'Presentation' to reference 'Domain'",
			"Architecture.anl");

		updatedConfig.Should().Contain("<AllowedProjectReference from=\"Presentation\" to=\"Domain\" />");
	}

	[Fact]
	public async Task SameGroupProjectReference_AddsExplicitSelfEdge()
	{
		const string config = """
			<ArchitecturalLevels>
			  <ProjectArchitecture requireRecognizedProjects="true">
			    <ProjectGroup name="Domain">
			      <Project endsWith=".Domain" />
			    </ProjectGroup>
			    <ProjectGroup name="Application">
			      <Project endsWith=".Application" />
			    </ProjectGroup>
			    <AllowedProjectReference from="Domain" to="Application" />
			  </ProjectArchitecture>
			</ArchitecturalLevels>
			""";
		var manifest = string.Join(
			"\n",
			ArchitectureReferenceManifest.Header,
			@"Project	D:\repo\Shop.Domain.csproj	D:\repo\Shop.Domain.csproj");
		const string source = "public sealed class Placeholder { }";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			[("Architecture.anl", config), (ArchitectureReferenceManifest.FileName, manifest)],
			ArchitecturalDiagnosticIds.ProjectReferenceViolation,
			"Allow project group 'Domain' to reference itself",
			"Architecture.anl");

		updatedConfig.Should().Contain("<AllowedProjectReference from=\"Domain\" to=\"Domain\" />");
	}

	[Fact]
	public async Task BlockedProjectReference_OffersRuleRemoval()
	{
		const string config = """
			<ArchitecturalLevels>
			  <ProjectArchitecture requireRecognizedProjects="true">
			    <ProjectGroup name="Domain">
			      <Project endsWith=".Domain" />
			    </ProjectGroup>
			    <ProjectGroup name="Infrastructure">
			      <Project endsWith=".Infrastructure" />
			    </ProjectGroup>
			    <BlockedProjectReference from="Domain" to="Infrastructure" />
			  </ProjectArchitecture>
			</ArchitecturalLevels>
			""";
		var manifest = string.Join(
			"\n",
			ArchitectureReferenceManifest.Header,
			@"Project	D:\repo\Shop.Domain.csproj	D:\repo\Shop.Infrastructure.csproj");
		const string source = "public sealed class Placeholder { }";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			[("Architecture.anl", config), (ArchitectureReferenceManifest.FileName, manifest)],
			ArchitecturalDiagnosticIds.ProjectReferenceViolation,
			"Remove blocking <BlockedProjectReference from=\"Domain\" to=\"Infrastructure\" />",
			"Architecture.anl");

		updatedConfig.Should().NotContain("<BlockedProjectReference from=\"Domain\" to=\"Infrastructure\" />");
	}

	[Fact]
	public async Task MissingAllowedProjectReference_InlineSettings_UpdatesAssemblyMetadata()
	{
		var manifest = string.Join(
			"\n",
			ArchitectureReferenceManifest.Header,
			@"Project	D:\repo\Shop.Web.csproj	D:\repo\Shop.Domain.csproj");
		const string source = """"
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", """
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
			    <AllowedProjectReference from="Application" to="Domain" />
			  </ProjectArchitecture>
			</ArchitecturalLevels>
			""")]

			public sealed class Placeholder
			{
			}
			"""";

		var updatedSource = await AnalyzerTestHelper.ApplyCodeFixAsync(
			source,
			[(ArchitectureReferenceManifest.FileName, manifest)],
			ArchitecturalDiagnosticIds.ProjectReferenceViolation,
			"Allow project group 'Presentation' to reference 'Domain'");

		updatedSource.Should().Contain("<AllowedProjectReference from=\"Presentation\" to=\"Domain\" />");
	}
}
