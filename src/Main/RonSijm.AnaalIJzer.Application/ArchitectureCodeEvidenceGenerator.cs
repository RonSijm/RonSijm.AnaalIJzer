using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Observations;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ArchitectureCodeEvidenceGenerator
{
	public static string Append(string documentation, Compilation compilation, AnalyzerConfiguration config, ImmutableArray<Diagnostic> diagnostics, string projectDirectory, CancellationToken cancellationToken)
	{
		var types = CompilationTypeCollector.GetProjectTypes(compilation, cancellationToken);
		var matches = GetMatches(types, config);
		string? ResolveLayer(INamedTypeSymbol type)
		{
			var namespaceName = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();
			return config.Engine.FindLayer(type.Name, namespaceName, type)?.Layer.Name;
		}
		var dependencies = ProjectDependencyScanner.Scan(compilation, ResolveLayer, cancellationToken);
		var sb = new StringBuilder(documentation.TrimEnd());
		sb.AppendLine();
		sb.AppendLine();
		sb.AppendLine("## Code Evidence");
		sb.AppendLine();
		sb.AppendLine("This optional section evaluates the configured rules against the current project compilation.");
		sb.AppendLine();
		AppendRuleMatches(sb, config, matches);
		AppendVisibilityPolicyEvidence(sb, config, types, projectDirectory);
		AppendApiSurfaceEvidence(sb, compilation, config, types, projectDirectory, cancellationToken);
		AppendAllowedDependencyUsages(sb, config, dependencies, projectDirectory);
		AppendUnclassifiedTypes(sb, types, matches);
		AppendViolations(sb, diagnostics, projectDirectory);
		return sb.ToString();
	}
}

