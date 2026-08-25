using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.ObservedDependencies;

public readonly struct ObservedDependencyCycle
{
	public ObservedDependencyCycle(string scope, ImmutableArray<string> layers, ImmutableArray<ObservedDependency> representativeEdges, ImmutableArray<string> observedSites)
	{
		Scope = scope;
		Layers = layers;
		RepresentativeEdges = representativeEdges;
		ObservedSites = observedSites;
	}

	public string Scope { get; }

	public ImmutableArray<string> Layers { get; }

	public ImmutableArray<ObservedDependency> RepresentativeEdges { get; }

	public ImmutableArray<string> ObservedSites { get; }

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
