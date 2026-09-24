using RonSijm.AnaalIJzer.Workspace.Loading;

namespace RonSijm.AnaalIJzer.Workspace.Tests.Workspace.Loading;

public sealed class WorkspaceRestoreServiceTests
{
    [Fact]
    public void AreSolutionProjectAssetsAvailable_ReturnsTrueWhenEverySlnxProjectHasAssets()
    {
        var fixture = CreateSolutionFixture();

        try
        {
            WriteAssets(fixture.FirstProjectPath);
            WriteAssets(fixture.SecondProjectPath);

            var result = WorkspaceRestoreService.AreSolutionProjectAssetsAvailable(fixture.SolutionPath);

            result.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(fixture.RootDirectory, true);
        }
    }

    [Fact]
    public void AreSolutionProjectAssetsAvailable_ReturnsFalseWhenOneSlnxProjectNeedsRestore()
    {
        var fixture = CreateSolutionFixture();

        try
        {
            WriteAssets(fixture.FirstProjectPath);

            var result = WorkspaceRestoreService.AreSolutionProjectAssetsAvailable(fixture.SolutionPath);

            result.Should().BeFalse();
        }
        finally
        {
            Directory.Delete(fixture.RootDirectory, true);
        }
    }

    [Fact]
    public void AreSolutionProjectAssetsAvailable_ReturnsTrueWhenEverySlnProjectHasAssets()
    {
        var fixture = CreateSolutionFixture(".sln");

        try
        {
            WriteAssets(fixture.FirstProjectPath);
            WriteAssets(fixture.SecondProjectPath);
            WorkspaceRestoreService.ReadSolutionProjectPaths(fixture.SolutionPath).Should().HaveCount(2);

            var result = WorkspaceRestoreService.AreSolutionProjectAssetsAvailable(fixture.SolutionPath);

            result.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(fixture.RootDirectory, true);
        }
    }

    [Fact]
    public void CreateDotNetRestoreArguments_DisablesParallelismAndBuildServers()
    {
        var projectPath = Path.Combine("repo", "Example.csproj");

        var result = WorkspaceRestoreService.CreateDotNetRestoreArguments(projectPath);

        result.Should().Equal(
            "restore",
            projectPath,
            "--verbosity",
            "minimal",
            "--disable-parallel",
            "--disable-build-servers",
            "-p:UseSharedCompilation=false");
    }

    [Fact]
    public void GetFallbackBuildLimits_UsesOneNonReusableNode()
    {
        var result = WorkspaceRestoreService.GetFallbackBuildLimits();

        result.EnableNodeReuse.Should().BeFalse();
        result.MaxNodeCount.Should().Be(1);
    }

    private static SolutionFixture CreateSolutionFixture(string solutionExtension = ".slnx")
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "AnaalIJzer-restore-assets-" + Guid.NewGuid().ToString("N"));
        var firstProjectPath = Path.Combine(rootDirectory, "First", "First.csproj");
        var secondProjectPath = Path.Combine(rootDirectory, "Second", "Second.csproj");
        var solutionPath = Path.Combine(rootDirectory, "Fixture" + solutionExtension);
        Directory.CreateDirectory(Path.GetDirectoryName(firstProjectPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(secondProjectPath)!);
        File.WriteAllText(firstProjectPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        File.WriteAllText(secondProjectPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        File.WriteAllText(solutionPath, solutionExtension == ".slnx" ? CreateSlnxContent() : CreateSlnContent());
        var result = new SolutionFixture(rootDirectory, solutionPath, firstProjectPath, secondProjectPath);

        return result;
    }

    private static string CreateSlnxContent()
    {
        var result = """
		             <Solution>
		               <Project Path="First/First.csproj" />
		               <Project Path="Second/Second.csproj" />
		             </Solution>
		             """;

        return result;
    }

    private static string CreateSlnContent()
    {
        var result = """
		             Microsoft Visual Studio Solution File, Format Version 12.00
		             # Visual Studio Version 17
		             Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "First", "First\First.csproj", "{C29C42A5-36DA-4E4D-BDE4-FDF72C06AB7A}"
		             EndProject
		             Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Second", "Second\Second.csproj", "{7A692EB9-852A-47F9-8DA5-BE8782383636}"
		             EndProject
		             Global
		             EndGlobal
		             """;

        return result;
    }

    private static void WriteAssets(string projectPath)
    {
        var directory = Path.Combine(Path.GetDirectoryName(projectPath)!, "obj");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "project.assets.json"), "{}");
    }

    private sealed record SolutionFixture(string RootDirectory, string SolutionPath, string FirstProjectPath, string SecondProjectPath);
}