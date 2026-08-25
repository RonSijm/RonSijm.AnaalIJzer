using System.Collections.Immutable;
using System.Globalization;
using RonSijm.AnaalIJzer.Indicators;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;

namespace RonSijm.AnaalIJzer.Core.Editor.QuickInfo;

public static class ArchitectureQuickInfoContentBuilder
{
	public static ArchitectureQuickInfoContent CreateLayerContent(ArchitectureLayerIndicator indicator, ArchitectureEditorOptions? options = null)
	{
		options ??= ArchitectureEditorOptions.Default;
		var lines = ImmutableArray.CreateBuilder<string>();
		lines.Add("Type: " + indicator.TypeName);
		lines.Add("Layer: " + indicator.LayerPath);
		if (!indicator.IsInLayer)
		{
			if (!string.IsNullOrWhiteSpace(indicator.Description))
			{
				lines.Add("Reason: " + indicator.Description);
			}

			var unclassifiedResult = new ArchitectureQuickInfoContent("AnaalIJzer layer", lines.ToImmutable());

			return unclassifiedResult;
		}

		if (indicator.LayerAncestry.Length > 0)
		{
			lines.Add("Ancestry: " + string.Join(" > ", indicator.LayerAncestry));
		}

		lines.Add("Palette slot: AnaalIJzer Layer " + indicator.PaletteSlot.ToString("00", CultureInfo.InvariantCulture));
		if (options.ShowLinearCallChainInBadges && indicator.LinearCallChain.Length > 1)
		{
			lines.Add("Call chain: " + string.Join(" -> ", indicator.LinearCallChain));
		}

		lines.Add("Can be called by: " + FormatLayerList(FilterGlobalLayerRules(indicator.LayersThatCanCallThisLayer, options)));
		lines.Add("Can call: " + FormatLayerList(FilterGlobalLayerRules(indicator.LayersThisLayerCanCall, options)));
		if (indicator.ExceptionReviewCount > 0)
		{
			lines.Add("Exception reviews: " + indicator.ExceptionReviewCount);
			foreach (var summary in indicator.ExceptionReviewSummaries)
			{
				lines.Add("  - " + summary);
			}
		}
		if (!string.IsNullOrWhiteSpace(indicator.Description))
		{
			lines.Add("Description: " + indicator.Description);
		}

		var result = new ArchitectureQuickInfoContent("AnaalIJzer layer", lines.ToImmutable());

		return result;
	}

	private static ImmutableArray<string> FilterGlobalLayerRules(ImmutableArray<string> layers, ArchitectureEditorOptions options)
	{
		if (options.ShowGlobalLayerRulesInBadges)
		{
			return layers;
		}

		var result = layers
			.Where(layer => !IsGlobalLayerRule(layer))
			.ToImmutableArray();

		return result;
	}

	private static bool IsGlobalLayerRule(string layer)
	{
		var result = layer == "*" || layer.StartsWith("* ", StringComparison.Ordinal);

		return result;
	}

	private static string FormatLayerList(ImmutableArray<string> layers)
	{
		var result = layers.Length == 0 ? "none configured" : string.Join(", ", layers);

		return result;
	}

	public static ArchitectureQuickInfoContent CreateSiteContent(ArchitectureDependencySiteIndicator indicator)
	{
		var lines = ImmutableArray.CreateBuilder<string>();
		lines.Add("Site: " + indicator.Site);
		lines.Add("Caller: " + indicator.CallerTypeName + " (" + indicator.CallerLayerPath + ")");
		var dependencyLayer = string.IsNullOrWhiteSpace(indicator.DependencyLayerPath)
			? "unclassified"
			: indicator.DependencyLayerPath;
		lines.Add("Dependency: " + indicator.DependencyTypeName + " (" + dependencyLayer + ")");
		lines.Add("Status: " + indicator.Status);
		if (!string.IsNullOrWhiteSpace(indicator.DiagnosticId))
		{
			lines.Add("Diagnostic: " + indicator.DiagnosticId);
		}

		lines.Add("Reason: " + indicator.Reason);

		var result = new ArchitectureQuickInfoContent("AnaalIJzer dependency site", lines.ToImmutable());

		return result;
	}

