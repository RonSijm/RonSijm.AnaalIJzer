using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;

namespace RonSijm.AnaalIJzer.Application.Tests.ApplicationOperations;

public sealed partial class ApplicationOperationsTests
{
	private static readonly object MsBuildRegistrationLock = new();

	[Fact]
	public async Task MsBuildWorkspace_GeneratesProjectReferenceManifest_ForProjectArchitecture()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-project-manifest-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			RegisterMsBuild();
			var repositoryRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FindSchemaPath())!, "..", "..", "..", ".."));
			var analyzerProjectPath = Path.Combine(repositoryRoot, "src", "Main", "RonSijm.AnaalIJzer", "RonSijm.AnaalIJzer.csproj");
			var propsPath = Path.Combine(repositoryRoot, "build", "Settings", "RonSijm.AnaalIJzer.props");
			var targetsPath = Path.Combine(repositoryRoot, "build", "Settings", "RonSijm.AnaalIJzer.targets");

			var infrastructureProjectPath = Path.Combine(tempDirectory, "Shop.Infrastructure", "Shop.Infrastructure.csproj");
			var domainProjectPath = Path.Combine(tempDirectory, "Shop.Domain", "Shop.Domain.csproj");
			Directory.CreateDirectory(Path.GetDirectoryName(infrastructureProjectPath)!);
			Directory.CreateDirectory(Path.GetDirectoryName(domainProjectPath)!);

			await File.WriteAllTextAsync(infrastructureProjectPath, """
			                                                   <Project Sdk="Microsoft.NET.Sdk">
			                                                     <PropertyGroup>
			                                                       <TargetFramework>net10.0</TargetFramework>
			                                                       <Nullable>enable</Nullable>
			                                                     </PropertyGroup>
			                                                   </Project>
			                                                   """, cancellationToken);
			await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(infrastructureProjectPath)!, "Example.cs"), """
			                                                                                                           namespace Shop.Infrastructure;
			                                                                                                           public sealed class SqlStore { }
			                                                                                                           """, cancellationToken);

			await File.WriteAllTextAsync(domainProjectPath, $$"""
			                                           <Project Sdk="Microsoft.NET.Sdk">
			                                             <Import Project="{{propsPath}}" />
			                                             <PropertyGroup>
			                                               <TargetFramework>net10.0</TargetFramework>
			                                               <Nullable>enable</Nullable>
			                                               <EnableArchitecturalLevelAnalyzer>true</EnableArchitecturalLevelAnalyzer>
			                                               <EnableSourceLink>false</EnableSourceLink>
			                                             </PropertyGroup>
			                                             <ItemGroup>
			                                               <AdditionalFiles Include="Architecture.anl" />
			                                               <ProjectReference Include="{{Path.GetRelativePath(Path.GetDirectoryName(domainProjectPath)!, infrastructureProjectPath)}}" />
			                                               <ProjectReference Include="{{analyzerProjectPath}}" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
			                                             </ItemGroup>
			                                             <Import Project="{{targetsPath}}" />
			                                           </Project>
			                                           """, cancellationToken);
			await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(domainProjectPath)!, "Example.cs"), """
			                                                                                                   namespace Shop.Domain;
			                                                                                                   public sealed class AggregateRoot { }
			                                                                                                   """, cancellationToken);
			await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(domainProjectPath)!, "Architecture.anl"), """
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
			                                                                                                           """, cancellationToken);

			using var workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
			{
				["Configuration"] = "Release",
				["DesignTimeBuild"] = "true",
				["EnableArchitecturalLevelAnalyzer"] = "true",
				["EnableSourceLink"] = "false"
			});
			var project = await workspace.OpenProjectAsync(domainProjectPath, cancellationToken: cancellationToken);
			var compilation = await project.GetCompilationAsync(cancellationToken) ?? throw new InvalidOperationException("Could not compile the design-time project.");
			var analyzerDiagnostics = await compilation
				.WithAnalyzers([new ArchitecturalLevelAnalyzer()], project.AnalyzerOptions)
				.GetAnalyzerDiagnosticsAsync(cancellationToken);

			project.AnalyzerOptions.AdditionalFiles.Should().ContainSingle(file => Path.GetFileName(file.Path) == "AnaalIJzerReferenceManifest.txt")
				.Which.Path.Should().Contain(Path.Combine("obj", "Release", "net10.0", "AnaalIJzer", "AnaalIJzerReferenceManifest.txt"));
			analyzerDiagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.ProjectReferenceViolation)
				.Which.GetMessage().Should().Contain("Shop.Infrastructure");
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	private static void RegisterMsBuild()
	{
		lock (MsBuildRegistrationLock)
		{
			if (MSBuildLocator.CanRegister)
			{
				MSBuildLocator.RegisterDefaults();
			}
		}
	}

}

