using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.GraphWorkspace.Tests.Workspace;

public sealed class ArchitectureGraphWorkspaceSnapshotLoaderTests
{
	[Fact]
	public async Task LoadAsync_LoadsSolutionGraphAndCodeEvidence()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-graph-solution-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var projectPath = FindRepositoryProject("Examples", "Scenarios", "Example.RepositoryQuerySurface", "Example.RepositoryQuerySurface.csproj");
			var solutionPath = WriteSolutionFile(tempDirectory, projectPath);
			var snapshot = await new ArchitectureGraphWorkspaceSnapshotLoader("Release").LoadAsync(solutionPath, cancellationToken);

			snapshot.HasConfiguration.Should().BeTrue();
			snapshot.ConfigurationSource.Path.Should().EndWith("Architecture.anl");
			snapshot.Layers.Select(layer => layer.Path).Should().Contain(["Presentation", "Application", "Persistence", "QuerySurface", "Projection"]);
			snapshot.Evidence.HasEvidence.Should().BeTrue();
			snapshot.Evidence.Types.Should().Contain(type => type.LayerPath == "Application" && type.TypeName == "OrderService");
			snapshot.Evidence.Dependencies.Should().Contain(dependency =>
				dependency.DiagnosticId == "ARCH001"
				&& dependency.CallerTypeName == "OrderService"
				&& dependency.DependencyTypeName == "OrderQuery"
				&& dependency.Site == "Local");
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
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

	private static string WriteSolutionFile(string directory, params string[] projectPaths)
	{
		var solutionPath = Path.Combine(directory, "ExampleSolution.slnx");
		new XDocument(new XElement("Solution", projectPaths.Select(projectPath => new XElement("Project", new XAttribute("Path", projectPath))))).Save(solutionPath);

		return solutionPath;
	}
}
