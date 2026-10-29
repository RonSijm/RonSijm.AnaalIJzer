using Microsoft.CodeAnalysis;

namespace RonSijm.IJzer.Core.ApiSurface.Analysis.Model;

public readonly struct ExposurePathSegment(string displayName, Location? location)
{
    public string DisplayName { get; } = displayName;
    public Location? Location { get; } = location;
}