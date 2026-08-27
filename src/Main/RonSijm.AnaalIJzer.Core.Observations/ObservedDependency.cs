using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.Observations;

public readonly struct ObservedDependency(
	string callerTypeName,
	string callerLayer,
	string dependencyTypeName,
	string dependencyLayer,
	string site,
	Location location,
	string? sourceProjectName = null)
{
	public string CallerTypeName { get; } = callerTypeName;

	public string CallerLayer { get; } = callerLayer;

	public string DependencyTypeName { get; } = dependencyTypeName;

	public string DependencyLayer { get; } = dependencyLayer;

	public string Site { get; } = site;

	public Location Location { get; } = location;

	public string? SourceProjectName { get; } = sourceProjectName;

	public bool IsCycleCandidate
	{
		get
		{
			var result = CallerLayer != DependencyLayer && CallerTypeName != DependencyTypeName;

			return result;
		}
	}

	public string GetDeduplicationKey()
	{
		var lineSpan = Location.GetLineSpan();
		var path = lineSpan.Path ?? string.Empty;
		var start = Location.SourceSpan.Start.ToString(System.Globalization.CultureInfo.InvariantCulture);
		var end = Location.SourceSpan.End.ToString(System.Globalization.CultureInfo.InvariantCulture);
		var result = string.Join(
			"|",
			CallerTypeName,
			CallerLayer,
			DependencyTypeName,
			DependencyLayer,
			Site,
			path,
			start,
			end);

		return result;
	}
}
