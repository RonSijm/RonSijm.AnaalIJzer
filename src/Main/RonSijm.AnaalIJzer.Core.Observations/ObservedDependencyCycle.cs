using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.Observations;

public readonly struct ObservedDependencyCycle(
	string scope,
	ImmutableArray<string> layers,
	ImmutableArray<ObservedDependency> representativeEdges,
	ImmutableArray<string> observedSites)
{
	public string Scope { get; } = scope;

	public ImmutableArray<string> Layers { get; } = layers;

	public ImmutableArray<ObservedDependency> RepresentativeEdges { get; } = representativeEdges;

	public ImmutableArray<string> ObservedSites { get; } = observedSites;

	public int Length
	{
		get
		{
			var result = Layers.Length;

			return result;
		}
	}

	public string GetDisplayPath()
	{
		if (Layers.IsDefaultOrEmpty)
		{
			return string.Empty;
		}

		var result = string.Join(" -> ", Layers) + " -> " + Layers[0];

		return result;
	}
}
