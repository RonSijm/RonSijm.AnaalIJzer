using System.Collections.Immutable;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;
using RonSijm.AnaalIJzer.EditorRuntime.Editor.Snapshots;
using RonSijm.AnaalIJzer.Workspace.Analysis;

namespace RonSijm.AnaalIJzer.IntegrationTests;

internal sealed class ExampleProjectAnalysisHost : IDisposable
{
	private static readonly object MsBuildRegistrationLock = new();

	public ExampleProjectAnalysisHost()
	{
		RegisterMsBuild();
	}

	public async Task<ExampleProjectAnalysisResult> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken)
	{
		using var host = new ProjectAnalysisHost("Release");
		var analysis = await host.AnalyzeAsync(projectPath, cancellationToken);
		var analyzerDiagnosticMessages = analysis.AnalyzerDiagnostics
			.Select(diagnostic => diagnostic.ToString())
			.ToImmutableArray();
		var result = new ExampleProjectAnalysisResult(
			analysis.Config.HasLayers || analysis.Config.HasProjectArchitecture,
			CountAnalyzerDiagnostics(analysis.AnalyzerDiagnostics),
			analyzerDiagnosticMessages,
			analysis.CompilerErrors,
			analysis.InlineConfigXml,
			analysis.WorkspaceFailures);

		return result;
	}

	public async Task<ArchitectureEditorSnapshot> CreateEditorSnapshotAsync(string projectPath, string documentFileName, CancellationToken cancellationToken)
	{
		RegisterMsBuild();

		var failures = ImmutableArray.CreateBuilder<string>();
		using var workspace = MSBuildWorkspace.Create(CreateGlobalProperties("Debug", enableAnalyzer: false, designTimeBuild: true));
		workspace.WorkspaceFailed += (_, args) => failures.Add(args.Diagnostic.Message);

		var project = await workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken);
		if (failures.Count > 0)
		{
			throw new InvalidOperationException("Workspace failed to load the example project:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
		}

		var document = project.Documents.SingleOrDefault(candidate => string.Equals(Path.GetFileName(candidate.FilePath), documentFileName, StringComparison.OrdinalIgnoreCase));
		if (document is null)
		{
			throw new InvalidOperationException("Could not find document '" + documentFileName + "' in example project '" + projectPath + "'.");
		}

		var result = await ArchitectureEditorSnapshotService.CreateSnapshotAsync(document, project.AnalyzerOptions.AdditionalFiles, cancellationToken: cancellationToken);

		return result;
	}

	public void Dispose()
	{
	}

	private static Dictionary<string, string> CreateGlobalProperties(string configuration, bool enableAnalyzer, bool designTimeBuild)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["Configuration"] = configuration,
			["DesignTimeBuild"] = designTimeBuild ? "true" : "false",
			["EnableArchitecturalLevelAnalyzer"] = enableAnalyzer ? "true" : "false",
			["EnableSourceLink"] = "false",
			["UseSharedCompilation"] = "false"
		};

		return result;
	}

	private static void RegisterMsBuild()
	{
		lock (MsBuildRegistrationLock)
		{
			if (MSBuildLocator.CanRegister)
			{
				MSBuildLocator.RegisterDefaults();
			}
		}
	}

	private static Dictionary<string, int> CountAnalyzerDiagnostics(IEnumerable<Diagnostic> diagnostics)
	{
		var result = diagnostics
			.GroupBy(diagnostic => diagnostic.Id, StringComparer.Ordinal)
			.ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

		return result;
	}

}

internal sealed record ExampleProjectAnalysisResult(
	bool HasConfiguration,
	IReadOnlyDictionary<string, int> AnalyzerDiagnostics,
	ImmutableArray<string> AnalyzerDiagnosticMessages,
	ImmutableArray<string> CompilerErrors,
	string? InlineConfigXml,
	ImmutableArray<string> WorkspaceFailures);
