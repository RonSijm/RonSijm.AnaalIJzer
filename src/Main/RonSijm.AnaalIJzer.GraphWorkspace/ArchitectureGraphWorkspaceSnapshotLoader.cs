using RonSijm.AnaalIJzer.Graphing.Loading;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Workspace;

namespace RonSijm.AnaalIJzer.GraphWorkspace;

public sealed class ArchitectureGraphWorkspaceSnapshotLoader(string configuration = "Release")
{
	private readonly string configuration = string.IsNullOrWhiteSpace(configuration) ? "Release" : configuration;
	private readonly WorkspaceAnalysisService workspace = new(configuration);

	public async Task<ArchitectureGraphSnapshot> LoadAsync(string path, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArchitectureGraphWorkspaceException("Choose an AnaalIJzer settings, project, or solution file first.");
		}

		var fullPath = Path.GetFullPath(path);
		var extension = Path.GetExtension(fullPath);
		if (IsSolutionExtension(extension))
		{
			var result = await AnalyzeSolutionAsync(fullPath, cancellationToken);
			var snapshot = ArchitectureGraphWorkspaceSnapshotFactory.CreateForSolution(fullPath, result, cancellationToken);

			return snapshot;
		}

		if (string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase))
		{
			var result = await AnalyzeProjectAsync(fullPath, cancellationToken);
			var snapshot = ArchitectureGraphWorkspaceSnapshotFactory.CreateForProject(fullPath, result, cancellationToken);

			return snapshot;
		}

		return ArchitectureGraphXmlSnapshotLoader.Load(fullPath);
	}

	public async Task<ArchitectureGraphSnapshot> LoadSolutionAsync(string solutionPath, CancellationToken cancellationToken = default)
	{
		var fullPath = Path.GetFullPath(solutionPath);
		var result = await AnalyzeSolutionAsync(fullPath, cancellationToken);
		var snapshot = ArchitectureGraphWorkspaceSnapshotFactory.CreateForSolution(fullPath, result, cancellationToken);

		return snapshot;
	}

	private async Task<ProjectAnalysisResult> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken)
	{
		var result = await ExecuteAsync(() => workspace.AnalyzeProjectAsync(projectPath, cancellationToken));

		return result;
	}

	private async Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionPath, CancellationToken cancellationToken)
	{
		var result = await ExecuteAsync(() => workspace.AnalyzeSolutionAsync(solutionPath, cancellationToken));

		return result;
	}

	private static bool IsSolutionExtension(string extension)
	{
		var result = string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase)
		             || string.Equals(extension, ".slnx", StringComparison.OrdinalIgnoreCase);

		return result;
	}

	private static async Task<T> ExecuteAsync<T>(Func<Task<T>> callback)
	{
		try
		{
			var result = await callback();

			return result;
		}
		catch (InvalidOperationException exception)
		{
			throw new ArchitectureGraphWorkspaceException(exception.Message);
		}
	}
}

