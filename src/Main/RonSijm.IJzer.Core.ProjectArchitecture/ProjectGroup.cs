using System.Collections.Immutable;
using RonSijm.IJzer.Core.Matchers.ProjectArchitecture;

namespace RonSijm.IJzer.Core.ProjectArchitecture;

public readonly struct ProjectGroup(
    string name,
    ImmutableArray<ProjectMatcher> matchers,
    string? description,
    string xmlPath,
    int xmlLineNumber,
    int xmlLinePosition)
{
    public string Name { get; } = name;

    public ImmutableArray<ProjectMatcher> Matchers { get; } = matchers;

    public string? Description { get; } = description;

    public string XmlPath { get; } = xmlPath;

    public int XmlLineNumber { get; } = xmlLineNumber;

    public int XmlLinePosition { get; } = xmlLinePosition;
}