using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace RonSijm.AnaalIJzer.Workspace.Loading;

public sealed class WorkspaceCompilationLoader : IDisposable
{
    private readonly string _configuration;
    private readonly WorkspaceRestoreMode _restoreMode;
    private readonly string? _targetFramework;
    private readonly MSBuildWorkspace _workspace;
    private readonly List<string> _workspaceFailures = [];

    public WorkspaceCompilationLoader(string configuration, WorkspaceRestoreMode restoreMode = WorkspaceRestoreMode.Auto, string? targetFramework = null)
    {
        _configuration = string.IsNullOrWhiteSpace(configuration) ? "Release" : configuration;
        _restoreMode = restoreMode;
        _targetFramework = string.IsNullOrWhiteSpace(targetFramework) ? null : targetFramework;
        WorkspaceBuildEnvironment.Initialize();
        var properties = new Dictionary<string, string>
        {
            ["Configuration"] = _configuration,
            ["DesignTimeBuild"] = "true",
            ["EnableArchitecturalLevelAnalyzer"] = "false",
            ["EnableSourceLink"] = "false",
            ["RunAnalyzersDuringBuild"] = "false"
        };
        if (_targetFramework is not null)
        {
            properties["TargetFramework"] = _targetFramework;
        }

        _workspace = MSBuildWorkspace.Create(properties);
        _workspace.WorkspaceFailed += (_, args) => RecordWorkspaceFailure(args.Diagnostic);
    }

    public async Task<WorkspaceCompilationLoadResult> LoadProjectAsync(string projectPath, CancellationToken cancellationToken)
    {
        var fullProjectPath = Path.GetFullPath(projectPath);
        if (!File.Exists(fullProjectPath))
        {
            throw new InvalidOperationException("Project file not found: " + fullProjectPath);
        }

        _workspaceFailures.Clear();
        WorkspaceCompilationProject projectResult;
        try
        {
            WorkspaceRestoreService.EnsureProjectRestored(fullProjectPath, _configuration, _restoreMode, cancellationToken);
            var project = FindLoadedProject(fullProjectPath) ?? await _workspace.OpenProjectAsync(fullProjectPath, cancellationToken: cancellationToken);
            projectResult = await CreateProjectResultAsync(project, fullProjectPath, cancellationToken);
        }
        catch (Exception exception)
        {
            projectResult = CreateFailedProject(fullProjectPath, exception);
        }

        var result = new WorkspaceCompilationLoadResult(fullProjectPath, [projectResult], [.. _workspaceFailures]);

        return result;
    }

    public async Task<WorkspaceCompilationLoadResult> LoadProjectsAsync(IEnumerable<string> projectPaths, CancellationToken cancellationToken)
    {
        var fullProjectPaths = projectPaths
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (fullProjectPaths.Length == 0)
        {
            return new WorkspaceCompilationLoadResult(Directory.GetCurrentDirectory(), [], []);
        }

        foreach (var fullProjectPath in fullProjectPaths)
        {
            if (!File.Exists(fullProjectPath))
            {
                throw new InvalidOperationException("Project file not found: " + fullProjectPath);
            }
        }

        using var solution = TemporaryWorkspaceSolution.Create(fullProjectPaths);
        var solutionResult = await LoadSolutionAsync(solution.FilePath, cancellationToken);
        var result = new WorkspaceCompilationLoadResult(
            Path.GetDirectoryName(fullProjectPaths[0]) ?? Directory.GetCurrentDirectory(),
            solutionResult.Projects,
            solutionResult.WorkspaceFailures);

        return result;
    }

    public async Task<WorkspaceCompilationLoadResult> LoadSolutionAsync(string solutionPath, CancellationToken cancellationToken)
    {
        var fullSolutionPath = Path.GetFullPath(solutionPath);
        if (!File.Exists(fullSolutionPath))
        {
            throw new InvalidOperationException("Solution file not found: " + fullSolutionPath);
        }

        _workspaceFailures.Clear();
        var projectResults = new List<WorkspaceCompilationProject>();
        try
        {
            WorkspaceRestoreService.EnsureSolutionRestored(fullSolutionPath, _configuration, _restoreMode, cancellationToken);
            var solution = await _workspace.OpenSolutionAsync(fullSolutionPath, cancellationToken: cancellationToken);
            foreach (var project in solution.Projects.Where(project => project.Language == LanguageNames.CSharp).OrderBy(project => project.FilePath ?? project.Name, StringComparer.OrdinalIgnoreCase))
            {
                var projectPath = project.FilePath ?? fullSolutionPath;
                projectResults.Add(await CreateProjectResultAsync(project, projectPath, cancellationToken));
            }
        }
        catch (Exception exception)
        {
            projectResults.Add(CreateFailedProject(fullSolutionPath, exception));
        }

        var result = new WorkspaceCompilationLoadResult(fullSolutionPath, projectResults, [.. _workspaceFailures]);

        return result;
    }

    public void Dispose()
    {
        _workspace.Dispose();
    }

    private static async Task<WorkspaceCompilationProject> CreateProjectResultAsync(Project project, string projectPath, CancellationToken cancellationToken)
    {
        try
        {
            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation is null)
            {
                return new WorkspaceCompilationProject(projectPath, project.Name, null, GetTargetFramework(project), null, [], "Could not create a compilation.");
            }

            var targetFramework = GetTargetFramework(project);
            var repairedCompilation = FrameworkReferenceRepair.RepairIfNeeded(compilation, targetFramework);
            var compilerDiagnostics = repairedCompilation.GetDiagnostics(cancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
            return new WorkspaceCompilationProject(projectPath, project.Name, repairedCompilation.AssemblyName, targetFramework, repairedCompilation, compilerDiagnostics, null);
        }
        catch (Exception exception)
        {
            return CreateFailedProject(projectPath, exception, project.Name, GetTargetFramework(project));
        }
    }

    private static WorkspaceCompilationProject CreateFailedProject(string projectPath, Exception exception, string? projectName = null, string? targetFramework = null)
    {
        var resolvedProjectName = projectName ?? Path.GetFileNameWithoutExtension(projectPath);
        var result = new WorkspaceCompilationProject(projectPath, resolvedProjectName, null, targetFramework, null, [], exception.Message);

        return result;
    }

    private static string? GetTargetFramework(Project project)
    {
        var globalOptions = project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions;
        var result = globalOptions.TryGetValue("build_property.TargetFramework", out var targetFramework) ? targetFramework : null;

        return result;
    }

    private Project? FindLoadedProject(string fullProjectPath)
    {
        var result = _workspace.CurrentSolution.Projects.FirstOrDefault(project =>
            project.FilePath is not null
            && string.Equals(Path.GetFullPath(project.FilePath), fullProjectPath, StringComparison.OrdinalIgnoreCase));

        return result;
    }

    internal static bool IsReportableWorkspaceFailure(WorkspaceDiagnostic diagnostic)
    {
        var result = diagnostic.Kind == WorkspaceDiagnosticKind.Failure
                     && !WorkspaceFailureFilter.IsIgnorable(diagnostic.ToString());

        return result;
    }

    private void RecordWorkspaceFailure(WorkspaceDiagnostic diagnostic)
    {
        if (IsReportableWorkspaceFailure(diagnostic))
        {
            _workspaceFailures.Add(diagnostic.ToString());
        }
    }
}