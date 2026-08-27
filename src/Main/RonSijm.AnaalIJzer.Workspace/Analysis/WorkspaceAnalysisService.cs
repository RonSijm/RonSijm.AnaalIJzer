using RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model;

namespace RonSijm.AnaalIJzer.Workspace.Analysis;

internal sealed class WorkspaceAnalysisService(string configuration)
{
	private readonly string _configuration = string.IsNullOrWhiteSpace(configuration) ? "Release" : configuration;

	public async Task<ProjectAnalysisResult> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken)
	{
		var fullProjectPath = Path.GetFullPath(projectPath);
		if (!File.Exists(fullProjectPath))
		{
			throw new InvalidOperationException($"Project file not found: {fullProjectPath}");
		}

		using var host = new ProjectAnalysisHost(_configuration);
		var result = await host.AnalyzeAsync(fullProjectPath, cancellationToken);
		EnsureWorkspaceLoaded(result.WorkspaceFailures, "project");
		EnsureCompilerErrorsAbsent(result.CompilerErrors, "Project");

		return result;
	}

	public async Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionPath, CancellationToken cancellationToken)
	{
		var fullSolutionPath = Path.GetFullPath(solutionPath);
		if (!File.Exists(fullSolutionPath))
		{
			throw new InvalidOperationException($"Solution file not found: {fullSolutionPath}");
		}

		using var host = new ProjectAnalysisHost(_configuration);
		var result = await host.AnalyzeSolutionAsync(fullSolutionPath, cancellationToken);
		if (result.Projects.Length == 0)
		{
			throw new InvalidOperationException($"No C# projects were found in solution: {fullSolutionPath}");
		}

		EnsureWorkspaceLoaded(result.WorkspaceFailures, "solution");
		EnsureCompilerErrorsAbsent(result.CompilerErrors, "Solution");

		return result;
	}

	public void EnsureConfigHasRules(AnalyzerConfig config)
	{
		if (!config.Engine.HasLayers && !config.HasProjectArchitecture)
		{
			throw new InvalidOperationException("No ArchitecturalLevels config was found. Add Architecture.anl or AssemblyMetadata(\"AnaalIJzerSettings\", ...).");
		}
	}

	public ProjectAnalysisResult EnsureSolutionHasLayers(SolutionAnalysisResult result)
	{
		var representativeProject = result.FirstConfiguredProject;
		if (representativeProject is null)
		{
			throw new InvalidOperationException("No ArchitecturalLevels config was found in the solution. Add Architecture.anl or AssemblyMetadata(\"AnaalIJzerSettings\", ...) to at least one project.");
		}

		return representativeProject;
	}

	private static void EnsureWorkspaceLoaded(IReadOnlyList<string> workspaceFailures, string inputKind)
	{
		if (workspaceFailures.Count > 0)
		{
			throw new InvalidOperationException("Workspace failed to load the " + inputKind + ":" + Environment.NewLine + string.Join(Environment.NewLine, workspaceFailures));
		}
	}

	private static void EnsureCompilerErrorsAbsent(IReadOnlyList<string> compilerErrors, string label)
	{
		if (compilerErrors.Count > 0)
		{
			throw new InvalidOperationException(label + " has compiler errors:" + Environment.NewLine + string.Join(Environment.NewLine, compilerErrors));
		}
	}
}
