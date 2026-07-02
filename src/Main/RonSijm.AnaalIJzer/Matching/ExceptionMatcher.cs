using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Matching;

/// <summary>
///     A matcher inside an <c>&lt;Exceptions&gt;</c> tree. Matching depth determines whether the
///     type is excluded from, or re-included in, the parent rule.
/// </summary>
internal readonly struct ExceptionMatcher(PatternMatcher matcher, ImmutableArray<ExceptionMatcher> exceptions)
{
	public PatternMatcher Matcher { get; } = matcher;

	public ImmutableArray<ExceptionMatcher> Exceptions { get; } = exceptions;

	public int FindDeepestMatchingDepth(string typeName, string namespaceName, ITypeSymbol? symbol, int depth)
	{
		var deepest = Matcher.TryMatch(typeName, namespaceName, symbol) is null ? 0 : depth;
		if (Exceptions.IsDefaultOrEmpty)
		{
			return deepest;
		}

		foreach (var exception in Exceptions)
		{
			var nestedDepth = exception.FindDeepestMatchingDepth(typeName, namespaceName, symbol, depth + 1);
			if (nestedDepth > deepest)
			{
				deepest = nestedDepth;
			}
		}

		return deepest;
	}
}
