using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Conditions;

namespace RonSijm.AnaalIJzer.SourceLocations;

public readonly struct SourceLocationRule(
	ImmutableArray<MatchCondition> conditions,
	string? assemblyName,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public ImmutableArray<MatchCondition> Conditions { get; } = conditions;

	public string? AssemblyName { get; } = assemblyName;

	public string? Description { get; } = description;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public bool Matches(string normalizedSourcePath, string compilationAssemblyName)
	{
		if (AssemblyName is not null
		    && !string.Equals(AssemblyName, compilationAssemblyName, StringComparison.Ordinal))
		{
			return false;
		}

		var matcher = new PatternMatcher(MatchTarget.TypeName, Conditions);
		var result = matcher.TryMatch(normalizedSourcePath, string.Empty);

		return result is not null;
	}
}
