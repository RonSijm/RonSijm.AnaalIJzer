using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Engine;
using RonSijm.AnaalIJzer.Workspace.Analysis;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ApplicationConfigurationGenerator
{
	public static string ReadSchema()
	{
		using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(SchemaResourceName)
			?? throw new InvalidOperationException($"Embedded resource not found: {SchemaResourceName}");
		using var reader = new StreamReader(stream);
		var result = reader.ReadToEnd();

		return result;
	}

	public static Task<ImmutableArray<Diagnostic>> ValidateAsync(Compilation compilation, string configuration, string configurationPath, CancellationToken cancellationToken)
	{
		var options = new AnalyzerOptions([new GeneratedAdditionalText(configurationPath, configuration)]);
		var result = compilation.WithAnalyzers([new ArchitecturalLevelAnalyzer()], options).GetAnalyzerDiagnosticsAsync(cancellationToken);

		return result;
	}

	public static async Task<ImmutableArray<Diagnostic>> ValidateAsync(SolutionAnalysisResult solution, string configuration, string configurationPath, CancellationToken cancellationToken)
	{
		var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
		foreach (var project in solution.Projects)
		{
			var projectDiagnostics = await ValidateAsync(project.Compilation, configuration, configurationPath, cancellationToken);
			diagnostics.AddRange(projectDiagnostics);
		}

		var result = diagnostics.ToImmutable();

		return result;
	}

	public static AnalyzerConfiguration Parse(Compilation compilation, string configuration, string configurationPath, CancellationToken cancellationToken)
	{
		var additionalText = new GeneratedAdditionalText(configurationPath, configuration);
		var result = ArchitecturalConfigParser.Parse([additionalText], compilation, configurationPath, cancellationToken);

		return result;
	}

	private static IEnumerable<INamedTypeSymbol> DistinctTypes(IEnumerable<INamedTypeSymbol> types)
	{
		var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
		foreach (var type in types)
		{
			if (seen.Add(type))
			{
				yield return type;
			}
		}
	}
}

