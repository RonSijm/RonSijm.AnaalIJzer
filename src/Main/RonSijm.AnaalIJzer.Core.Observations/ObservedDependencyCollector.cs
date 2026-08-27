using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.Observations;

public sealed class ObservedDependencyCollector
{
	private readonly ConcurrentDictionary<string, ObservedDependency> _observations = new(StringComparer.Ordinal);

	public void Record(string callerTypeName, string callerLayer, string dependencyTypeName, string dependencyLayer, string site, Location location, string? sourceProjectName = null)
	{
		var observation = new ObservedDependency(callerTypeName, callerLayer, dependencyTypeName, dependencyLayer, site, location, sourceProjectName);
		_observations.TryAdd(observation.GetDeduplicationKey(), observation);
	}

	public ImmutableArray<ObservedDependency> GetSnapshot()
	{
		var result = _observations.Values
			.OrderBy(observation => observation.CallerLayer, StringComparer.Ordinal)
			.ThenBy(observation => observation.DependencyLayer, StringComparer.Ordinal)
			.ThenBy(observation => observation.CallerTypeName, StringComparer.Ordinal)
			.ThenBy(observation => observation.DependencyTypeName, StringComparer.Ordinal)
			.ThenBy(observation => observation.Site, StringComparer.Ordinal)
			.ThenBy(observation => observation.Location.SourceTree?.FilePath, StringComparer.OrdinalIgnoreCase)
			.ThenBy(observation => observation.Location.SourceSpan.Start)
			.ToImmutableArray();

		return result;
	}
}
