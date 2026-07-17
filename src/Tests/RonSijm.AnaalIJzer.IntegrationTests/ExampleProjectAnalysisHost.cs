using System.Collections.Immutable;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;

namespace RonSijm.AnaalIJzer.IntegrationTests;

internal sealed class ExampleProjectAnalysisHost : IDisposable
{
	private const string InlineSettingsMetadataKey = "AnaalIJzerSettings";
	private static readonly object MsBuildRegistrationLock = new();
	private readonly MSBuildWorkspace _workspace;
	private readonly List<string> _workspaceFailures = [];

	public ExampleProjectAnalysisHost()
	{
		RegisterMsBuild();
		_workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
		{
			["Configuration"] = "Release",
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

	public async Task<ProjectAnalysisResult> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken)
	{
		_workspaceFailures.Clear();
		var project = await _workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken);
		var compilation = await project.GetCompilationAsync(cancellationToken) ?? throw new InvalidOperationException($"Could not compile {projectPath}.");
		var compilerErrors = compilation.GetDiagnostics(cancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
			.Select(diagnostic => diagnostic.ToString())
			.ToImmutableArray();

		var analyzerDiagnostics = await compilation
			.WithAnalyzers([new ArchitecturalLevelAnalyzer()], project.AnalyzerOptions)
			.GetAnalyzerDiagnosticsAsync(cancellationToken);

		return new ProjectAnalysisResult(
			CountAnalyzerDiagnostics(analyzerDiagnostics),
			compilerErrors,
			ReadInlineConfigXml(compilation),
			[.._workspaceFailures]);
	}

	public void Dispose()
    {
        _workspace.Dispose();
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

	private static Dictionary<string, int> CountAnalyzerDiagnostics(ImmutableArray<Diagnostic> diagnostics)
    {
        return diagnostics
            .GroupBy(diagnostic => diagnostic.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
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
			    && string.Equals(attribute.ConstructorArguments[0].Value as string, InlineSettingsMetadataKey, StringComparison.Ordinal)
			    && attribute.ConstructorArguments[1].Value is string xml)
			{
				return xml;
			}
		}

		return null;
	}

	private static bool IsAssemblyMetadataAttribute(INamedTypeSymbol? attributeClass)
    {
        return attributeClass is not null
               && string.Equals(attributeClass.Name, "AssemblyMetadataAttribute", StringComparison.Ordinal)
               && string.Equals(attributeClass.ContainingNamespace?.ToDisplayString(), "System.Reflection",
                   StringComparison.Ordinal);
    }
}

internal sealed record ProjectAnalysisResult(
	IReadOnlyDictionary<string, int> AnalyzerDiagnostics,
	ImmutableArray<string> CompilerErrors,
	string? InlineConfigXml,
	ImmutableArray<string> WorkspaceFailures);
