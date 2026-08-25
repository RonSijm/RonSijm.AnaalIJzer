using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Exceptions;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.ObservedDependencies;
using RonSijm.AnaalIJzer.Workspace;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.GraphWorkspace;

internal static partial class ArchitectureGraphWorkspaceSnapshotFactory
{
	private static ImmutableArray<ArchitectureGraphExceptionReview> CreateExceptionReviews(ImmutableArray<ProjectAnalysisResult> projects, AnalyzerConfiguration config, CancellationToken cancellationToken)
	{
		var reviews = ImmutableArray.CreateBuilder<ArchitectureGraphExceptionReview>();
		foreach (var review in config.ExceptionReviews)
		{
			reviews.Add(new ArchitectureGraphExceptionReview(
				review.OwnerLayerPath,
				review.MatcherKind,
				review.MatcherLabel,
				review.Status.ToString(),
				review.Message,
				review.Metadata.Reason,
				review.Metadata.Owner,
				review.Metadata.ExpiresOnText,
				review.XmlPath,
				review.XmlLineNumber,
				review.XmlLinePosition));
		}

		var seenStaleKeys = new HashSet<string>(StringComparer.Ordinal);
		var types = GetDistinctProjectTypes(projects, cancellationToken);
		foreach (var definition in config.ExceptionDefinitions)
		{
			if (!definition.IsActive)
			{
				continue;
			}

			if (types.Any(type => definition.Matcher.TryMatch(type.Name, GetNamespace(type), type) is not null))
			{
				continue;
			}

			var staleKey = definition.XmlPath + "|" + definition.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
			if (!seenStaleKeys.Add(staleKey))
			{
				continue;
			}

			reviews.Add(new ArchitectureGraphExceptionReview(
				definition.OwnerLayerPath,
				definition.MatcherKind,
				definition.MatcherLabel,
				ArchitectureExceptionStatus.Stale.ToString(),
				ArchitectureExceptionEvaluator.CreateStaleMessage(definition, projects.Length > 1 ? "solution" : "project"),
				definition.Metadata.Reason,
				definition.Metadata.Owner,
				definition.Metadata.ExpiresOnText,
				definition.XmlPath,
				definition.XmlLineNumber,
				definition.XmlLinePosition));
		}

		var result = reviews
			.OrderBy(review => review.SourcePath, StringComparer.OrdinalIgnoreCase)
			.ThenBy(review => review.XmlLineNumber)
			.ThenBy(review => review.MatcherLabel, StringComparer.Ordinal)
			.ToImmutableArray();

		return result;
	}

	private static ImmutableArray<INamedTypeSymbol> GetDistinctProjectTypes(IReadOnlyList<ProjectAnalysisResult> projects, CancellationToken cancellationToken)
	{
		var types = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);
		foreach (var project in projects)
		{
			foreach (var type in CompilationTypeCollector.GetProjectTypes(project.Compilation, cancellationToken))
			{
				var identity = type.ContainingAssembly.Name + ":" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
				types.TryAdd(identity, type);
			}
		}

		var result = types.Values.ToImmutableArray();

		return result;
	}
}

