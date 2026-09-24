using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Model;

namespace RonSijm.AnaalIJzer.Core.SemanticOperations.Matching;

public readonly struct SemanticOperationMatcher(
    ImmutableHashSet<SemanticOperationKind> kinds,
    ImmutableArray<MatchCondition> containingTypeConditions,
    ImmutableArray<MatchCondition> memberConditions,
    bool? requireStaticAccess = null,
    ImmutableHashSet<SemanticOperationMemberKind>? memberKinds = null)
{
    public ImmutableHashSet<SemanticOperationKind> Kinds { get; } = kinds;

    public ImmutableArray<MatchCondition> ContainingTypeConditions { get; } = containingTypeConditions;

    public ImmutableArray<MatchCondition> MemberConditions { get; } = memberConditions;

    public bool? RequireStaticAccess { get; } = requireStaticAccess;

    public ImmutableHashSet<SemanticOperationMemberKind> MemberKinds { get; } = memberKinds ?? ImmutableHashSet<SemanticOperationMemberKind>.Empty;

    public bool Matches(SemanticOperation operation)
    {
        if (Kinds.Count > 0 && !Kinds.Contains(operation.Kind))
        {
            return false;
        }

        if (RequireStaticAccess.HasValue && operation.IsStaticAccess != RequireStaticAccess.Value)
        {
            return false;
        }

        var matcher = new SemanticSymbolMatcher(ContainingTypeConditions, MemberConditions, MemberKinds);
        var result = matcher.Matches(operation.SelectedSymbol);

        return result;
    }
}