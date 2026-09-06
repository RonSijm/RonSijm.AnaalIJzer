using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Matching;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Model;

namespace RonSijm.AnaalIJzer.Core.OperationPolicies.Policies;

public readonly struct ForbiddenOperationRule(
	ImmutableArray<SemanticOperationMatcher> matchers,
	DependencySiteFilter siteFilter,
	string displayName,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public ImmutableArray<SemanticOperationMatcher> Matchers { get; } = matchers;

	public DependencySiteFilter SiteFilter { get; } = siteFilter;

	public string DisplayName { get; } = displayName;

	public string? Description { get; } = description;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public bool Matches(SemanticOperation operation)
	{
		if (!SiteFilter.Allows(operation.Site))
		{
			return false;
		}

		foreach (var matcher in Matchers)
		{
			if (matcher.Matches(operation))
			{
				return true;
			}
		}

		return false;
	}
}
