using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Model;

namespace RonSijm.AnaalIJzer.Core.SemanticOperations.Matching;

/// <summary>Matches the declaration that owns an operation body.</summary>
public readonly struct SemanticDeclarationMatcher(
    ImmutableArray<MatchCondition> containingTypeConditions,
    ImmutableArray<MatchCondition> memberConditions,
    ImmutableHashSet<SemanticOperationMemberKind> memberKinds)
{
    public ImmutableArray<MatchCondition> ContainingTypeConditions { get; } = containingTypeConditions;

    public ImmutableArray<MatchCondition> MemberConditions { get; } = memberConditions;

    public ImmutableHashSet<SemanticOperationMemberKind> MemberKinds { get; } = memberKinds;

    public bool Matches(ISymbol symbol)
    {
        var matcher = new SemanticSymbolMatcher(ContainingTypeConditions, MemberConditions, MemberKinds);
        var result = matcher.Matches(symbol);

        return result;
    }
}