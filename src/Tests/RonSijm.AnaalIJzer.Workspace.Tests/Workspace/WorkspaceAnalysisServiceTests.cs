using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.Findings;

namespace RonSijm.AnaalIJzer.Workspace.Tests.Workspace;

public sealed class WorkspaceAnalysisServiceTests
{
	[Fact]
	public async Task AnalyzeProjectAsync_Throws_WhenProjectFileDoesNotExist()
	{
		var service = new WorkspaceAnalysisService("Release");
		var projectPath = Path.Combine(Path.GetTempPath(), $"Missing-{Guid.NewGuid():N}.csproj");

		var action = async () => await service.AnalyzeProjectAsync(projectPath, TestContext.Current.CancellationToken);

		await action.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage($"Project file not found: {Path.GetFullPath(projectPath)}");
	}

	[Fact]
	public async Task AnalyzeSolutionAsync_Throws_WhenSolutionFileDoesNotExist()
	{
		var service = new WorkspaceAnalysisService("Release");
		var solutionPath = Path.Combine(Path.GetTempPath(), $"Missing-{Guid.NewGuid():N}.slnx");

		var action = async () => await service.AnalyzeSolutionAsync(solutionPath, TestContext.Current.CancellationToken);

		await action.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage($"Solution file not found: {Path.GetFullPath(solutionPath)}");
	}

