using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Indicators;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;

namespace RonSijm.AnaalIJzer.VisualStudio.Editor.LayerIndicators;

internal static class ArchitectureLayerCodeLensText
{
	internal static string CreateSummary(ArchitectureLayerIndicator indicator, ArchitectureEditorOptions options)
	{
		if (!indicator.IsInLayer)
		{
			var unclassifiedResult = "AnaalIJzer layer: not in a configured layer";

			return unclassifiedResult;
		}

		var inboundCount = CountLayers(indicator.LayersThatCanCallThisLayer, options);
		var outboundCount = CountLayers(indicator.LayersThisLayerCanCall, options);
		var result = "AnaalIJzer layer: "
		             + indicator.LayerPath
		             + " | called by "
		             + FormatLayerCount(inboundCount)
		             + " | can call "
		             + FormatLayerCount(outboundCount);

		return result;
	}

	private static int CountLayers(ImmutableArray<string> layers, ArchitectureEditorOptions options)
	{
		var count = layers.Count(layer => options.ShowGlobalLayerRulesInBadges || !IsGlobalLayerRule(layer));
		var result = count;

		return result;
	}

	private static bool IsGlobalLayerRule(string layer)
	{
		var result = layer == "*" || layer.StartsWith("* ", StringComparison.Ordinal);

		return result;
	}

	private static string FormatLayerCount(int count)
	{
		var result = count == 1 ? "1 layer" : count + " layers";

		return result;
	}
}
