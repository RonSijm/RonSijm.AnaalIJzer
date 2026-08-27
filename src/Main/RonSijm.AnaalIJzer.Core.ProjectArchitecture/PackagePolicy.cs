using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Matchers.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture;

public readonly struct PackagePolicy(
	string projectGroup,
	ImmutableArray<PackageMatcher> allowedMatchers,
	ImmutableArray<PackageMatcher> forbiddenMatchers,
	bool includeTransitive,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public string ProjectGroup { get; } = projectGroup;

	public ImmutableArray<PackageMatcher> AllowedMatchers { get; } = allowedMatchers;

	public ImmutableArray<PackageMatcher> ForbiddenMatchers { get; } = forbiddenMatchers;

	public bool IncludeTransitive { get; } = includeTransitive;

	public string? Description { get; } = description;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;
}
