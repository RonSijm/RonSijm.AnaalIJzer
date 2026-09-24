using System.Globalization;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Observations;
using RonSijm.AnaalIJzer.Core.Statistics.Model;
using RonSijm.AnaalIJzer.Statistics.Roslyn.Analysis;
using RonSijm.AnaalIJzer.Workspace.Loading;

namespace RonSijm.AnaalIJzer.Statistics.Workspace.Scanning;

public sealed class StatisticsWorkspaceScanner
{
	public async Task<StatisticsScanSnapshot> ScanAsync(StatisticsWorkspaceScanRequest request, CancellationToken cancellationToken)
	{
		var inputPath = Path.GetFullPath(request.InputPath);
		var projects = new List<StatisticsProjectSnapshot>();
		var failures = new List<StatisticsScanFailure>();
		switch (request.InputKind)
		{
			case StatisticsWorkspaceInputKind.Project:
				using (var projectLoader = CreateLoader(request))
				{
					await ScanLoadResultAsync(await projectLoader.LoadProjectAsync(inputPath, cancellationToken), inputPath, request, projects, failures, cancellationToken);
				}
				break;
			case StatisticsWorkspaceInputKind.Solution:
				using (var solutionLoader = CreateLoader(request))
				{
					await ScanLoadResultAsync(await solutionLoader.LoadSolutionAsync(inputPath, cancellationToken), inputPath, request, projects, failures, cancellationToken);
				}
				break;
			case StatisticsWorkspaceInputKind.Directory:
				using (var directoryLoader = CreateLoader(request))
				{
					var projectPaths = StatisticsProjectDiscovery.FindProjects(inputPath);
					await ScanLoadResultAsync(await directoryLoader.LoadProjectsAsync(projectPaths, cancellationToken), inputPath, request, projects, failures, cancellationToken);
				}
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(request), request.InputKind, "Unsupported workspace input kind.");
		}

		var status = GetScanStatus(projects, failures);
		var result = new StatisticsScanSnapshot(inputPath, status, projects, failures);

		return result;
	}

	private static WorkspaceCompilationLoader CreateLoader(StatisticsWorkspaceScanRequest request)
	{
		var result = new WorkspaceCompilationLoader(request.Configuration, request.RestoreMode, request.TargetFramework);

		return result;
	}

	private static async Task ScanLoadResultAsync(WorkspaceCompilationLoadResult loadResult, string inputPath, StatisticsWorkspaceScanRequest request, List<StatisticsProjectSnapshot> projects, List<StatisticsScanFailure> failures, CancellationToken cancellationToken)
	{
		foreach (var workspaceFailure in loadResult.WorkspaceFailures)
		{
			failures.Add(new StatisticsScanFailure(null, "Workspace", workspaceFailure));
		}

		foreach (var project in loadResult.Projects)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (!project.IsLoaded || project.Compilation is null)
			{
				failures.Add(new StatisticsScanFailure(project.ProjectPath, "Load", project.Failure ?? "Project did not load."));
				continue;
			}

			var identity = new StatisticsProjectIdentity(
				GetRelativeProjectPath(inputPath, project.ProjectPath),
				project.ProjectName,
				project.AssemblyName,
				project.TargetFramework ?? request.TargetFramework);
			var generatedCodeScope = request.IncludeGeneratedCode
				? new GeneratedCodeAnalysisScope(GeneratedCodeAnalysisMode.IncludeAll, [])
				: GeneratedCodeAnalysisScope.Exclude;
			var snapshot = await Task.Run(() => CompilationStatisticsCollector.Collect(project.Compilation, identity, generatedCodeScope, cancellationToken), cancellationToken);
			projects.Add(snapshot);
			if (snapshot.CompilerErrorCount > 0)
			{
				failures.Add(new StatisticsScanFailure(project.ProjectPath, "Compiler", GetCompilerFailureMessage(project.CompilerDiagnostics)));
			}
		}
	}

	private static StatisticsScanStatus GetScanStatus(IReadOnlyList<StatisticsProjectSnapshot> projects, IReadOnlyList<StatisticsScanFailure> failures)
	{
		var result = projects.Count == 0
			? failures.Count == 0 ? StatisticsScanStatus.NoProjects : StatisticsScanStatus.Failed
			: failures.Count == 0 ? StatisticsScanStatus.Complete : StatisticsScanStatus.Partial;

		return result;
	}

	private static string GetRelativeProjectPath(string inputPath, string projectPath)
	{
		var rootDirectory = Directory.Exists(inputPath)
			? inputPath
			: Path.GetDirectoryName(inputPath) ?? Directory.GetCurrentDirectory();
		var result = Path.GetRelativePath(rootDirectory, projectPath);

		return result;
	}

	private static string GetCompilerFailureMessage(IReadOnlyList<Diagnostic> diagnostics)
	{
		var firstDiagnostics = diagnostics
			.Take(3)
			.Select(diagnostic => diagnostic.Id + ": " + diagnostic.GetMessage(CultureInfo.InvariantCulture));
		var message = diagnostics.Count + " compiler error(s) were observed: " + string.Join(" | ", firstDiagnostics);
		var result = diagnostics.Count > 3 ? message + " | ..." : message;

		return result;
	}
}
