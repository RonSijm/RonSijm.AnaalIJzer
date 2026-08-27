using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.ApiSurface.Engine.Policies;

public readonly struct ApiSurfacePolicy(
	string ownerLayerPath,
	bool requireRecognizedTypes,
	ImmutableArray<ApiSurfaceLayerRule> allowedLayers,
	ImmutableArray<ApiSurfaceLayerRule> blockedLayers,
	TransitiveExposureOptions? transitiveExposure,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public string OwnerLayerPath { get; } = ownerLayerPath;
	public bool RequireRecognizedTypes { get; } = requireRecognizedTypes;
	public ImmutableArray<ApiSurfaceLayerRule> AllowedLayers { get; } = allowedLayers;
	public ImmutableArray<ApiSurfaceLayerRule> BlockedLayers { get; } = blockedLayers;
	public TransitiveExposureOptions? TransitiveExposure { get; } = transitiveExposure;
	public string? Description { get; } = description;
	public string XmlPath { get; } = xmlPath;
	public int XmlLineNumber { get; } = xmlLineNumber;
	public int XmlLinePosition { get; } = xmlLinePosition;

	public ApiSurfaceEvaluation? Evaluate(ApiSurfaceLayerSelection exposedLayer, string exposedTypeName, string site, int exposureDepth = 0)
	{
		if (exposureDepth > 0 && (TransitiveExposure is null || exposureDepth > TransitiveExposure.Value.MaxDepth))
		{
			return null;
		}

		if (!exposedLayer.IsRecognized)
		{
			var result = RequireRecognizedTypes
				? new ApiSurfaceEvaluation(this, null, $"the API surface policy in layer '{OwnerLayerPath}' requires exposed types to belong to a configured layer")
				: (ApiSurfaceEvaluation?)null;

			return result;
		}

		foreach (var blockedRule in BlockedLayers)
		{
			if (blockedRule.SiteFilter.Allows(site) && exposedLayer.Selects(blockedRule.LayerPath))
			{
				var reason = $"the API surface policy in layer '{OwnerLayerPath}' blocks layer '/{blockedRule.LayerPath}' at {site}";
				var result = new ApiSurfaceEvaluation(this, blockedRule, reason);

				return result;
			}
		}

		var applicableAllowedRules = AllowedLayers.Where(rule => rule.SiteFilter.Allows(site)).ToArray();
		if (applicableAllowedRules.Length == 0
		    || applicableAllowedRules.Any(rule => exposedLayer.Selects(rule.LayerPath)))
		{
			return null;
		}

		var allowedPaths = string.Join(", ", applicableAllowedRules.Select(rule => $"'/{rule.LayerPath}'"));
		var denialReason = $"the API surface policy in layer '{OwnerLayerPath}' allows only {allowedPaths} at {site}; exposed type '{exposedTypeName}' belongs to '{exposedLayer.LayerPath}'";
		var evaluation = new ApiSurfaceEvaluation(this, null, denialReason);

		return evaluation;
	}
}
