using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.ApiSurface.Engine.Policies;

namespace RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Model;

public readonly struct TransitiveExposureViolationCandidate(
	INamedTypeSymbol forbiddenType,
	string? forbiddenLayerName,
	ApiSurfaceEvaluation evaluation,
	string site,
	ExposurePath path,
	int depth,
	ISymbol? nestedMember,
	Location? nestedLocation)
{
	public INamedTypeSymbol ForbiddenType { get; } = forbiddenType;
	public string? ForbiddenLayerName { get; } = forbiddenLayerName;
	public ApiSurfaceEvaluation Evaluation { get; } = evaluation;
	public string Site { get; } = site;
	public ExposurePath Path { get; } = path;
	public int Depth { get; } = depth;
	public ISymbol? NestedMember { get; } = nestedMember;
	public Location? NestedLocation { get; } = nestedLocation;
}
