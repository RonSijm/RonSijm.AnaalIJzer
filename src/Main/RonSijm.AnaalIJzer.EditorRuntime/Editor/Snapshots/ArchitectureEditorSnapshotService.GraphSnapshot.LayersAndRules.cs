using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.GraphModel.Building;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.EditorRuntime.Editor.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static void AddGraphLayer(ProjectAnalyzerConfig config, LayerNode layer, ImmutableDictionary<string, int> paletteSlots, ImmutableArray<string> activeLayerPaths, ArchitectureConfigurationSource configurationSource, string inlineConfigPath, ImmutableArray<ArchitectureGraphLayerInput>.Builder layers)
	{
		var layerPath = layer.Definition.Name;
		var slashIndex = layerPath.LastIndexOf('/');
		var displayName = slashIndex >= 0 ? layerPath.Substring(slashIndex + 1) : layerPath;
		var paletteSlot = paletteSlots.TryGetValue(layerPath, out var slot) ? slot : 1;
		var documentationItem = config.Documentation.Items.FirstOrDefault(item => item.Kind == "Layer" && item.LayerPath == layerPath);
		var sourcePath = documentationItem.Kind == "Layer" ? documentationItem.SourcePath : string.Empty;
		layers.Add(new ArchitectureGraphLayerInput(
			layerPath,
			displayName,
			FindLayerDescription(config, layerPath),
			layerPath.Count(character => character == '/'),
			paletteSlot,
			activeLayerPaths.Any(activeLayerPath => PathsOverlap(layerPath, activeLayerPath)),
			GetEditableElementPath(sourcePath, configurationSource, inlineConfigPath),
			GetEditableElementSourceKind(sourcePath, configurationSource, inlineConfigPath),
			documentationItem.Kind == "Layer" ? documentationItem.XmlLineNumber : 0));

		foreach (var child in layer.Children)
		{
			AddGraphLayer(config, child, paletteSlots, activeLayerPaths, configurationSource, inlineConfigPath, layers);
		}
	}

	private static bool RuleTouchesActiveLayer(DependencyEdge edge, ImmutableArray<string> activeLayerPaths)
	{
		var result = activeLayerPaths.Any(activeLayerPath => EndpointTouchesLayer(edge.From, activeLayerPath) || EndpointTouchesLayer(edge.To, activeLayerPath));

		return result;
	}

	private static bool EndpointTouchesLayer(string endpoint, string activeLayerPath)
	{
		var result = endpoint == "*" || PathsOverlap(endpoint, activeLayerPath);

		return result;
	}

	private static bool PathsOverlap(string left, string right)
	{
		var result = left == right
		             || left.StartsWith(right + "/", StringComparison.Ordinal)
		             || right.StartsWith(left + "/", StringComparison.Ordinal);

		return result;
	}
}
