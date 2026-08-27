using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.GraphModel.Model;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.EditorRuntime.Editor.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static ImmutableDictionary<string, int> BuildPaletteSlots(ProjectAnalyzerConfig config)
	{
		var slots = ImmutableDictionary.CreateBuilder<string, int>(StringComparer.Ordinal);
		var index = 0;
		foreach (var layerName in config.LayerNames)
		{
			if (!slots.ContainsKey(layerName))
			{
				slots.Add(layerName, index++ % PaletteSlotCount + 1);
			}
		}

		return slots.ToImmutable();
	}

	private static void AddLayerIndicator(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableDictionary<string, int> paletteSlots, ImmutableArray<ArchitectureGraphExceptionReview> exceptionReviews, ImmutableArray<ArchitectureLayerIndicator>.Builder indicators, ImmutableArray<ArchitectureLayerIndicator>.Builder unclassifiedIndicators, CancellationToken cancellationToken)
	{
		if (semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) is not ITypeSymbol typeSymbol)
		{
			return;
		}

		var match = config.Engine.FindLayer(typeSymbol.Name, typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty, typeSymbol);
		if (match is null)
		{
			unclassifiedIndicators.Add(new ArchitectureLayerIndicator(
				typeDeclaration.Span,
				typeDeclaration.Identifier.Span,
				typeSymbol.Name,
				"not in layer",
				ImmutableArray<string>.Empty,
				"This type is not assigned to any configured AnaalIJzer layer.",
				0,
				false));

			return;
		}

		if (match.Value.Layer.IsForbidden)
		{
			return;
		}

		var layerPath = match.Value.Layer.Name;
		var ancestry = match.Value.Layers.Select(layer => layer.Name).ToImmutableArray();
		var paletteSlot = paletteSlots.TryGetValue(layerPath, out var slot) ? slot : 1;
		var layersThatCanCallThisLayer = GetLayersThatCanCall(config, layerPath);
		var layersThisLayerCanCall = GetLayersThisLayerCanCall(config, layerPath);
		var linearCallChain = GetLinearCallChain(config, layerPath);
		var layerExceptionReviewSummaries = GetLayerExceptionReviewSummaries(exceptionReviews, layerPath);
		indicators.Add(new ArchitectureLayerIndicator(
			typeDeclaration.Span,
			typeDeclaration.Identifier.Span,
			typeSymbol.Name,
			layerPath,
			ancestry,
			FindLayerDescription(config, layerPath),
			paletteSlot,
			true,
			layersThatCanCallThisLayer,
			layersThisLayerCanCall,
			linearCallChain,
			layerExceptionReviewSummaries.Length,
			layerExceptionReviewSummaries));
	}
}
