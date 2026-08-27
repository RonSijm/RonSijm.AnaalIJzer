using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.EntryPoints;
using RonSijm.AnaalIJzer.Core.LayerModel;

namespace RonSijm.AnaalIJzer.Core.PolicyEvaluation.Engine.DependencyRules;

/// <summary>
///     Owns the two-tier type-to-layer lookup (exact-name fast path + pattern list)
///     together with the <see cref="FindLayer" /> and exception-evaluation logic
///     that was previously inlined on <see cref="AnalyzerConfig" />.
/// </summary>
public readonly partial struct LayerRegistry
{
	private readonly CompiledLayerCatalog _catalog;

	public LayerRegistry(CompiledLayerCatalog catalog)
	{
		_catalog = catalog;
		HasLayers = catalog.HasLayers;
		HasContractPolicies = catalog.HasContractPolicies;
		HasInheritancePolicies = catalog.HasInheritancePolicies;
		HasVisibilityPolicies = catalog.HasVisibilityPolicies;
		HasApiSurfacePolicies = catalog.HasApiSurfacePolicies;
		HasEntryPointPolicies = catalog.HasEntryPointPolicies;
		HasSourceLocationPolicies = catalog.HasSourceLocationPolicies;
	}

	public bool HasLayers { get; }
	public bool HasContractPolicies { get; }
	public bool HasInheritancePolicies { get; }
	public bool HasVisibilityPolicies { get; }
	public bool HasApiSurfacePolicies { get; }
	public bool HasEntryPointPolicies { get; }
	public bool HasSourceLocationPolicies { get; }

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

			if (!_catalog.NodesByPath.TryGetValue(boundary.Name, out var node) || node.EntryPointPolicies.IsDefaultOrEmpty)
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
