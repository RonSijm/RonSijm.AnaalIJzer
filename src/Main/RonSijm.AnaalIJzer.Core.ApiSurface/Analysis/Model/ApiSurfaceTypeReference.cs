using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Analysis.ApiSurface.Model;

public readonly struct ApiSurfaceTypeReference(INamedTypeSymbol type, string site, Location location)
{
	public INamedTypeSymbol Type { get; } = type;
	public string Site { get; } = site;
	public Location Location { get; } = location;
}
