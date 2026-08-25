using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Analysis.ApiSurface.Model;

public readonly struct ExposureMemberTypeReference(string segmentName, ITypeSymbol type, string site, Location? location)
{
	public string SegmentName { get; } = segmentName;
	public ITypeSymbol Type { get; } = type;
	public string Site { get; } = site;
	public Location? Location { get; } = location;
}
