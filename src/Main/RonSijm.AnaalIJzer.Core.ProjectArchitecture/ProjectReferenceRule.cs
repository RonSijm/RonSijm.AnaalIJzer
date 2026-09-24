using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Matchers.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture;

public readonly partial struct ProjectReferenceRule(
    ProjectReferenceRuleKind kind,
    string from,
    string to,
    string? description,
    string xmlPath,
    int xmlLineNumber,
    int xmlLinePosition,
    ImmutableArray<ProjectMatcher> fromMatchers = default,
    ImmutableArray<ProjectMatcher> toMatchers = default)
{
    public ProjectReferenceRuleKind Kind { get; } = kind;

    public string From { get; } = from;

    public string To { get; } = to;

    public string? Description { get; } = description;

    public string XmlPath { get; } = xmlPath;

    public int XmlLineNumber { get; } = xmlLineNumber;

    public int XmlLinePosition { get; } = xmlLinePosition;

    public ImmutableArray<ProjectMatcher> FromMatchers { get; } = fromMatchers.IsDefault ? ImmutableArray<ProjectMatcher>.Empty : fromMatchers;

    public ImmutableArray<ProjectMatcher> ToMatchers { get; } = toMatchers.IsDefault ? ImmutableArray<ProjectMatcher>.Empty : toMatchers;
}