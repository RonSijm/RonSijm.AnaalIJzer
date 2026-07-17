using System.Collections.Immutable;
using System.Globalization;
using RonSijm.AnaalIJzer.Indicators;
using RonSijm.AnaalIJzer.Snapshots;

namespace RonSijm.AnaalIJzer.QuickInfo;

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
}
