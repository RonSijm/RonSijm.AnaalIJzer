using System.Collections.Immutable;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using RonSijm.AnaalIJzer.Config;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Config.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Tooling;

internal sealed class ProjectAnalysisHost : IDisposable
{
	private static readonly object MsBuildRegistrationLock = new();
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
		_workspaceFailures.Clear();
		var project = await _workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken);
		var projectFilePath = project.FilePath ?? projectPath;
		var projectDirectory = Path.GetDirectoryName(projectFilePath)!;
		var compilation = await project.GetCompilationAsync(cancellationToken) ?? throw new InvalidOperationException($"Could not compile {projectPath}.");
		var compilerErrors = compilation.GetDiagnostics(cancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
			.Select(diagnostic => diagnostic.ToString())
			.ToImmutableArray();
		var inlineConfigXml = ReadInlineConfigXml(compilation);
		var (configInputXml, configInputPath) = ReadConfigInput(project.AnalyzerOptions.AdditionalFiles, inlineConfigXml, projectDirectory, cancellationToken);

		var config = ArchitecturalConfigParser.Parse(
			project.AnalyzerOptions.AdditionalFiles,
			compilation,
			Path.Combine(projectDirectory, ArchitecturalConfigParser.InlineSettingsMetadataKey),
			cancellationToken);

		var analyzerDiagnostics = await compilation
			.WithAnalyzers([new ArchitecturalLevelAnalyzer()], project.AnalyzerOptions)
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
			analyzerDiagnostics,
			compilerErrors,
			[.._workspaceFailures]);
	}

	public void Dispose() => _workspace.Dispose();

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

	private static string? ReadInlineConfigXml(Compilation compilation)
	{
		foreach (var attribute in compilation.Assembly.GetAttributes())
		{
			if (!IsAssemblyMetadataAttribute(attribute.AttributeClass))
			{
				continue;
			}

			if (attribute.ConstructorArguments.Length >= 2
			    && string.Equals(attribute.ConstructorArguments[0].Value as string, ArchitecturalConfigParser.InlineSettingsMetadataKey, StringComparison.Ordinal)
			    && attribute.ConstructorArguments[1].Value is string xml)
			{
				return xml;
			}
		}

		return null;
	}

	private static (string? Xml, string? Path) ReadConfigInput(ImmutableArray<AdditionalText> additionalFiles, string? inlineConfigXml, string projectDirectory, CancellationToken cancellationToken)
	{
		var configFile = additionalFiles.FirstOrDefault(file => string.Equals(Path.GetFileName(file.Path), ArchitecturalConfigParser.ConfigFileName, StringComparison.OrdinalIgnoreCase));
		if (configFile is not null)
		{
			return (configFile.GetText(cancellationToken)?.ToString(), configFile.Path);
		}

		return (inlineConfigXml, inlineConfigXml is null ? null : Path.Combine(projectDirectory, ArchitecturalConfigParser.InlineSettingsMetadataKey));
	}

	private static bool IsAssemblyMetadataAttribute(INamedTypeSymbol? attributeClass) =>
		attributeClass is not null
		&& string.Equals(attributeClass.Name, "AssemblyMetadataAttribute", StringComparison.Ordinal)
		&& string.Equals(attributeClass.ContainingNamespace?.ToDisplayString(), "System.Reflection", StringComparison.Ordinal);
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
	ImmutableArray<Diagnostic> AnalyzerDiagnostics,
	ImmutableArray<string> CompilerErrors,
	ImmutableArray<string> WorkspaceFailures);
