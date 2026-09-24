using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using RonSijm.AnaalIJzer.Core.BuildMetadata;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Sources;
using RonSijm.AnaalIJzer.Core.ProjectArchitecture.SolutionTopology;
using RonSijm.AnaalIJzer.Engine;
using RonSijm.AnaalIJzer.Workspace.Loading;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Workspace.Analysis;

internal sealed partial class ProjectAnalysisHost : IDisposable
{
    private readonly string _configuration;
    private readonly MSBuildWorkspace _workspace;
    private readonly List<string> _workspaceFailures = [];

    public ProjectAnalysisHost(string configuration)
    {
        _configuration = string.IsNullOrWhiteSpace(configuration) ? "Release" : configuration;
        WorkspaceBuildEnvironment.Initialize();
        _workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
        {
            ["Configuration"] = _configuration,
            ["DesignTimeBuild"] = "true",
            ["EnableArchitecturalLevelAnalyzer"] = "false",
            ["EnableSourceLink"] = "false"
        });
        _workspace.WorkspaceFailed += (_, args) =>
        {
            var diagnosticText = args.Diagnostic.ToString();
            if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure && !IsIgnorableWorkspaceFailure(diagnosticText))
            {
                _workspaceFailures.Add(diagnosticText);
            }
        };
    }

    public async Task<ProjectAnalysisResult> AnalyzeAsync(string projectPath, CancellationToken cancellationToken)
    {
        var fullProjectPath = Path.GetFullPath(projectPath);
        WorkspaceRestoreService.EnsureProjectRestored(fullProjectPath, _configuration, WorkspaceRestoreMode.Auto, cancellationToken);
        _workspaceFailures.Clear();
        var project = FindLoadedProject(fullProjectPath) ?? await _workspace.OpenProjectAsync(fullProjectPath, cancellationToken: cancellationToken);
        var result = await AnalyzeProjectAsync(project, fullProjectPath, cancellationToken);

        return result with { WorkspaceFailures = [.. _workspaceFailures] };
    }

    private Project? FindLoadedProject(string fullProjectPath)
    {
        var result = _workspace.CurrentSolution.Projects.FirstOrDefault(project =>
            project.FilePath is not null
            && string.Equals(Path.GetFullPath(project.FilePath), fullProjectPath, StringComparison.OrdinalIgnoreCase));

        return result;
    }

    public async Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionPath, CancellationToken cancellationToken)
    {
        WorkspaceRestoreService.EnsureSolutionRestored(solutionPath, _configuration, WorkspaceRestoreMode.Auto, cancellationToken);
        _workspaceFailures.Clear();
        var solution = await _workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);
        ThrowIfWorkspaceLoadFailed("solution");
        var solutionConfigFile = FindSolutionConfigFile(solutionPath, cancellationToken);
        var projects = ImmutableArray.CreateBuilder<ProjectAnalysisResult>();
        foreach (var project in solution.Projects
                     .Where(project => project.Language == LanguageNames.CSharp)
                     .OrderBy(project => project.FilePath ?? project.Name, StringComparer.OrdinalIgnoreCase))
        {
            projects.Add(await AnalyzeProjectAsync(project, project.FilePath ?? solutionPath, cancellationToken, solutionConfigFile));
        }

        var projectReferences = CollectSolutionProjectReferences(solution);

        return new SolutionAnalysisResult(
            solutionPath,
            Path.GetDirectoryName(solutionPath)!,
            Path.GetFileNameWithoutExtension(solutionPath),
            projects.ToImmutable(),
            [.. _workspaceFailures],
            projectReferences);
    }

    private static ImmutableArray<SolutionProjectReference> CollectSolutionProjectReferences(Solution solution)
    {
        var references = ImmutableArray.CreateBuilder<SolutionProjectReference>();
        var projectsByFilePath = solution.Projects
            .Where(project => project.Language == LanguageNames.CSharp && !string.IsNullOrWhiteSpace(project.FilePath))
            .GroupBy(project => Path.GetFullPath(project.FilePath!), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var sourceProject in solution.Projects.Where(project => project.Language == LanguageNames.CSharp))
        {
            if (string.IsNullOrWhiteSpace(sourceProject.FilePath))
            {
                continue;
            }

            foreach (var targetProjectPath in ReadDirectProjectReferences(sourceProject.FilePath))
            {
                if (!projectsByFilePath.TryGetValue(Path.GetFullPath(targetProjectPath), out var targetProject))
                {
                    continue;
                }

                references.Add(new SolutionProjectReference(
                    sourceProject.FilePath ?? sourceProject.Name,
                    sourceProject.Name,
                    targetProject.FilePath ?? targetProject.Name,
                    targetProject.Name));
            }
        }

        var result = references
            .GroupBy(reference => reference.SourceProjectPath + "\u001f" + reference.TargetProjectPath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(reference => reference.SourceProjectName, StringComparer.Ordinal)
            .ThenBy(reference => reference.TargetProjectName, StringComparer.Ordinal)
            .ToImmutableArray();

        return result;
    }

    private static async Task<ProjectAnalysisResult> AnalyzeProjectAsync(Project project, string projectPath, CancellationToken cancellationToken, AdditionalText? fallbackConfigFile = null)
    {
        var projectFilePath = project.FilePath ?? projectPath;
        var projectDirectory = Path.GetDirectoryName(projectFilePath) ?? Directory.GetCurrentDirectory();
        var compilation = await project.GetCompilationAsync(cancellationToken) ?? throw new InvalidOperationException($"Could not compile {projectPath}.");
        var projectAdditionalFiles = NormalizeProjectAdditionalFiles(project.AnalyzerOptions.AdditionalFiles, projectDirectory, cancellationToken);
        var inlineConfigDocument = ArchitectureConfigurationSourceDiscovery.TryReadInlineConfigurationTextDocument(compilation, null, cancellationToken);
        var inlineConfigXml = inlineConfigDocument?.Content;
        var inlineConfigSourcePath = inlineConfigDocument?.Path;
        var supplementalConfigFiles = fallbackConfigFile is null
            ? GetSupplementalConfigurationFiles(projectFilePath, projectAdditionalFiles, inlineConfigXml, cancellationToken)
            : ArchitecturalConfigParser.FindConfigFile(projectAdditionalFiles) is null && inlineConfigXml is null
                ? GetSupplementalConfigurationFiles(projectFilePath, projectAdditionalFiles, inlineConfigXml, cancellationToken, fallbackConfigFile.Path)
                : GetSupplementalConfigurationFiles(projectFilePath, projectAdditionalFiles, inlineConfigXml, cancellationToken);
        var referenceManifest = CreateProjectReferenceManifest(project);
        var additionalFiles = GetEffectiveAdditionalFiles(projectAdditionalFiles, supplementalConfigFiles, referenceManifest, projectFilePath);
        var analyzerOptions = new AnalyzerOptions(additionalFiles, project.AnalyzerOptions.AnalyzerConfigOptionsProvider);
        var (configInputXml, configInputPath) = ReadConfigInput(additionalFiles, inlineConfigDocument, cancellationToken);

        var config = ArchitecturalConfigParser.Parse(
            additionalFiles,
            compilation,
            Path.Combine(projectDirectory, ArchitectureConfigurationDocumentLoader.InlineSettingsMetadataKey),
            cancellationToken);

        var analyzer = new ArchitecturalLevelAnalyzer();
        var analyzerDiagnosticIds = analyzer.SupportedDiagnostics.Select(descriptor => descriptor.Id).ToImmutableHashSet(StringComparer.Ordinal);
        var allDiagnostics = await compilation
            .WithAnalyzers([analyzer], analyzerOptions)
            .GetAllDiagnosticsAsync(cancellationToken);
        var analyzerDiagnostics = allDiagnostics
            .Where(diagnostic => analyzerDiagnosticIds.Contains(diagnostic.Id))
            .ToImmutableArray();
        var compilerErrors = allDiagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && !analyzerDiagnosticIds.Contains(diagnostic.Id))
            .Select(diagnostic => diagnostic.ToString())
            .ToImmutableArray();

        return new ProjectAnalysisResult(
            projectFilePath,
            projectDirectory,
            compilation.AssemblyName,
            compilation,
            config,
            inlineConfigXml,
            configInputXml,
            configInputPath,
            inlineConfigSourcePath,
            analyzerDiagnostics,
            compilerErrors,
            [],
            referenceManifest ?? ArchitectureReferenceManifest.Empty);
    }

    public void Dispose()
    {
        _workspace.Dispose();
    }

    internal IReadOnlyList<string> WorkspaceFailures => _workspaceFailures;

    internal static bool IsIgnorableWorkspaceFailure(string diagnosticText)
    {
        var result = WorkspaceFailureFilter.IsIgnorable(diagnosticText);

        return result;
    }

    private void ThrowIfWorkspaceLoadFailed(string inputKind)
    {
        if (_workspaceFailures.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException("Workspace failed to load the " + inputKind + ":" + Environment.NewLine + string.Join(Environment.NewLine, _workspaceFailures));
    }
}

