using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Matchers.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture;

/// <summary>
///     Applies an allowlist or blocklist to direct raw assembly references from one project group.
/// </summary>
public readonly struct AssemblyReferencePolicy(
	string projectGroup,
	ImmutableArray<ReferenceIdentityMatcher> allowedMatchers,
	ImmutableArray<ReferenceIdentityMatcher> forbiddenMatchers,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public string ProjectGroup { get; } = projectGroup;

	public ImmutableArray<ReferenceIdentityMatcher> AllowedMatchers { get; } = allowedMatchers;

	public ImmutableArray<ReferenceIdentityMatcher> ForbiddenMatchers { get; } = forbiddenMatchers;

	public string? Description { get; } = description;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;
}
