using RonSijm.AnaalIJzer.Model;

namespace RonSijm.AnaalIJzer.Application;

internal sealed class ApplicationWorkspaceAnalysisService(string configuration)
{
	private readonly WorkspaceAnalysisService workspace = new(configuration);

	public async Task<ProjectAnalysisResult> AnalyzeProjectAsync(ApplicationRequest request, CancellationToken cancellationToken)
	{
		var result = await AnalyzeProjectAsync(request.InputPaths[0], cancellationToken);

		return result;
	}

	public async Task<ProjectAnalysisResult> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken)
	{
		var result = await ExecuteAsync(() => workspace.AnalyzeProjectAsync(projectPath, cancellationToken));

		return result;
	}

	public async Task<SolutionAnalysisResult> AnalyzeSolutionAsync(ApplicationRequest request, CancellationToken cancellationToken)
	{
		var result = await AnalyzeSolutionAsync(request.InputPaths[0], cancellationToken);

		return result;
	}

	public async Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionPath, CancellationToken cancellationToken)
	{
		var result = await ExecuteAsync(() => workspace.AnalyzeSolutionAsync(solutionPath, cancellationToken));

		return result;
	}

	public void EnsureConfigHasRules(AnalyzerConfig config)
	{
		Execute(() => workspace.EnsureConfigHasRules(config));
	}

	public ProjectAnalysisResult EnsureSolutionHasLayers(SolutionAnalysisResult result)
	{
		var representativeProject = Execute(() => workspace.EnsureSolutionHasLayers(result));

		return representativeProject;
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
			throw new ApplicationOperationException(exception.Message);
		}
	}

	private static T Execute<T>(Func<T> callback)
	{
		try
		{
			var result = callback();

			return result;
		}
		catch (InvalidOperationException exception)
		{
			throw new ApplicationOperationException(exception.Message);
		}
	}

	private static void Execute(Action callback)
	{
		try
		{
			callback();
		}
		catch (InvalidOperationException exception)
		{
			throw new ApplicationOperationException(exception.Message);
		}
	}
}

