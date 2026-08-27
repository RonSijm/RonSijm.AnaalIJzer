using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Model;

public readonly struct ExposurePathSegment(string displayName, Location? location)
{
	public string DisplayName { get; } = displayName;
	public Location? Location { get; } = location;
}
