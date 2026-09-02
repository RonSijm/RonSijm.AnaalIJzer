using RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model;
using RonSijm.AnaalIJzer.Workspace.Analysis.ConfigurationFixes;
using RonSijm.AnaalIJzer.Workspace.Analysis;

namespace RonSijm.AnaalIJzer.Application;

internal sealed class ApplicationWorkspaceAnalysisService(string configuration)
{
	private readonly WorkspaceAnalysisService _workspace = new(configuration);

	public async Task<ProjectAnalysisResult> AnalyzeProjectAsync(ApplicationRequest request, CancellationToken cancellationToken)
	{
		var result = await AnalyzeProjectAsync(request.InputPaths[0], cancellationToken);

		return result;
	}

	private async Task<ProjectAnalysisResult> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken)
	{
		var result = await ExecuteAsync(() => _workspace.AnalyzeProjectAsync(projectPath, cancellationToken));

		return result;
	}

	public async Task<SolutionAnalysisResult> AnalyzeSolutionAsync(ApplicationRequest request, CancellationToken cancellationToken)
	{
		var result = await AnalyzeSolutionAsync(request.InputPaths[0], cancellationToken);

		return result;
	}

	private async Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionPath, CancellationToken cancellationToken)
	{
		var result = await ExecuteAsync(() => _workspace.AnalyzeSolutionAsync(solutionPath, cancellationToken));

		return result;
	}

	public void EnsureConfigHasRules(AnalyzerConfig config)
	{
		Execute(() => _workspace.EnsureConfigHasRules(config));
	}

	public ProjectAnalysisResult EnsureSolutionHasLayers(SolutionAnalysisResult result)
	{
		var representativeProject = Execute(() => _workspace.EnsureSolutionHasLayers(result));

		return representativeProject;
	}

	public async Task<ConfigurationFixCollectionResult> FindProjectConfigurationFixesAsync(ApplicationRequest request, CancellationToken cancellationToken)
	{
		var result = await ExecuteAsync(() => _workspace.FindProjectConfigurationFixesAsync(request.InputPaths[0], cancellationToken));

		return result;
	}

	public async Task<ConfigurationFixCollectionResult> FindSolutionConfigurationFixesAsync(ApplicationRequest request, CancellationToken cancellationToken)
	{
		var result = await ExecuteAsync(() => _workspace.FindSolutionConfigurationFixesAsync(request.InputPaths[0], cancellationToken));

		return result;
	}

	public async Task<ConfigurationFixApplyResult> ApplyProjectConfigurationFixAsync(ApplicationRequest request, CancellationToken cancellationToken)
	{
		var result = await ExecuteAsync(() => _workspace.ApplyProjectConfigurationFixAsync(request.InputPaths[0], request.FixId!, cancellationToken));

		return result;
	}

	public async Task<ConfigurationFixApplyResult> ApplySolutionConfigurationFixAsync(ApplicationRequest request, CancellationToken cancellationToken)
	{
		var result = await ExecuteAsync(() => _workspace.ApplySolutionConfigurationFixAsync(request.InputPaths[0], request.FixId!, cancellationToken));

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

