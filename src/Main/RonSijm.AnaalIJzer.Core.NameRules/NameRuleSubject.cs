using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Matchers;

namespace RonSijm.AnaalIJzer.Core.NameRules;

public readonly struct NameRuleSubject(
	NameRuleSubjectKind kind,
	string displayName,
	ImmutableArray<string> candidateNames,
	string namespaceName,
	ITypeSymbol? symbol)
{
	public NameRuleSubject(string displayName, ImmutableArray<string> candidateNames, ITypeSymbol? symbol) : this(NameRuleSubjectKind.ValueName, displayName, candidateNames, string.Empty, symbol)
	{
	}

	public NameRuleSubjectKind Kind { get; } = kind;
	public string DisplayName { get; } = displayName;
	public ImmutableArray<string> CandidateNames { get; } = candidateNames.IsDefaultOrEmpty ? [displayName] : candidateNames;
	public string NamespaceName { get; } = namespaceName;
	public ITypeSymbol? Symbol { get; } = symbol;
	public string NormalizedName { get; } = NameRuleNameNormalizer.Normalize(displayName);

	public bool Matches(PatternMatcher matcher)
	{
		foreach (var candidateName in CandidateNames)
		{
			if (matcher.TryMatch(candidateName, NamespaceName, Symbol) is not null)
			{
				return true;
			}
		}

		return false;
	}
}
