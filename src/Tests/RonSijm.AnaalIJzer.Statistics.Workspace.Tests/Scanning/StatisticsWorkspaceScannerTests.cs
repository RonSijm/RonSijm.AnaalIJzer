using RonSijm.AnaalIJzer.Core.Statistics.Model;
using RonSijm.AnaalIJzer.Statistics.Workspace.Scanning;
using RonSijm.AnaalIJzer.Workspace.Loading;

namespace RonSijm.AnaalIJzer.Statistics.Workspace.Tests.Scanning;

public sealed class StatisticsWorkspaceScannerTests
{
	[Fact]
	public async Task ScanAsync_LoadsCSharpProjectsFromSolutionAndDirectory()
	{
		var directoryPath = Path.Combine(Path.GetTempPath(), "Anaaltomy", "WorkspaceTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directoryPath);
		try
		{
			var solutionPath = CreateSolution(directoryPath);
			var scanner = new StatisticsWorkspaceScanner();

			var solutionResult = await scanner.ScanAsync(
				new StatisticsWorkspaceScanRequest(StatisticsWorkspaceInputKind.Solution, solutionPath, restoreMode: WorkspaceRestoreMode.Always),
				TestContext.Current.CancellationToken);
			var directoryResult = await scanner.ScanAsync(
				new StatisticsWorkspaceScanRequest(StatisticsWorkspaceInputKind.Directory, directoryPath, restoreMode: WorkspaceRestoreMode.Always),
				TestContext.Current.CancellationToken);

			solutionResult.Status.Should().Be(StatisticsScanStatus.Complete);
			solutionResult.Projects.Should().HaveCount(2);
			directoryResult.Status.Should().Be(StatisticsScanStatus.Complete);
			directoryResult.Projects.Should().HaveCount(2);
			directoryResult.Projects.Select(project => project.Identity.ProjectPath).Should().OnlyContain(projectPath => projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
		}
		finally
		{
			if (Directory.Exists(directoryPath))
			{
				Directory.Delete(directoryPath, true);
			}
		}
	}

	[Fact]
	public async Task ScanAsync_KeepsUsableMeasurementsWhenTheProjectHasCompilerErrors()
	{
		var directoryPath = Path.Combine(Path.GetTempPath(), "Anaaltomy", "WorkspaceTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directoryPath);
		try
		{
			var projectPath = Path.Combine(directoryPath, "BrokenPizza.csproj");
			File.WriteAllText(projectPath, """
				<Project Sdk="Microsoft.NET.Sdk">
				  <PropertyGroup>
				    <TargetFramework>net10.0</TargetFramework>
				  </PropertyGroup>
				</Project>
				""");
			File.WriteAllText(Path.Combine(directoryPath, "BrokenPizza.cs"), "public class BrokenPizza { MissingIngredient ingredient; }");
			var scanner = new StatisticsWorkspaceScanner();

			var result = await scanner.ScanAsync(
				new StatisticsWorkspaceScanRequest(StatisticsWorkspaceInputKind.Project, projectPath, restoreMode: WorkspaceRestoreMode.Always),
				TestContext.Current.CancellationToken);

			result.Status.Should().Be(StatisticsScanStatus.Partial);
			result.Projects.Should().ContainSingle();
			result.Projects[0].CompilerErrorCount.Should().BeGreaterThan(0);
			result.Projects[0].Measurements.Should().Contain(measurement => measurement.Dimension == StatisticsDimension.TypeKind && measurement.Bucket == "Class");
			result.Failures.Should().Contain(failure => failure.Stage == "Compiler");
		}
		finally
		{
			if (Directory.Exists(directoryPath))
			{
				Directory.Delete(directoryPath, true);
			}
		}
	}

	private static string CreateSolution(string directoryPath)
	{
		CreateProject(directoryPath, "Pizza.Domain", "public sealed class Pizza { }");
		CreateProject(directoryPath, "Pizza.Ordering", "public sealed class PizzaOrder { }");
		var solutionPath = Path.Combine(directoryPath, "Pizza.slnx");
		File.WriteAllText(solutionPath, """
			<Solution>
			  <Project Path="Pizza.Domain/Pizza.Domain.csproj" />
			  <Project Path="Pizza.Ordering/Pizza.Ordering.csproj" />
			</Solution>
			""");

		return solutionPath;
	}

	private static void CreateProject(string directoryPath, string projectName, string source)
	{
		var projectDirectory = Path.Combine(directoryPath, projectName);
		Directory.CreateDirectory(projectDirectory);
		File.WriteAllText(Path.Combine(projectDirectory, projectName + ".csproj"), """
			<Project Sdk="Microsoft.NET.Sdk">
			  <PropertyGroup>
			    <TargetFramework>net10.0</TargetFramework>
			  </PropertyGroup>
			</Project>
			""");
		File.WriteAllText(Path.Combine(projectDirectory, projectName + ".cs"), source);
	}
}