	[Fact]
	public void GetSupplementalConfigurationFiles_LoadsNearestConfigAndParentSupportFiles_WhenNoConfigIsProvided()
	{
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-workspace-fallback-config-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var projectDirectory = Path.Combine(tempDirectory, "src", "Shop.Application");
			Directory.CreateDirectory(projectDirectory);
			var projectPath = Path.Combine(projectDirectory, "Shop.Application.csproj");
			File.WriteAllText(projectPath, "<Project />");

			var localConfigPath = Path.Combine(projectDirectory, "Architecture.anl");
			File.WriteAllText(localConfigPath, """
			                               <ArchitecturalLevels>
			                                 <Include path="../Architecture.anl" />
			                               </ArchitecturalLevels>
			                               """);

			var parentConfigPath = Path.Combine(Path.GetDirectoryName(projectDirectory)!, "Architecture.anl");
			File.WriteAllText(parentConfigPath, """
			                                <ArchitecturalLevels>
			                                  <Layer name="Application" />
			                                </ArchitecturalLevels>
			                                """);

			var sharedConfigPath = Path.Combine(Path.GetDirectoryName(projectDirectory)!, "Shared.anl");
			File.WriteAllText(sharedConfigPath, "<ArchitecturalLevels />");

			var result = ProjectAnalysisHost.GetSupplementalConfigurationFiles(
				projectPath,
				ImmutableArray<AdditionalText>.Empty,
				inlineConfigXml: null,
				TestContext.Current.CancellationToken);

			result.Select(file => Path.GetFullPath(file.Path)).Should().BeEquivalentTo(
			[
				localConfigPath,
				parentConfigPath,
				sharedConfigPath
			]);
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public void GetSupplementalConfigurationFiles_AddsParentSupportFiles_WhenProjectAlreadyProvidesLocalConfig()
	{
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-workspace-supplemental-config-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var projectDirectory = Path.Combine(tempDirectory, "Examples", "Example.PackageReferenceBoundaries.Data");
			Directory.CreateDirectory(projectDirectory);
			var projectPath = Path.Combine(projectDirectory, "Example.PackageReferenceBoundaries.Data.csproj");
			File.WriteAllText(projectPath, "<Project />");

			var localConfigPath = Path.Combine(projectDirectory, "Architecture.anl");
			File.WriteAllText(localConfigPath, """
			                               <ArchitecturalLevels>
			                                 <Include path="../Architecture.anl" />
			                               </ArchitecturalLevels>
			                               """);

			var parentDirectory = Path.GetDirectoryName(projectDirectory)!;
			var parentConfigPath = Path.Combine(parentDirectory, "Architecture.anl");
			File.WriteAllText(parentConfigPath, """
			                                <ArchitecturalLevels>
			                                  <ProjectArchitecture requireRecognizedProjects="true" />
			                                </ArchitecturalLevels>
			                                """);

			var sharedConfigPath = Path.Combine(parentDirectory, "SharedRules.anl");
			File.WriteAllText(sharedConfigPath, "<ArchitecturalLevels />");

			var additionalFiles = ImmutableArray.Create<AdditionalText>(new TestAdditionalText(localConfigPath));
			var result = ProjectAnalysisHost.GetSupplementalConfigurationFiles(
				projectPath,
				additionalFiles,
				inlineConfigXml: null,
				TestContext.Current.CancellationToken);

			result.Select(file => Path.GetFullPath(file.Path)).Should().BeEquivalentTo(
			[
				localConfigPath,
				parentConfigPath,
				sharedConfigPath
			]);
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public void NormalizeProjectAdditionalFiles_RebasesRelativePathsAgainstProjectDirectory()
	{
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-workspace-normalize-files-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var projectDirectory = Path.Combine(tempDirectory, "Examples", "Example.PackageReferenceBoundaries.Data");
			Directory.CreateDirectory(projectDirectory);
			var localConfigPath = Path.Combine(projectDirectory, "Architecture.anl");
			File.WriteAllText(localConfigPath, "<ArchitecturalLevels />");

			var additionalFiles = ImmutableArray.Create<AdditionalText>(new TestAdditionalText("Architecture.anl", "<ArchitecturalLevels />"));
			var result = ProjectAnalysisHost.NormalizeProjectAdditionalFiles(additionalFiles, projectDirectory, TestContext.Current.CancellationToken);

			result.Should().ContainSingle();
			result[0].Path.Should().Be(ArchitectureConfigurationSourceLookup.NormalizePath(localConfigPath));
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public void NormalizeProjectAdditionalFiles_UsesExistingRepoRelativePathsBeforeProjectRebasing()
	{
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-workspace-repo-relative-files-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var projectDirectory = Path.Combine(tempDirectory, "Examples", "Example.PackageReferenceBoundaries.Domain");
			Directory.CreateDirectory(projectDirectory);
			var localConfigPath = Path.Combine(projectDirectory, "Architecture.anl");
			File.WriteAllText(localConfigPath, "<ArchitecturalLevels />");

			var repoRelativePath = Path.GetRelativePath(tempDirectory, localConfigPath);
			var additionalFiles = ImmutableArray.Create<AdditionalText>(new TestAdditionalText(repoRelativePath, "<ArchitecturalLevels />"));
			var result = ProjectAnalysisHost.NormalizeProjectAdditionalFiles(additionalFiles, projectDirectory, TestContext.Current.CancellationToken);

			result.Should().ContainSingle();
			result[0].Path.Should().Be(ArchitectureConfigurationSourceLookup.NormalizePath(localConfigPath));
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public async Task ProjectAnalysisHost_AppliesSolutionArchitectureFileToUnconfiguredProjects()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-solution-config-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var projectPath = FindRepositoryProject("src", "Main", "RonSijm.AnaalIJzer.Application", "RonSijm.AnaalIJzer.Application.csproj");
			var solutionPath = WriteSolutionFile(tempDirectory, projectPath);
			var configPath = Path.Combine(tempDirectory, "Architecture.anl");
			await File.WriteAllTextAsync(configPath, """
			                                           <ArchitecturalLevels>
			                                             <Layer name="Tooling">
			                                               <Assembly exactName="RonSijm.AnaalIJzer.Application" />
			                                             </Layer>
			                                             <AllowedDependency from="Tooling" to="Tooling" />
			                                           </ArchitecturalLevels>
			                                           """, cancellationToken);

			using var host = new ProjectAnalysisHost("Release");
			var result = await host.AnalyzeSolutionAsync(solutionPath, cancellationToken);
			var project = result.Projects.Single(project => project.AssemblyName == "RonSijm.AnaalIJzer.Application");

			project.ConfigInputPath.Should().Be(configPath);
			project.ConfigInputXml.Should().Contain("<Layer name=\"Tooling\">");
			project.AnalyzerDiagnostics.Should().NotContain(diagnostic => diagnostic.Id == "ARCH006");
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public async Task ProjectAnalysisHost_ReportsIllegalProjectReferences_FromSolutionTopology()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-project-architecture-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var domainProjectPath = Path.Combine(tempDirectory, "Shop.Domain", "Shop.Domain.csproj");
			var webProjectPath = Path.Combine(tempDirectory, "Shop.Web", "Shop.Web.csproj");
			Directory.CreateDirectory(Path.GetDirectoryName(domainProjectPath)!);
			Directory.CreateDirectory(Path.GetDirectoryName(webProjectPath)!);

			await File.WriteAllTextAsync(domainProjectPath, """
			                                          <Project Sdk="Microsoft.NET.Sdk">
			                                            <PropertyGroup>
			                                              <TargetFramework>net10.0</TargetFramework>
			                                              <Nullable>enable</Nullable>
			                                            </PropertyGroup>
			                                          </Project>
			                                          """, cancellationToken);
			await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(domainProjectPath)!, "DomainType.cs"), """
			                                                                                                      namespace Shop.Domain;
			                                                                                                      public sealed class DomainType { }
			                                                                                                      """, cancellationToken);

			await File.WriteAllTextAsync(webProjectPath, $"""
			                                        <Project Sdk="Microsoft.NET.Sdk">
			                                          <PropertyGroup>
			                                            <TargetFramework>net10.0</TargetFramework>
			                                            <Nullable>enable</Nullable>
			                                          </PropertyGroup>
			                                          <ItemGroup>
			                                            <ProjectReference Include="{Path.GetRelativePath(Path.GetDirectoryName(webProjectPath)!, domainProjectPath)}" />
			                                          </ItemGroup>
			                                        </Project>
			                                        """, cancellationToken);
			await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(webProjectPath)!, "WebType.cs"), """
			                                                                                                namespace Shop.Web;
			                                                                                                public sealed class WebType { }
			                                                                                                """, cancellationToken);

