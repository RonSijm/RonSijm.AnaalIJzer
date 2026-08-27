using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.ApiSurface.Engine.Policies;
using RonSijm.AnaalIJzer.Core.DependencyRules;

namespace RonSijm.AnaalIJzer.Core.ApiSurface.Tests.ApiSurface;

public sealed class ApiSurfacePolicyTests
{
	[Fact]
	public void Evaluate_IgnoresUnrecognizedTypesWhenRecognitionIsNotRequired()
	{
		var policy = CreatePolicy(requireRecognizedTypes: false);

		var result = policy.Evaluate(ApiSurfaceLayerSelection.Unrecognized, "CustomerDto", "MethodReturn");

		result.Should().BeNull();
	}

	[Fact]
	public void Evaluate_RejectsUnrecognizedTypesWhenRecognitionIsRequired()
	{
		var policy = CreatePolicy(requireRecognizedTypes: true);

		var result = policy.Evaluate(ApiSurfaceLayerSelection.Unrecognized, "CustomerDto", "MethodReturn");

		result.Should().NotBeNull();
		result.Value.Rule.Should().BeNull();
		result.Value.Reason.Should().Contain("requires exposed types to belong to a configured layer");
	}

	[Fact]
	public void Evaluate_BlockedRuleWinsWhenAncestorPathMatches()
	{
		var policy = CreatePolicy(
			blockedLayers: [CreateRule("Application", allowedSites: "MethodReturn")]);
		var exposedLayer = CreateLayer("Application/Contracts", "Application", "Application/Contracts");

		var result = policy.Evaluate(exposedLayer, "CustomerDto", "MethodReturn");

		result.Should().NotBeNull();
		result.Value.Rule.Should().NotBeNull();
		result.Value.Rule!.Value.LayerPath.Should().Be("Application");
		result.Value.Reason.Should().Contain("blocks layer '/Application' at MethodReturn");
	}

	[Fact]
	public void Evaluate_AllowsMatchingAllowedAncestorPath()
	{
		var policy = CreatePolicy(
			allowedLayers: [CreateRule("Application", allowedSites: "MethodReturn")]);
		var exposedLayer = CreateLayer("Application/Contracts", "Application", "Application/Contracts");

		var result = policy.Evaluate(exposedLayer, "CustomerDto", "MethodReturn");

		result.Should().BeNull();
	}

	[Fact]
	public void Evaluate_IgnoresAllowedRulesThatDoNotApplyToTheCurrentSite()
	{
		var policy = CreatePolicy(
			allowedLayers: [CreateRule("Contracts", allowedSites: "Method")]);
		var exposedLayer = CreateLayer("Repository/Contracts", "Repository", "Repository/Contracts");

		var result = policy.Evaluate(exposedLayer, "CustomerDto", "MethodReturn");

		result.Should().BeNull();
	}

	[Fact]
	public void Evaluate_RejectsRecognizedTypeWhenApplicableAllowedRulesDoNotMatch()
	{
		var policy = CreatePolicy(
			allowedLayers:
            [
                CreateRule("Contracts", allowedSites: "MethodReturn"),
				CreateRule("Contracts/Public", allowedSites: "MethodReturn")
            ]);
		var exposedLayer = CreateLayer("Repository/Contracts", "Repository", "Repository/Contracts");

		var result = policy.Evaluate(exposedLayer, "CustomerDto", "MethodReturn");

		result.Should().NotBeNull();
		result.Value.Rule.Should().BeNull();
		result.Value.Reason.Should().Contain("allows only '/Contracts', '/Contracts/Public' at MethodReturn");
		result.Value.Reason.Should().Contain("belongs to 'Repository/Contracts'");
	}

	[Fact]
	public void Evaluate_StopsCheckingTransitiveExposureBeyondMaxDepth()
	{
		var policy = CreatePolicy(
			blockedLayers: [CreateRule("Contracts")],
			transitiveExposure: new TransitiveExposureOptions(1, null, "Architecture.anl", 12, 3));
		var exposedLayer = CreateLayer("Contracts", "Contracts");

		var result = policy.Evaluate(exposedLayer, "CustomerDto", "MethodReturn", exposureDepth: 2);

		result.Should().BeNull();
	}

	private static ApiSurfacePolicy CreatePolicy(
		bool requireRecognizedTypes = false,
		ImmutableArray<ApiSurfaceLayerRule> allowedLayers = default,
		ImmutableArray<ApiSurfaceLayerRule> blockedLayers = default,
		TransitiveExposureOptions? transitiveExposure = null)
	{
		var result = new ApiSurfacePolicy(
			"Application",
			requireRecognizedTypes,
			allowedLayers.IsDefault ? ImmutableArray<ApiSurfaceLayerRule>.Empty : allowedLayers,
			blockedLayers.IsDefault ? ImmutableArray<ApiSurfaceLayerRule>.Empty : blockedLayers,
			transitiveExposure,
			null,
			"Architecture.anl",
			10,
			2);

		return result;
	}

	private static ApiSurfaceLayerRule CreateRule(string layerPath, string? allowedSites = null)
	{
		var siteFilter = allowedSites is null
			? DependencySiteFilter.All
			: new DependencySiteFilter(ImmutableHashSet.Create(StringComparer.Ordinal, allowedSites), ImmutableHashSet<string>.Empty);
		var result = new ApiSurfaceLayerRule(
			layerPath,
			"/" + layerPath,
			siteFilter,
			null,
			"Architecture.anl",
			11,
			4);

		return result;
	}

	private static ApiSurfaceLayerSelection CreateLayer(string layerPath, params string[] layerPaths)
	{
		var result = new ApiSurfaceLayerSelection(layerPath, [.. layerPaths]);

		return result;
	}
}
