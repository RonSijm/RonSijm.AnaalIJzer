using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Declarations;
using RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Model;
using RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Traversal;
using RonSijm.AnaalIJzer.Core.Visibility;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.ApiSurface;

internal static partial class ApiSurfaceAnalyzer
{
	internal static void AnalyzeSymbol(
		SymbolAnalysisContext context,
		AnalyzerConfig config,
		ConcurrentDictionary<ISymbol, byte> analyzedSymbols,
		ConcurrentDictionary<INamedTypeSymbol, ImmutableArray<ExposureMemberTypeReference>> transitiveMemberCache)
	{
		var symbol = NormalizePartialSymbol(context.Symbol);
		if (!analyzedSymbols.TryAdd(symbol, 0)
		    || symbol.IsImplicitlyDeclared
		    || symbol.DeclaringSyntaxReferences.Length == 0
		    || !symbol.IsEffectivelyExternallyVisible())
		{
			return;
		}

		var ownerType = GetPolicyOwnerType(symbol);
		if (ownerType is null)
		{
			return;
		}

		var callerLayer = config.Engine.FindLayer(ownerType.Name, ownerType.ContainingNamespace?.ToDisplayString() ?? string.Empty, ownerType);
		if (callerLayer is null || callerLayer.Value.Layer.IsForbidden)
		{
			return;
		}

		var reported = new HashSet<ApiSurfaceDiagnosticKey>();
		var reportedTransitive = new HashSet<ApiSurfaceDiagnosticKey>();
		var apiMemberName = GetDisplayName(symbol);
		var maxTransitiveDepth = config.Engine.GetTransitiveExposureMaxDepth(callerLayer.Value);
		foreach (var reference in ApiSurfaceDeclarationWalker.GetReferences(symbol, context.Compilation, context.CancellationToken))
		{
			var dependencyType = reference.Type.OriginalDefinition;
			var dependencyLayer = config.Engine.FindLayer(dependencyType.Name, dependencyType.ContainingNamespace?.ToDisplayString() ?? string.Empty, dependencyType);
			var evaluation = config.Engine.EvaluateApiSurfacePolicies(callerLayer.Value, dependencyLayer, dependencyType.Name, reference.Site);
			if (evaluation is not null)
			{
				ReportDirectViolation(context, ownerType, callerLayer.Value, dependencyType, dependencyLayer, reference, evaluation.Value, apiMemberName, reported);
				continue;
			}

			if (maxTransitiveDepth == 0 || dependencyLayer is null || !CanTraverse(dependencyType))
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
				context.CancellationToken);
			if (transitiveViolation is not null)
			{
				ReportTransitiveViolation(context, ownerType, callerLayer.Value, reference, transitiveViolation.Value, reportedTransitive);
			}
		}
	}
}
