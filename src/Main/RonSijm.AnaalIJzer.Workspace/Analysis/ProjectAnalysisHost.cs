using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Sources;
using RonSijm.AnaalIJzer.Engine;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Workspace.Analysis;

internal sealed partial class ProjectAnalysisHost : IDisposable
{
	private readonly MSBuildWorkspace _workspace;
	private readonly List<string> _workspaceFailures = [];

	public ProjectAnalysisHost(string configuration)
	{
		RegisterMsBuild();
		_workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
		{
			["Configuration"] = configuration,
			["DesignTimeBuild"] = "true",
			["EnableArchitecturalLevelAnalyzer"] = "false",
			["EnableSourceLink"] = "false"
		});
		_workspace.WorkspaceFailed += (_, args) =>
		{
			if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
			{
				_workspaceFailures.Add(args.Diagnostic.ToString());
			}
		};
	}

	public async Task<ProjectAnalysisResult> AnalyzeAsync(string projectPath, CancellationToken cancellationToken)
	{
		EnsureRestored(projectPath);
		_workspaceFailures.Clear();
		var project = await _workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken);
		var result = await AnalyzeProjectAsync(project, projectPath, cancellationToken);

		return result with { WorkspaceFailures = [.._workspaceFailures] };
	}

	public async Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionPath, CancellationToken cancellationToken)
	{
		EnsureSolutionRestored(solutionPath);
		_workspaceFailures.Clear();
		var solution = await _workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);
		var solutionConfigFile = FindSolutionConfigFile(solutionPath, cancellationToken);
		var projects = ImmutableArray.CreateBuilder<ProjectAnalysisResult>();
		foreach (var project in solution.Projects
			         .Where(project => project.Language == LanguageNames.CSharp)
			         .OrderBy(project => project.FilePath ?? project.Name, StringComparer.OrdinalIgnoreCase))
		{
			projects.Add(await AnalyzeProjectAsync(project, project.FilePath ?? solutionPath, cancellationToken, solutionConfigFile));
		}

		return new SolutionAnalysisResult(
			solutionPath,
			Path.GetDirectoryName(solutionPath)!,
			Path.GetFileNameWithoutExtension(solutionPath),
			projects.ToImmutable(),
			[.._workspaceFailures]);
	}

	private static async Task<ProjectAnalysisResult> AnalyzeProjectAsync(Project project, string projectPath, CancellationToken cancellationToken, AdditionalText? fallbackConfigFile = null)
	{
		var projectFilePath = project.FilePath ?? projectPath;
		var projectDirectory = Path.GetDirectoryName(projectFilePath) ?? Directory.GetCurrentDirectory();
		var compilation = await project.GetCompilationAsync(cancellationToken) ?? throw new InvalidOperationException($"Could not compile {projectPath}.");
		var projectAdditionalFiles = NormalizeProjectAdditionalFiles(project.AnalyzerOptions.AdditionalFiles, projectDirectory, cancellationToken);
		var compilerErrors = compilation.GetDiagnostics(cancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
			.Select(diagnostic => diagnostic.ToString())
			.ToImmutableArray();
		var inlineConfigDocument = ArchitectureConfigurationSourceDiscovery.TryReadInlineConfigurationTextDocument(compilation, null, cancellationToken);
		var inlineConfigXml = inlineConfigDocument?.Content;
		var inlineConfigSourcePath = inlineConfigDocument?.Path;
		var supplementalConfigFiles = fallbackConfigFile is null
			? GetSupplementalConfigurationFiles(projectFilePath, projectAdditionalFiles, inlineConfigXml, cancellationToken)
			: ArchitecturalConfigParser.FindConfigFile(projectAdditionalFiles) is null && inlineConfigXml is null
				? GetSupplementalConfigurationFiles(projectFilePath, projectAdditionalFiles, inlineConfigXml, cancellationToken, fallbackConfigFile.Path)
				: GetSupplementalConfigurationFiles(projectFilePath, projectAdditionalFiles, inlineConfigXml, cancellationToken);
		var additionalFiles = GetEffectiveAdditionalFiles(project, projectAdditionalFiles, supplementalConfigFiles);
		var analyzerOptions = new AnalyzerOptions(additionalFiles, project.AnalyzerOptions.AnalyzerConfigOptionsProvider);
		var (configInputXml, configInputPath) = ReadConfigInput(additionalFiles, inlineConfigDocument, cancellationToken);

		var config = ArchitecturalConfigParser.Parse(
			additionalFiles,
			compilation,
			Path.Combine(projectDirectory, ArchitectureConfigurationDocumentLoader.InlineSettingsMetadataKey),
			cancellationToken);

		var analyzerDiagnostics = await compilation
			.WithAnalyzers([new ArchitecturalLevelAnalyzer()], analyzerOptions)
			.GetAnalyzerDiagnosticsAsync(cancellationToken);

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
			[]);
	}

	public void Dispose()
	{
		_workspace.Dispose();
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
	ImmutableArray<string> WorkspaceFailures);

internal sealed record SolutionAnalysisResult(
	string SolutionPath,
	string SolutionDirectory,
	string SolutionName,
	ImmutableArray<ProjectAnalysisResult> Projects,
	ImmutableArray<string> WorkspaceFailures)
{
	public ImmutableArray<Diagnostic> AnalyzerDiagnostics
	{
		get => [..Projects.SelectMany(project => project.AnalyzerDiagnostics)];
	}

	public ImmutableArray<string> CompilerErrors
	{
		get => [..Projects.SelectMany(project => project.CompilerErrors)];
	}

	public ProjectAnalysisResult? FirstConfiguredProject
	{
		get { return Projects.FirstOrDefault(project => project.Config.HasLayers || project.Config.HasProjectArchitecture); }
	}
}
