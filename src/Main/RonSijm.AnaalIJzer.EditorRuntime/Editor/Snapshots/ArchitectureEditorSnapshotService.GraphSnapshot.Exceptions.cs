using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Exceptions;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.ObservedDependencies;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;

namespace RonSijm.AnaalIJzer.EditorRuntime.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static ImmutableArray<ArchitectureGraphExceptionReview> CreateGraphExceptionReviews(ProjectAnalyzerConfig config, Compilation compilation, CancellationToken cancellationToken)
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
		var types = GetProjectTypes(compilation, cancellationToken);
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
				ArchitectureExceptionEvaluator.CreateStaleMessage(definition, "project"),
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

	private static ImmutableArray<INamedTypeSymbol> GetProjectTypes(Compilation compilation, CancellationToken cancellationToken)
	{
		var result = CompilationTypeCollector.GetProjectTypes(compilation, cancellationToken).ToImmutableArray();

		return result;
	}

	private static string GetNamespace(INamedTypeSymbol type)
	{
		var result = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();

		return result;
	}
}