			var solutionPath = WriteSolutionFile(tempDirectory, domainProjectPath, webProjectPath);
			var configPath = Path.Combine(tempDirectory, "Architecture.anl");
			await File.WriteAllTextAsync(configPath, """
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
			                                           """, cancellationToken);

			using var host = new ProjectAnalysisHost("Release");
			var result = await host.AnalyzeSolutionAsync(solutionPath, cancellationToken);

			result.AnalyzerDiagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.ProjectReferenceViolation)
				.Which.GetMessage().Should().Contain("Shop.Web");
			result.AnalyzerDiagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.ProjectReferenceViolation)
				.Which.GetMessage().Should().Contain("Shop.Domain");
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public async Task ProjectAnalysisHost_GeneratesPackageReferenceManifest_ForProjectArchitecture()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-package-manifest-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var repositoryRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FindSchemaPath())!, "..", "..", "..", ".."));
			var analyzerProjectPath = Path.Combine(repositoryRoot, "src", "Main", "RonSijm.AnaalIJzer", "RonSijm.AnaalIJzer.csproj");
			var propsPath = Path.Combine(repositoryRoot, "build", "Settings", "RonSijm.AnaalIJzer.props");
			var targetsPath = Path.Combine(repositoryRoot, "build", "Settings", "RonSijm.AnaalIJzer.targets");

			var domainProjectPath = Path.Combine(tempDirectory, "Shop.Domain", "Shop.Domain.csproj");
			Directory.CreateDirectory(Path.GetDirectoryName(domainProjectPath)!);

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
			                                               <PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.0" />
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
			                                                                                                               <PackagePolicy projectGroup="Domain">
			                                                                                                                 <Forbidden>
			                                                                                                                   <Package exactName="Microsoft.Extensions.Logging" />
			                                                                                                                 </Forbidden>
			                                                                                                               </PackagePolicy>
			                                                                                                             </ProjectArchitecture>
			                                                                                                           </ArchitecturalLevels>
			                                                                                                           """, cancellationToken);

			using var host = new ProjectAnalysisHost("Release");
			var result = await host.AnalyzeAsync(domainProjectPath, cancellationToken);

			result.AnalyzerDiagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.PackageReferenceViolation)
				.Which.GetMessage().Should().Contain("Microsoft.Extensions.Logging");
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	private static string FindSchemaPath()
	{
		for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
		{
			var candidate = Path.Combine(directory.FullName, "src", "Main", "RonSijm.AnaalIJzer", "Scheme", "AnaalIJzer.xsd");
			if (File.Exists(candidate))
			{
				return candidate;
			}
		}

		throw new InvalidOperationException("Could not locate AnaalIJzer.xsd.");
	}

	private static string FindRepositoryProject(params string[] pathParts)
	{
		var repositoryRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FindSchemaPath())!, "..", "..", "..", ".."));
		var segments = new string[pathParts.Length + 1];
		segments[0] = repositoryRoot;
		Array.Copy(pathParts, 0, segments, 1, pathParts.Length);
		var result = Path.Combine(segments);

		return result;
	}

	private static string WriteSolutionFile(string directory, params string[] projectPaths)
	{
		var solutionPath = Path.Combine(directory, "ExampleSolution.slnx");
		new XDocument(new XElement("Solution", projectPaths.Select(projectPath => new XElement("Project", new XAttribute("Path", projectPath))))).Save(solutionPath);

		return solutionPath;
	}

	private sealed class TestAdditionalText(string path, string? content = null) : AdditionalText
	{
		public override string Path { get; } = path;

		public override SourceText GetText(CancellationToken cancellationToken = default)
		{
			var result = content is null
				? SourceText.From(File.ReadAllText(Path))
				: SourceText.From(content);

			return result;
		}
	}
}
