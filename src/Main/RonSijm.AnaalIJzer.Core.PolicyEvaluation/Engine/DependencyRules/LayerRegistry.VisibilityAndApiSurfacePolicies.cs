using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.ApiSurface.Engine.Policies;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Core.SourceLocations;
using RonSijm.AnaalIJzer.Core.Visibility;

namespace RonSijm.AnaalIJzer.Core.PolicyEvaluation.Engine.DependencyRules;

public readonly partial struct LayerRegistry
{
	public VisibilityPolicyEvaluation? EvaluateVisibilityPolicies(LayerMatch layerMatch, VisibilityPolicyTarget target, ArchitectureAccessibility accessibility)
	{
		foreach (var layer in layerMatch.Layers)
		{
			if (!_catalog.NodesByPath.TryGetValue(layer.Name, out var node))
			{
				continue;
			}

			foreach (var policy in node.VisibilityPolicies)
			{
				var result = policy.Evaluate(target, accessibility);
				if (result is not null)
				{
					return result;
				}
			}
		}

		return null;
	}

	public ApiSurfaceEvaluation? EvaluateApiSurfacePolicies(LayerMatch callerLayerMatch, LayerMatch? exposedLayerMatch, string exposedTypeName, string site, int exposureDepth = 0)
	{
		var exposedLayer = CreateApiSurfaceLayerSelection(exposedLayerMatch);
		foreach (var layer in callerLayerMatch.Layers)
		{
			if (!_catalog.NodesByPath.TryGetValue(layer.Name, out var node))
			{
				continue;
			}

			foreach (var policy in node.ApiSurfacePolicies)
			{
				var result = policy.Evaluate(exposedLayer, exposedTypeName, site, exposureDepth);
				if (result is not null)
				{
					return result;
				}
			}
		}

		return null;
	}

	private static ApiSurfaceLayerSelection CreateApiSurfaceLayerSelection(LayerMatch? exposedLayerMatch)
	{
		if (exposedLayerMatch is null)
		{
			return ApiSurfaceLayerSelection.Unrecognized;
		}

		var result = new ApiSurfaceLayerSelection(
			exposedLayerMatch.Value.Layer.Name,
			exposedLayerMatch.Value.Layers.Select(layer => layer.Name).ToImmutableArray());

		return result;
	}

	public int GetTransitiveExposureMaxDepth(LayerMatch callerLayerMatch)
	{
		var maxDepth = 0;
		foreach (var layer in callerLayerMatch.Layers)
		{
			if (!_catalog.NodesByPath.TryGetValue(layer.Name, out var node))
			{
				continue;
			}

			foreach (var policy in node.ApiSurfacePolicies)
			{
				if (policy.TransitiveExposure is { } options)
				{
					maxDepth = Math.Max(maxDepth, options.MaxDepth);
				}
			}
		}

		return maxDepth;
	}

	public ImmutableArray<SourceLocationPolicy> GetSourceLocationPolicies(LayerMatch layerMatch)
	{
		var result = ImmutableArray.CreateBuilder<SourceLocationPolicy>();
		foreach (var layer in layerMatch.Layers)
		{
			if (!_catalog.NodesByPath.TryGetValue(layer.Name, out var node))
			{
				continue;
			}

			result.AddRange(node.SourceLocationPolicies);
		}

		return result.ToImmutable();
	}
}