internal sealed record ProjectAnalysisResult(
    string ProjectPath,
    string ProjectDirectory,
    string? AssemblyName,
    Compilation Compilation,
    AnalyzerConfiguration Config,
    string? InlineConfigXml,
    string? ConfigInputXml,
    string? ConfigInputPath,
    string? InlineConfigSourcePath,
    ImmutableArray<Diagnostic> AnalyzerDiagnostics,
    ImmutableArray<string> CompilerErrors,
    ImmutableArray<string> WorkspaceFailures,
    ArchitectureReferenceManifest ReferenceManifest = default);

internal sealed record SolutionAnalysisResult(
    string SolutionPath,
    string SolutionDirectory,
    string SolutionName,
    ImmutableArray<ProjectAnalysisResult> Projects,
    ImmutableArray<string> WorkspaceFailures,
    ImmutableArray<SolutionProjectReference> ProjectReferences = default)
{
    public ImmutableArray<SolutionProjectReference> EffectiveProjectReferences
    {
        get => ProjectReferences.IsDefault ? [] : ProjectReferences;
    }

    public ImmutableArray<Diagnostic> AnalyzerDiagnostics
    {
        get => [.. Projects.SelectMany(project => project.AnalyzerDiagnostics)];
    }

    public ImmutableArray<string> CompilerErrors
    {
        get => [.. Projects.SelectMany(project => project.CompilerErrors)];
    }

    public ProjectAnalysisResult? FirstConfiguredProject
    {
        get { return Projects.FirstOrDefault(project => project.Config.HasConfiguredRules); }
    }
}