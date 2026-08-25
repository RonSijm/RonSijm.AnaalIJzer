using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Conditions;

namespace RonSijm.AnaalIJzer.Engine.NameRules;

public readonly struct NameRuleSubject
{
	public NameRuleSubject(string displayName, ImmutableArray<string> candidateNames, ITypeSymbol? symbol) : this(NameRuleSubjectKind.ValueName, displayName, candidateNames, string.Empty, symbol)
	{
	}

	public NameRuleSubject(NameRuleSubjectKind kind, string displayName, ImmutableArray<string> candidateNames, string namespaceName, ITypeSymbol? symbol)
	{
		Kind = kind;
		DisplayName = displayName;
		CandidateNames = candidateNames.IsDefaultOrEmpty ? [displayName] : candidateNames;
		NamespaceName = namespaceName;
		Symbol = symbol;
		NormalizedName = NameRuleNameNormalizer.Normalize(displayName);
	}

	public NameRuleSubjectKind Kind { get; }
	public string DisplayName { get; }
	public ImmutableArray<string> CandidateNames { get; }
	public string NamespaceName { get; }
	public ITypeSymbol? Symbol { get; }
	public string NormalizedName { get; }

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
