using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Engine.ApiSurface;

public readonly struct ApiSurfaceLayerSelection(string layerPath, ImmutableArray<string> layerPaths)
{
	public static ApiSurfaceLayerSelection Unrecognized { get; } = new(string.Empty, ImmutableArray<string>.Empty);

	public string LayerPath { get; } = layerPath;

	public ImmutableArray<string> LayerPaths { get; } = layerPaths;

	public bool IsRecognized
	{
		get { return !string.IsNullOrEmpty(LayerPath); }
	}

	public bool Selects(string configuredLayerPath)
	{
		var result = LayerPaths.Any(path => string.Equals(path, configuredLayerPath, StringComparison.Ordinal));

		return result;
	}
}
