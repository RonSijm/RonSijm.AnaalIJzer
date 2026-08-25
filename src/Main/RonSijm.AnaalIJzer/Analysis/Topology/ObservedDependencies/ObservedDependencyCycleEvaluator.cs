using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Engine.DependencyRules;

namespace RonSijm.AnaalIJzer.ObservedDependencies;

internal static class ObservedDependencyCycleEvaluator
{
	public static ImmutableArray<ObservedDependencyCycle> FindCycles(IEnumerable<string> layerNames, IEnumerable<ObservedDependency> observations, string scope)
	{
		var observationArray = observations
			.Where(observation => observation.IsCycleCandidate)
			.ToImmutableArray();
		var distinctEdges = observationArray
			.Select(observation => (observation.CallerLayer, observation.DependencyLayer))
			.Distinct()
			.ToImmutableArray();
		var cycles = DependencyCycleDetector.FindCycles(layerNames, distinctEdges);
		var result = ImmutableArray.CreateBuilder<ObservedDependencyCycle>();

		foreach (var cycle in cycles)
		{
			var representativeEdges = ImmutableArray.CreateBuilder<ObservedDependency>();
			var observedSites = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);

			for (var index = 0; index < cycle.Length; index++)
			{
				var from = cycle[index];
				var to = cycle[(index + 1) % cycle.Length];
				var matchingObservations = observationArray
					.Where(observation => observation.CallerLayer == from && observation.DependencyLayer == to)
					.OrderBy(observation => observation.SourceProjectName, StringComparer.Ordinal)
					.ThenBy(observation => observation.Location.SourceTree?.FilePath, StringComparer.OrdinalIgnoreCase)
					.ThenBy(observation => observation.Location.SourceSpan.Start)
					.ThenBy(observation => observation.Site, StringComparer.Ordinal)
					.ThenBy(observation => observation.CallerTypeName, StringComparer.Ordinal)
					.ThenBy(observation => observation.DependencyTypeName, StringComparer.Ordinal)
					.ToArray();
				if (matchingObservations.Length == 0)
				{
					representativeEdges.Clear();
					break;
				}

				representativeEdges.Add(matchingObservations[0]);
				foreach (var observation in matchingObservations)
				{
					observedSites.Add(observation.Site);
				}
			}

			if (representativeEdges.Count != cycle.Length)
			{
				continue;
			}

			result.Add(new ObservedDependencyCycle(
				scope,
				cycle,
				representativeEdges.ToImmutable(),
				observedSites.OrderBy(site => site, StringComparer.Ordinal).ToImmutableArray()));
		}

		return result.ToImmutable();
	}
}
