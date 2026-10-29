using Microsoft.CodeAnalysis;

namespace RonSijm.IJzer.Core.ApiSurface.Analysis.Model;

public readonly struct ApiSurfaceTypeReference(INamedTypeSymbol type, string site, Location location)
{
    public INamedTypeSymbol Type { get; } = type;
    public string Site { get; } = site;
    public Location Location { get; } = location;
}