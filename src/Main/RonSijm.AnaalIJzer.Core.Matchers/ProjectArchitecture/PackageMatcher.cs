using System.Collections.Immutable;
using System.Text.RegularExpressions;
using RonSijm.AnaalIJzer.Conditions;

namespace RonSijm.AnaalIJzer.ProjectArchitecture;

public readonly struct PackageMatcher(
	ImmutableArray<MatchCondition> conditions,
	string? comment,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public ImmutableArray<MatchCondition> Conditions { get; } = conditions;

	public string? Comment { get; } = comment;

	public string? Description { get; } = description;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public bool Matches(string packageId)
	{
		if (Conditions.IsDefaultOrEmpty)
		{
			return false;
		}

		foreach (var condition in Conditions)
		{
			if (!condition.MatchesString(packageId, StringComparison.OrdinalIgnoreCase, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
			{
				return false;
			}
		}

		var result = true;

		return result;
	}
}
