using System.Collections.Immutable;
using System.Text.RegularExpressions;
using RonSijm.IJzer.Core.Matchers.Conditions;

namespace RonSijm.IJzer.Core.Observations;

public readonly struct GeneratedCodePathRule(ImmutableArray<MatchCondition> conditions)
{
    public ImmutableArray<MatchCondition> Conditions { get; } = conditions;

    public bool Matches(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || Conditions.IsDefaultOrEmpty)
        {
            return false;
        }

        var normalizedPath = filePath!.Replace('/', '\\');
        foreach (var condition in Conditions)
        {
            if (!condition.MatchesString(normalizedPath, StringComparison.Ordinal, RegexOptions.CultureInvariant))
            {
                return false;
            }
        }

        var result = true;

        return result;
    }
}