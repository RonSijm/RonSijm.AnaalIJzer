using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Matchers.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture.SolutionTopology;

public readonly struct SolutionModule(
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

    public bool Matches(string projectName)
    {
        var result = Matchers.Any(matcher => matcher.Matches(projectName));

        return result;
    }
}