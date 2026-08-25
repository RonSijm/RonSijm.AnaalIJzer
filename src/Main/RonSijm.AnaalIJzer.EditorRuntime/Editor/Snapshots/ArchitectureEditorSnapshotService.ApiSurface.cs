using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Analysis.ApiSurface.Declarations;
using RonSijm.AnaalIJzer.Analysis.ApiSurface.Model;
using RonSijm.AnaalIJzer.Analysis.ApiSurface.Traversal;
using RonSijm.AnaalIJzer.Indicators;
using RonSijm.AnaalIJzer.SymbolFacts;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;

namespace RonSijm.AnaalIJzer.EditorRuntime.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static void AddApiSurfaceIndicators(
		SyntaxNode node,
		SemanticModel semanticModel,
		Compilation compilation,
		ProjectAnalyzerConfig config,
		ImmutableArray<ArchitectureApiSurfaceIndicator>.Builder indicators,
		HashSet<ISymbol> analyzedSymbols,
		ConcurrentDictionary<INamedTypeSymbol, ImmutableArray<ExposureMemberTypeReference>> transitiveMemberCache,
		CancellationToken cancellationToken)
	{
		foreach (var symbol in GetDeclaredSymbols(node, semanticModel, cancellationToken))
		{
			var normalizedSymbol = symbol is IMethodSymbol { PartialDefinitionPart: not null } method
				? method.PartialDefinitionPart
				: symbol;
			if (!analyzedSymbols.Add(normalizedSymbol)
			    || normalizedSymbol.IsImplicitlyDeclared
			    || !normalizedSymbol.IsEffectivelyExternallyVisible())
			{
				continue;
			}

			var ownerType = normalizedSymbol as INamedTypeSymbol ?? normalizedSymbol.ContainingType;
			if (ownerType is null)
			{
				continue;
			}

			var callerLayer = config.Engine.FindLayer(ownerType.Name, ownerType.ContainingNamespace?.ToDisplayString() ?? string.Empty, ownerType);
			if (callerLayer is null || callerLayer.Value.Layer.IsForbidden)
			{
				continue;
			}

			var apiMemberName = normalizedSymbol is INamedTypeSymbol
				? normalizedSymbol.Name
				: $"{normalizedSymbol.ContainingType?.Name}.{normalizedSymbol.Name}";
			var maxTransitiveDepth = config.Engine.GetTransitiveExposureMaxDepth(callerLayer.Value);
			foreach (var reference in ApiSurfaceDeclarationWalker.GetReferences(normalizedSymbol, compilation, cancellationToken))
			{
				if (reference.Location.SourceTree != node.SyntaxTree)
				{
					continue;
				}

				var dependencyType = reference.Type.OriginalDefinition;
				var dependencyLayer = config.Engine.FindLayer(dependencyType.Name, dependencyType.ContainingNamespace?.ToDisplayString() ?? string.Empty, dependencyType);
				var evaluation = config.Engine.EvaluateApiSurfacePolicies(callerLayer.Value, dependencyLayer, dependencyType.Name, reference.Site);
				if (evaluation is not null)
				{
					var policy = evaluation.Value.Policy;
					indicators.Add(new ArchitectureApiSurfaceIndicator(
						reference.Location.SourceSpan,
						apiMemberName,
						ownerType.Name,
						callerLayer.Value.Layer.Name,
						dependencyType.Name,
						dependencyLayer?.Layer.Name ?? "unrecognized",
						reference.Site,
						evaluation.Value.Reason,
						policy.Description,
						policy.XmlPath,
						policy.XmlLineNumber));
					continue;
				}

				if (maxTransitiveDepth == 0 || dependencyLayer is null)
				{
					continue;
				}

				var transitiveViolation = TransitiveExposureWalker.FindFirstViolation(
					dependencyType,
					apiMemberName,
					maxTransitiveDepth,
					transitiveMemberCache,
					(candidateType, site, depth) =>
					{
						var dependencyLayer = config.Engine.FindLayer(candidateType.Name, candidateType.ContainingNamespace?.ToDisplayString() ?? string.Empty, candidateType);
						var evaluation = config.Engine.EvaluateApiSurfacePolicies(callerLayer.Value, dependencyLayer, candidateType.Name, site, depth);
						var result = (evaluation, dependencyLayer?.Layer.Name);

						return result;
					},
					cancellationToken);
				if (transitiveViolation is null)
				{
					continue;
				}

				var transitivePolicy = transitiveViolation.Value.Evaluation.Policy;
				var path = transitiveViolation.Value.Path.ToDisplayText(transitiveViolation.Value.ForbiddenType.Name);
				var segments = transitiveViolation.Value.Path.Segments
					.Select(segment => new ArchitectureExposurePathSegment(
						segment.DisplayName,
						segment.Location?.SourceTree?.FilePath,
						segment.Location?.IsInSource == true ? segment.Location.SourceSpan : null))
					.ToImmutableArray();
				indicators.Add(new ArchitectureApiSurfaceIndicator(
					reference.Location.SourceSpan,
					apiMemberName,
					ownerType.Name,
					callerLayer.Value.Layer.Name,
					transitiveViolation.Value.ForbiddenType.Name,
					transitiveViolation.Value.ForbiddenLayerName ?? "unrecognized",
					transitiveViolation.Value.Site,
					transitiveViolation.Value.Evaluation.Reason,
					transitivePolicy.TransitiveExposure?.Description ?? transitivePolicy.Description,
					transitivePolicy.XmlPath,
					transitivePolicy.XmlLineNumber,
					ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure,
					path,
					transitiveViolation.Value.Depth,
					segments));
			}
		}
	}
}
