using RonSijm.AnaalIJzer.Statistics.Workspace.Scanning;

namespace RonSijm.AnaalIJzer.Statistics.Workspace.Tests.Scanning;

public sealed class StatisticsProjectDiscoveryTests
{
	[Fact]
	public void FindProjects_ExcludesBuildAndRepositoryDirectories()
	{
		var rootDirectory = Path.Combine(Path.GetTempPath(), "Anaaltomy", Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(Path.Combine(rootDirectory, "src"));
			Directory.CreateDirectory(Path.Combine(rootDirectory, "src", "obj"));
			Directory.CreateDirectory(Path.Combine(rootDirectory, ".git"));
			File.WriteAllText(Path.Combine(rootDirectory, "src", "Pizza.csproj"), "<Project />");
			File.WriteAllText(Path.Combine(rootDirectory, "src", "obj", "Ignored.csproj"), "<Project />");
			File.WriteAllText(Path.Combine(rootDirectory, ".git", "Ignored.csproj"), "<Project />");

			var projects = StatisticsProjectDiscovery.FindProjects(rootDirectory);

			projects.Should().ContainSingle().Which.Should().EndWith(Path.Combine("src", "Pizza.csproj"));
		}
		finally
		{
			if (Directory.Exists(rootDirectory))
			{
				Directory.Delete(rootDirectory, true);
			}
		}
	}
}