	public static ArchitectureQuickInfoContent CreateNameRuleContent(ArchitectureNameRuleIndicator indicator)
	{
		var lines = ImmutableArray.CreateBuilder<string>();
		lines.Add("Rule: " + indicator.RuleKind);
		lines.Add("Site: " + indicator.Site);
		lines.Add("Caller: " + indicator.CallerTypeName + " (" + indicator.CallerLayerPath + ")");
		lines.Add("Source: " + indicator.SourceName + " -> " + indicator.NormalizedSourceName);
		lines.Add("Target: " + indicator.TargetName + " -> " + indicator.NormalizedTargetName);
		lines.Add("Diagnostic: " + indicator.DiagnosticId);
		lines.Add("Reason: " + indicator.Reason);
		var result = new ArchitectureQuickInfoContent("AnaalIJzer name rule", lines.ToImmutable());

		return result;
	}

	public static ArchitectureQuickInfoContent CreateVisibilityPolicyContent(ArchitectureVisibilityPolicyIndicator indicator)
	{
		var lines = ImmutableArray.CreateBuilder<string>();
		lines.Add("Declaration: " + indicator.DeclarationName);
		lines.Add("Layer: " + indicator.LayerPath);
		lines.Add("Target: " + indicator.DeclarationTarget);
		lines.Add("Declared accessibility: " + indicator.DeclaredAccessibility);
		lines.Add("Effectively externally visible: " + (indicator.IsEffectivelyExternallyVisible ? "yes" : "no"));
		lines.Add("Diagnostic: " + indicator.DiagnosticId);
		lines.Add("Reason: " + indicator.Reason);
		if (!string.IsNullOrWhiteSpace(indicator.Description))
		{
			lines.Add("Description: " + indicator.Description);
		}
		if (!string.IsNullOrWhiteSpace(indicator.ConfigurationPath))
		{
			lines.Add("Policy: " + indicator.ConfigurationPath + (indicator.ConfigurationLine > 0 ? ":" + indicator.ConfigurationLine : string.Empty));
		}

		var result = new ArchitectureQuickInfoContent("AnaalIJzer visibility policy", lines.ToImmutable());

		return result;
	}

	public static ArchitectureQuickInfoContent CreateApiSurfaceContent(ArchitectureApiSurfaceIndicator indicator)
	{
		var lines = ImmutableArray.CreateBuilder<string>();
		lines.Add("API member: " + indicator.ApiMemberName);
		lines.Add("Owner: " + indicator.CallerTypeName + " (" + indicator.CallerLayerPath + ")");
		lines.Add("Exposed type: " + indicator.ExposedTypeName + " (" + indicator.ExposedLayerPath + ")");
		lines.Add("Site: " + indicator.Site);
		lines.Add("Diagnostic: " + indicator.DiagnosticId);
		if (indicator.IsTransitive)
		{
			lines.Add("Exposure path: " + indicator.ExposurePath);
			lines.Add("Exposure depth: " + indicator.ExposureDepth);
			var navigableSegments = indicator.ExposureSegments.Count(segment => segment.CanNavigate);
			lines.Add("Source-backed path segments: " + navigableSegments);
		}
		lines.Add("Reason: " + indicator.Reason);
		if (!string.IsNullOrWhiteSpace(indicator.Description))
		{
			lines.Add("Description: " + indicator.Description);
		}
		if (!string.IsNullOrWhiteSpace(indicator.ConfigurationPath))
		{
			lines.Add("Policy: " + indicator.ConfigurationPath + (indicator.ConfigurationLine > 0 ? ":" + indicator.ConfigurationLine : string.Empty));
		}

		var title = indicator.IsTransitive ? "AnaalIJzer transitive API exposure" : "AnaalIJzer API exposure";
		var result = new ArchitectureQuickInfoContent(title, lines.ToImmutable());

		return result;
	}
}
