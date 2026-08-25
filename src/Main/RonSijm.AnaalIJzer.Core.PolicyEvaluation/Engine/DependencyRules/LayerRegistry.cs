using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Engine.ApiSurface;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.Contracts;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.Engine.Policies;
using RonSijm.AnaalIJzer.Engine.EntryPoints;
using RonSijm.AnaalIJzer.Inheritance;
using RonSijm.AnaalIJzer.Model;
using RonSijm.AnaalIJzer.SourceLocations;
using RonSijm.AnaalIJzer.SymbolFacts;
using RonSijm.AnaalIJzer.Engine.Visibility;
using RonSijm.AnaalIJzer.Engine.LayerModel;

namespace RonSijm.AnaalIJzer.Engine.DependencyRules;

/// <summary>
///     Owns the two-tier type-to-layer lookup (exact-name fast path + pattern list)
///     together with the <see cref="FindLayer" /> and exception-evaluation logic
///     that was previously inlined on <see cref="Model.AnalyzerConfig" />.
/// </summary>
public readonly partial struct LayerRegistry(
	CompiledLayerCatalog catalog)
{
	public bool HasLayers { get; } = catalog.HasLayers;
	public bool HasContractPolicies { get; } = catalog.HasContractPolicies;
	public bool HasInheritancePolicies { get; } = catalog.HasInheritancePolicies;
	public bool HasVisibilityPolicies { get; } = catalog.HasVisibilityPolicies;
	public bool HasApiSurfacePolicies { get; } = catalog.HasApiSurfacePolicies;
	public bool HasEntryPointPolicies { get; } = catalog.HasEntryPointPolicies;
	public bool HasSourceLocationPolicies { get; } = catalog.HasSourceLocationPolicies;

	/// <summary>
	///     Finds the layer for a type.
	///     Exact <c>typeName=</c> / <c>exactName=</c> matches take precedence over pattern
	///     and semantic matches. Pattern matches are evaluated in document order.
	///     Pass <paramref name="symbol" /> to enable semantic matchers
	///     (<c>inherits</c>, <c>implements</c>, <c>withAttribute</c>, <c>withAccessModifier</c>);
	///     omitting it limits matching to the string-based attributes.
	/// </summary>
	public LayerMatch? FindLayer(string typeName, string namespaceName, ITypeSymbol? symbol = null)
	{
		var match = FindFlatRootExact(typeName, namespaceName, symbol);
		if (match is not null)
		{
			return match;
		}

		match = FindForbidden(typeName, namespaceName, symbol, exactOnly: true);
		if (match is not null)
		{
			return match;
		}

		return FindNormal(typeName, namespaceName, symbol)
		       ?? FindForbidden(typeName, namespaceName, symbol, exactOnly: false);
	}

	public BoundaryEntryPointEvaluation EvaluateBoundaryEntryPoints(LayerMatch callerMatch, LayerMatch dependencyMatch, string dependencyTypeName, string dependencyNamespace, ITypeSymbol dependencyType, string site)
	{
		foreach (var boundary in dependencyMatch.Layers)
		{
			if (BoundaryEntryPointSelector.IsContainedInBoundary(boundary.Name, callerMatch.Layer.Name))
			{
				continue;
			}

			if (!catalog.NodesByPath.TryGetValue(boundary.Name, out var node) || node.EntryPointPolicies.IsDefaultOrEmpty)
			{
				continue;
			}

			foreach (var policy in node.EntryPointPolicies)
			{
				var evaluation = EvaluateBoundaryPolicy(policy, dependencyMatch, dependencyTypeName, dependencyNamespace, dependencyType, site);
				if (!evaluation.IsAllowed)
				{
					return evaluation;
				}
			}
		}

		return BoundaryEntryPointEvaluation.Allowed;
	}
}
