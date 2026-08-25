using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Contracts;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.Engine.ApiSurface;
using RonSijm.AnaalIJzer.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Engine.EntryPoints;
using RonSijm.AnaalIJzer.Engine.LayerModel;
using RonSijm.AnaalIJzer.Engine.NameRules;
using RonSijm.AnaalIJzer.Engine.Policies;
using RonSijm.AnaalIJzer.Engine.Visibility;
using RonSijm.AnaalIJzer.Inheritance;
using RonSijm.AnaalIJzer.SourceLocations;
using RonSijm.AnaalIJzer.SymbolFacts;

namespace RonSijm.AnaalIJzer.Engine.PolicyEvaluation;

public readonly struct ArchitecturePolicyEngine(
	CompiledLayerCatalog catalog)
{
	private readonly LayerRegistry registry = new(catalog);

	public bool HasLayers => registry.HasLayers;
	public bool HasContractPolicies => registry.HasContractPolicies;
	public bool HasInheritancePolicies => registry.HasInheritancePolicies;
	public bool HasVisibilityPolicies => registry.HasVisibilityPolicies;
	public bool HasApiSurfacePolicies => registry.HasApiSurfacePolicies;
	public bool HasEntryPointPolicies => registry.HasEntryPointPolicies;
	public bool HasSourceLocationPolicies => registry.HasSourceLocationPolicies;

	public LayerMatch? FindLayer(string typeName, string namespaceName, ITypeSymbol? symbol = null)
	{
		var result = registry.FindLayer(typeName, namespaceName, symbol);

		return result;
	}

	public TypePolicyViolation? EvaluateTypePolicy(LayerMatch layerMatch, string typeName, string namespaceName, ITypeSymbol? symbol = null)
	{
		var result = registry.EvaluateTypePolicy(layerMatch, typeName, namespaceName, symbol);

		return result;
	}

	public NameRuleViolation? EvaluateNameRules(LayerMatch layerMatch, NameRuleTrigger trigger, NameRuleSubject source, NameRuleSubject target, string site)
	{
		var result = registry.EvaluateNameRules(layerMatch, trigger, source, target, site);

		return result;
	}

	public ContractPolicyEvaluation? EvaluateContractPolicies(LayerMatch layerMatch, ContractDeclarationShape shape)
	{
		var result = registry.EvaluateContractPolicies(layerMatch, shape);

		return result;
	}

	public InheritancePolicyEvaluation? EvaluateInheritancePolicies(LayerMatch layerMatch, INamedTypeSymbol symbol)
	{
		var result = registry.EvaluateInheritancePolicies(layerMatch, symbol);

		return result;
	}

	public VisibilityPolicyEvaluation? EvaluateVisibilityPolicies(LayerMatch layerMatch, VisibilityPolicyTarget target, ArchitectureAccessibility accessibility)
	{
		var result = registry.EvaluateVisibilityPolicies(layerMatch, target, accessibility);

		return result;
	}

	public ApiSurfaceEvaluation? EvaluateApiSurfacePolicies(LayerMatch callerLayerMatch, LayerMatch? exposedLayerMatch, string exposedTypeName, string site, int exposureDepth = 0)
	{
		var result = registry.EvaluateApiSurfacePolicies(callerLayerMatch, exposedLayerMatch, exposedTypeName, site, exposureDepth);

		return result;
	}

	public int GetTransitiveExposureMaxDepth(LayerMatch callerLayerMatch)
	{
		var result = registry.GetTransitiveExposureMaxDepth(callerLayerMatch);

		return result;
	}

	public BoundaryEntryPointEvaluation EvaluateBoundaryEntryPoints(LayerMatch callerMatch, LayerMatch dependencyMatch, string dependencyTypeName, string dependencyNamespace, ITypeSymbol dependencyType, string site)
	{
		var result = registry.EvaluateBoundaryEntryPoints(callerMatch, dependencyMatch, dependencyTypeName, dependencyNamespace, dependencyType, site);

		return result;
	}

	public ImmutableArray<SourceLocationPolicy> GetSourceLocationPolicies(LayerMatch layerMatch)
	{
		var result = registry.GetSourceLocationPolicies(layerMatch);

		return result;
	}
}
