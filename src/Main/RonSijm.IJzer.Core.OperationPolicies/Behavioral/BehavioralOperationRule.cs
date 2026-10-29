using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.IJzer.Core.DependencyRules;
using RonSijm.IJzer.Core.SemanticOperations.Matching;
using RonSijm.IJzer.Core.SemanticOperations.Model;

namespace RonSijm.IJzer.Core.OperationPolicies.Behavioral;

public readonly struct BehavioralOperationRule(
    BehavioralOperationRuleKind kind,
    SemanticDeclarationMatcher declarationMatcher,
    ImmutableArray<SemanticOperationMatcher> operationMatchers,
    ImmutableArray<SemanticOperationMatcher> relatedOperationMatchers,
    DependencySiteFilter siteFilter,
    BehavioralOperationOrdering ordering,
    int maximumCount,
    string displayName,
    string? description,
    string xmlPath,
    int xmlLineNumber,
    int xmlLinePosition)
{
    public BehavioralOperationRuleKind Kind { get; } = kind;

    public SemanticDeclarationMatcher DeclarationMatcher { get; } = declarationMatcher;

    public ImmutableArray<SemanticOperationMatcher> OperationMatchers { get; } = operationMatchers;

    public ImmutableArray<SemanticOperationMatcher> RelatedOperationMatchers { get; } = relatedOperationMatchers;

    public DependencySiteFilter SiteFilter { get; } = siteFilter;

    public BehavioralOperationOrdering Ordering { get; } = ordering;

    public int MaximumCount { get; } = maximumCount;

    public string DisplayName { get; } = displayName;

    public string? Description { get; } = description;

    public string XmlPath { get; } = xmlPath;

    public int XmlLineNumber { get; } = xmlLineNumber;

    public int XmlLinePosition { get; } = xmlLinePosition;

    public bool AppliesTo(ISymbol owningSymbol)
    {
        var result = DeclarationMatcher.Matches(owningSymbol);

        return result;
    }

    public bool MatchesOperation(SemanticOperationOccurrence occurrence)
    {
        if (!SiteFilter.Allows(occurrence.Operation.Site))
        {
            return false;
        }

        var result = MatchesAny(OperationMatchers, occurrence.Operation);

        return result;
    }

    public bool MatchesRelatedOperation(SemanticOperationOccurrence occurrence)
    {
        if (!SiteFilter.Allows(occurrence.Operation.Site))
        {
            return false;
        }

        var result = MatchesAny(RelatedOperationMatchers, occurrence.Operation);

        return result;
    }

    private static bool MatchesAny(ImmutableArray<SemanticOperationMatcher> matchers, SemanticOperation operation)
    {
        foreach (var matcher in matchers)
        {
            if (matcher.Matches(operation))
            {
                return true;
            }
        }

        return false;
    }
}