using System.Collections.Immutable;
using System.Text.RegularExpressions;
using RonSijm.IJzer.Core.Matchers.Conditions;

namespace RonSijm.IJzer.Core.Matchers.ProjectArchitecture;

public readonly struct ProjectMatcher(ImmutableArray<MatchCondition> conditions)
{
    public ImmutableArray<MatchCondition> Conditions { get; } = conditions;

    public bool Matches(string projectName)
    {
        if (Conditions.IsDefaultOrEmpty)
        {
            return false;
        }

        foreach (var condition in Conditions)
        {
            if (!condition.MatchesString(projectName, StringComparison.Ordinal, RegexOptions.CultureInvariant))
            {
                return false;
            }
        }

        var result = true;

        return result;
    }
}