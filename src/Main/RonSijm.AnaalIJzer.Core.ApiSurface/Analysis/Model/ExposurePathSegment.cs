using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Analysis.ApiSurface.Model;

public readonly struct ExposurePathSegment(string displayName, Location? location)
{
	public string DisplayName { get; } = displayName;
	public Location? Location { get; } = location;
}
