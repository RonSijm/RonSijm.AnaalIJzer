using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Engine.EntryPoints;

public readonly struct BoundaryEntryPointPolicy(
	string ownerLayerPath,
	ImmutableArray<BoundaryEntryPointRule> rules,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public string OwnerLayerPath { get; } = ownerLayerPath;

	public ImmutableArray<BoundaryEntryPointRule> Rules { get; } = rules;

	public string? Description { get; } = description;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;
}
