using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.ObservedDependencies;

public readonly struct ObservedDependency
{
	public ObservedDependency(string callerTypeName, string callerLayer, string dependencyTypeName, string dependencyLayer, string site, Location location, string? sourceProjectName = null)
	{
		CallerTypeName = callerTypeName;
		CallerLayer = callerLayer;
		DependencyTypeName = dependencyTypeName;
		DependencyLayer = dependencyLayer;
		Site = site;
		Location = location;
		SourceProjectName = sourceProjectName;
	}

	public string CallerTypeName { get; }

	public string CallerLayer { get; }

	public string DependencyTypeName { get; }

	public string DependencyLayer { get; }

	public string Site { get; }

	public Location Location { get; }

	public string? SourceProjectName { get; }

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
