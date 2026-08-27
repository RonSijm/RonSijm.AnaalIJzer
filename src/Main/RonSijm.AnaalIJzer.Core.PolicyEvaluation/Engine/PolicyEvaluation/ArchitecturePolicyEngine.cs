using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.ApiSurface.Engine.Policies;
using RonSijm.AnaalIJzer.Core.Contracts.Contracts;
using RonSijm.AnaalIJzer.Core.EntryPoints;
using RonSijm.AnaalIJzer.Core.Inheritance.Policies;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Core.NameRules;
using RonSijm.AnaalIJzer.Core.PolicyEvaluation.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Core.SourceLocations;
using RonSijm.AnaalIJzer.Core.Visibility;

namespace RonSijm.AnaalIJzer.Core.PolicyEvaluation.Engine.PolicyEvaluation;

public readonly struct ArchitecturePolicyEngine(
	CompiledLayerCatalog catalog)
{
	private readonly LayerRegistry _registry = new(catalog);

	public bool HasLayers => _registry.HasLayers;
	public bool HasContractPolicies => _registry.HasContractPolicies;
	public bool HasInheritancePolicies => _registry.HasInheritancePolicies;
	public bool HasVisibilityPolicies => _registry.HasVisibilityPolicies;
	public bool HasApiSurfacePolicies => _registry.HasApiSurfacePolicies;
	public bool HasEntryPointPolicies => _registry.HasEntryPointPolicies;
	public bool HasSourceLocationPolicies => _registry.HasSourceLocationPolicies;

	public LayerMatch? FindLayer(string typeName, string namespaceName, ITypeSymbol? symbol = null)
	{
		var result = _registry.FindLayer(typeName, namespaceName, symbol);

		return result;
	}

	public TypePolicyViolation? EvaluateTypePolicy(LayerMatch layerMatch, string typeName, string namespaceName, ITypeSymbol? symbol = null)
	{
		var result = _registry.EvaluateTypePolicy(layerMatch, typeName, namespaceName, symbol);

		return result;
	}

	public NameRuleViolation? EvaluateNameRules(LayerMatch layerMatch, NameRuleTrigger trigger, NameRuleSubject source, NameRuleSubject target, string site)
	{
		var result = _registry.EvaluateNameRules(layerMatch, trigger, source, target, site);

		return result;
	}

	public ContractPolicyEvaluation? EvaluateContractPolicies(LayerMatch layerMatch, ContractDeclarationShape shape)
	{
		var result = _registry.EvaluateContractPolicies(layerMatch, shape);

		return result;
	}

	public InheritancePolicyEvaluation? EvaluateInheritancePolicies(LayerMatch layerMatch, INamedTypeSymbol symbol)
	{
		var result = _registry.EvaluateInheritancePolicies(layerMatch, symbol);

		return result;
	}

	public VisibilityPolicyEvaluation? EvaluateVisibilityPolicies(LayerMatch layerMatch, VisibilityPolicyTarget target, ArchitectureAccessibility accessibility)
	{
		var result = _registry.EvaluateVisibilityPolicies(layerMatch, target, accessibility);

		return result;
	}

	public ApiSurfaceEvaluation? EvaluateApiSurfacePolicies(LayerMatch callerLayerMatch, LayerMatch? exposedLayerMatch, string exposedTypeName, string site, int exposureDepth = 0)
	{
		var result = _registry.EvaluateApiSurfacePolicies(callerLayerMatch, exposedLayerMatch, exposedTypeName, site, exposureDepth);

		return result;
	}

	public int GetTransitiveExposureMaxDepth(LayerMatch callerLayerMatch)
	{
		var result = _registry.GetTransitiveExposureMaxDepth(callerLayerMatch);

		return result;
	}

	public BoundaryEntryPointEvaluation EvaluateBoundaryEntryPoints(LayerMatch callerMatch, LayerMatch dependencyMatch, string dependencyTypeName, string dependencyNamespace, ITypeSymbol dependencyType, string site)
	{
		var result = _registry.EvaluateBoundaryEntryPoints(callerMatch, dependencyMatch, dependencyTypeName, dependencyNamespace, dependencyType, site);

		return result;
	}

	public ImmutableArray<SourceLocationPolicy> GetSourceLocationPolicies(LayerMatch layerMatch)
	{
		var result = _registry.GetSourceLocationPolicies(layerMatch);

		return result;
	}
}
