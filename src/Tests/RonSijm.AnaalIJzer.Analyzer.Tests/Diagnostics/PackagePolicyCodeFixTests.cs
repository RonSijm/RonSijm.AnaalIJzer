using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.BuildMetadata;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

public sealed class PackagePolicyCodeFixTests
{
	[Fact]
	public void PackageReferenceViolation_IsListedAsFixable()
	{
		new ArchitecturalLevelCodeFixProvider()
			.FixableDiagnosticIds
			.Should().Contain(ArchitecturalDiagnosticIds.PackageReferenceViolation);
	}

	[Fact]
	public async Task AllowedPackageListMiss_AddsExactPackageMatcherToConfiguration()
	{
		const string config = """
			<ArchitecturalLevels>
			  <ProjectArchitecture requireRecognizedProjects="true">
			    <ProjectGroup name="Domain">
			      <Project endsWith=".Domain" />
			    </ProjectGroup>
			    <PackagePolicy projectGroup="Domain">
			      <Allowed>
			        <Package exactName="System.Text.Json" />
			      </Allowed>
			    </PackagePolicy>
			  </ProjectArchitecture>
			</ArchitecturalLevels>
			""";
		var manifest = string.Join(
			"\n",
			ArchitectureReferenceManifest.Header,
			@"Package	D:\repo\Shop.Domain.csproj	Microsoft.Extensions.Logging	9.0.0	Direct");
		const string source = "public sealed class Placeholder { }";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			[("Architecture.anl", config), (ArchitectureReferenceManifest.FileName, manifest)],
			ArchitecturalDiagnosticIds.PackageReferenceViolation,
			"Allow package 'Microsoft.Extensions.Logging' for project group 'Domain'",
			"Architecture.anl");

		updatedConfig.Should().Contain("<Package exactName=\"Microsoft.Extensions.Logging\" />");
	}

	[Fact]
	public async Task ForbiddenPackagePolicy_DoesNotOfferAnIncorrectAllowListFix()
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
			"\n",
			ArchitectureReferenceManifest.Header,
			@"Package	D:\repo\Shop.Domain.csproj	Microsoft.Extensions.Logging	9.0.0	Direct");
		const string source = "public sealed class Placeholder { }";

		var titles = await AnalyzerTestHelper.GetCodeFixTitlesAsync(
			source,
			[("Architecture.anl", config), (ArchitectureReferenceManifest.FileName, manifest)],
			ArchitecturalDiagnosticIds.PackageReferenceViolation);

		titles.Should().BeEmpty();
	}

	[Fact]
	public async Task AllowedPackageListMiss_InlineSettings_UpdatesAssemblyMetadata()
	{
		var manifest = string.Join(
			"\n",
			ArchitectureReferenceManifest.Header,
			@"Package	D:\repo\Shop.Domain.csproj	Microsoft.Extensions.Logging	9.0.0	Direct");
		const string source = """"
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", """
			<ArchitecturalLevels>
			  <ProjectArchitecture requireRecognizedProjects="true">
			    <ProjectGroup name="Domain">
			      <Project endsWith=".Domain" />
			    </ProjectGroup>
			    <PackagePolicy projectGroup="Domain">
			      <Allowed>
			        <Package exactName="System.Text.Json" />
			      </Allowed>
			    </PackagePolicy>
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
			ArchitecturalDiagnosticIds.PackageReferenceViolation,
			"Allow package 'Microsoft.Extensions.Logging' for project group 'Domain'");

		updatedSource.Should().Contain("<Package exactName=\"Microsoft.Extensions.Logging\" />");
	}
}
