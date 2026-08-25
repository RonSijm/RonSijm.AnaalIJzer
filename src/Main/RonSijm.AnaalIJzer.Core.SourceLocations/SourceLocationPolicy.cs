using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.SourceLocations;

public readonly struct SourceLocationPolicy(
	string ownerLayerPath,
	SourceLocationBase relativeTo,
	ImmutableArray<SourceLocationRule> rules,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public string OwnerLayerPath { get; } = ownerLayerPath;

	public SourceLocationBase RelativeTo { get; } = relativeTo;

	public ImmutableArray<SourceLocationRule> Rules { get; } = rules;

	public string? Description { get; } = description;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public bool Matches(string normalizedSourcePath, string compilationAssemblyName)
	{
		foreach (var rule in Rules)
		{
			if (rule.Matches(normalizedSourcePath, compilationAssemblyName))
			{
				return true;
			}
		}

		return false;
	}
}
