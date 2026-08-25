using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Findings;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ArchitectureHealthReportGenerator
{
	private static ImmutableArray<INamedTypeSymbol> GetDistinctProjectTypes(IReadOnlyList<ProjectAnalysisResult> projects, CancellationToken cancellationToken)
	{
		var types = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);
		foreach (var project in projects)
		{
			foreach (var type in CompilationTypeCollector.GetProjectTypes(project.Compilation, cancellationToken))
			{
				types.TryAdd(GetTypeIdentity(type), type);
			}
		}

		var result = types.Values.ToImmutableArray();

		return result;
	}

	private static string GetTypeIdentity(INamedTypeSymbol type)
	{
		var result = type.ContainingAssembly.Name + ":" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

		return result;
	}

	private static string GetProjectContext(INamedTypeSymbol type)
	{
		var result = type.ContainingAssembly?.Name ?? string.Empty;

		return result;
	}

	private static ArchitectureFinding AddProjectContext(string projectName, ArchitectureFinding finding)
	{
		var result = finding.WithContextPrefix(projectName);

		return result;
	}

	private static string AddProjectContext(string projectName, string context)
	{
		var result = string.IsNullOrWhiteSpace(context)
			? projectName
			: $"{projectName} - {context}";

		return result;
	}

	private static string GetNamespace(INamedTypeSymbol type)
	{
		var result = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();

		return result;
	}

	private static string GetTypeName(INamedTypeSymbol type)
	{
		var result = type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

		return result;
	}
}

