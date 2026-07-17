using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Conditions;

namespace RonSijm.AnaalIJzer;

/// <summary>
///     A pattern rule containing one or more conditions. Every condition must match.
/// </summary>
internal readonly struct PatternMatcher(MatchTarget target, ImmutableArray<MatchCondition> conditions)
{
	public PatternMatcher(MatchTarget target, MatchKind kind, string value) : this(target, [new MatchCondition(kind, value)])
	{
	}

    public MatchTarget Target { get; } = target;

    public ImmutableArray<MatchCondition> Conditions { get; } = conditions;

    public bool IsExactTypeName => Target == MatchTarget.TypeName && Conditions.Any(condition => condition.Kind == MatchKind.Equals);
	public bool IsPureExactTypeName => IsExactTypeName && Conditions.Length == 1;

	/// <summary>
	///     Attempts to match against <paramref name="typeName" /> / <paramref name="namespaceName" />,
	///     and against the semantic <paramref name="symbol" /> when a condition requires it.
	///     Returns <see langword="null" /> on no match.
	///     Returns the matched type-name suffix for code-fix rename when one condition is
	///     <see cref="MatchKind.EndsWith" />; otherwise returns <see cref="string.Empty" />.
	/// </summary>
	public string? TryMatch(string typeName, string namespaceName, ITypeSymbol? symbol = null)
	{
		if (Conditions.IsDefaultOrEmpty)
		{
			return null;
		}

		string? matchedSuffix = null;
		foreach (var condition in Conditions)
		{
			if (!condition.Matches(Target, typeName, namespaceName, symbol))
			{
				return null;
			}

			if (Target == MatchTarget.TypeName && condition.Kind == MatchKind.EndsWith)
			{
				matchedSuffix = condition.Value;
			}
		}

		var result = matchedSuffix ?? string.Empty;

		return result;
	}
}
