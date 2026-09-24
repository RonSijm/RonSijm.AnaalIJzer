using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.Observations;

public readonly struct DependencySiteObservation(INamedTypeSymbol callerType, INamedTypeSymbol dependencyType, string site, Location location)
{
    public INamedTypeSymbol CallerType { get; } = callerType;
    public INamedTypeSymbol DependencyType { get; } = dependencyType;
    public string Site { get; } = site;
    public Location Location { get; } = location;
}