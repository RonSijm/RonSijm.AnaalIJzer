using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.OperationPolicies.Behavioral;

/// <summary>Evaluates mechanically provable operation-presence, ordering, and count rules for one layer.</summary>
public readonly struct BehavioralOperationPolicy(
    string ownerLayerPath,
    ImmutableArray<BehavioralOperationRule> rules,
    string? description,
    string xmlPath,
    int xmlLineNumber,
    int xmlLinePosition)
{
    public string OwnerLayerPath { get; } = ownerLayerPath;

    public ImmutableArray<BehavioralOperationRule> Rules { get; } = rules;

    public string? Description { get; } = description;

    public string XmlPath { get; } = xmlPath;

    public int XmlLineNumber { get; } = xmlLineNumber;

    public int XmlLinePosition { get; } = xmlLinePosition;

    public ImmutableArray<BehavioralOperationPolicyEvaluation> Evaluate(BehavioralOperationBodyAnalysis body)
    {
        var evaluations = ImmutableArray.CreateBuilder<BehavioralOperationPolicyEvaluation>();
        foreach (var rule in Rules)
        {
            if (!rule.AppliesTo(body.OwningSymbol))
            {
                continue;
            }

            switch (rule.Kind)
            {
                case BehavioralOperationRuleKind.RequiredOperation:
                    EvaluateRequiredOperation(rule, body, evaluations);
                    break;
                case BehavioralOperationRuleKind.RequiredOperationBefore:
                    EvaluateRequiredOperationBefore(rule, body, evaluations);
                    break;
                case BehavioralOperationRuleKind.ForbiddenOperationAfter:
                    EvaluateForbiddenOperationAfter(rule, body, evaluations);
                    break;
                case BehavioralOperationRuleKind.MaximumOperationCount:
                    EvaluateMaximumOperationCount(rule, body, evaluations);
                    break;
            }
        }

        var result = evaluations.ToImmutable();

        return result;
    }

    private void EvaluateRequiredOperation(BehavioralOperationRule rule, BehavioralOperationBodyAnalysis body, ImmutableArray<BehavioralOperationPolicyEvaluation>.Builder evaluations)
    {
        var matches = FindOperationMatches(rule, body);
        if (matches.IsDefaultOrEmpty)
        {
            evaluations.Add(CreateEvaluation(rule, BehavioralOperationViolationKind.MissingRequiredOperation, $"the BehavioralOperations policy in layer '{OwnerLayerPath}' requires {rule.DisplayName} in declaration '{FormatDeclaration(body.OwningSymbol)}'", null));

            return;
        }

        if (rule.Ordering != BehavioralOperationOrdering.Dominance || matches.Any(body.DominatesEveryExit))
        {
            return;
        }

        evaluations.Add(CreateEvaluation(rule, BehavioralOperationViolationKind.RequiredOperationDoesNotDominateExit, $"the BehavioralOperations policy in layer '{OwnerLayerPath}' requires {rule.DisplayName} to execute on every path through declaration '{FormatDeclaration(body.OwningSymbol)}'", null));
    }

    private void EvaluateRequiredOperationBefore(BehavioralOperationRule rule, BehavioralOperationBodyAnalysis body, ImmutableArray<BehavioralOperationPolicyEvaluation>.Builder evaluations)
    {
        var requiredOperations = FindOperationMatches(rule, body);
        var targetOperations = FindRelatedOperationMatches(rule, body);
        foreach (var targetOperation in targetOperations)
        {
            if (requiredOperations.Any(requiredOperation => IsBefore(rule, body, requiredOperation, targetOperation)))
            {
                continue;
            }

            evaluations.Add(CreateEvaluation(rule, BehavioralOperationViolationKind.MissingRequiredOperationBefore, $"the BehavioralOperations policy in layer '{OwnerLayerPath}' requires {rule.DisplayName} before {targetOperation.Operation.DisplayName} in declaration '{FormatDeclaration(body.OwningSymbol)}'", targetOperation));
        }
    }

    private void EvaluateForbiddenOperationAfter(BehavioralOperationRule rule, BehavioralOperationBodyAnalysis body, ImmutableArray<BehavioralOperationPolicyEvaluation>.Builder evaluations)
    {
        var forbiddenOperations = FindOperationMatches(rule, body);
        var terminalOperations = FindRelatedOperationMatches(rule, body);
        foreach (var forbiddenOperation in forbiddenOperations)
        {
            if (!terminalOperations.Any(terminalOperation => IsBefore(rule, body, terminalOperation, forbiddenOperation)))
            {
                continue;
            }

            evaluations.Add(CreateEvaluation(rule, BehavioralOperationViolationKind.ForbiddenOperationAfter, $"the BehavioralOperations policy in layer '{OwnerLayerPath}' forbids {forbiddenOperation.Operation.DisplayName} after the configured terminal operation in declaration '{FormatDeclaration(body.OwningSymbol)}'", forbiddenOperation));
        }
    }

    private void EvaluateMaximumOperationCount(BehavioralOperationRule rule, BehavioralOperationBodyAnalysis body, ImmutableArray<BehavioralOperationPolicyEvaluation>.Builder evaluations)
    {
        var operations = FindOperationMatches(rule, body)
            .OrderBy(occurrence => occurrence.SourcePosition)
            .ThenBy(occurrence => occurrence.SourceOrder)
            .ToArray();
        if (operations.Length <= rule.MaximumCount)
        {
            return;
        }

        foreach (var operation in operations.Skip(rule.MaximumCount))
        {
            evaluations.Add(CreateEvaluation(rule, BehavioralOperationViolationKind.MaximumOperationCountExceeded, $"the BehavioralOperations policy in layer '{OwnerLayerPath}' allows at most {rule.MaximumCount} occurrence(s) of {rule.DisplayName} in declaration '{FormatDeclaration(body.OwningSymbol)}', but found {operations.Length}", operation));
        }
    }

    private BehavioralOperationPolicyEvaluation CreateEvaluation(BehavioralOperationRule rule, BehavioralOperationViolationKind violationKind, string reason, SemanticOperationOccurrence? occurrence)
    {
        var result = new BehavioralOperationPolicyEvaluation(this, rule, violationKind, reason, occurrence);

        return result;
    }

    private static ImmutableArray<SemanticOperationOccurrence> FindOperationMatches(BehavioralOperationRule rule, BehavioralOperationBodyAnalysis body)
    {
        var result = body.Operations.Where(rule.MatchesOperation).ToImmutableArray();

        return result;
    }

    private static ImmutableArray<SemanticOperationOccurrence> FindRelatedOperationMatches(BehavioralOperationRule rule, BehavioralOperationBodyAnalysis body)
    {
        var result = body.Operations.Where(rule.MatchesRelatedOperation).ToImmutableArray();

        return result;
    }

    private static bool IsBefore(BehavioralOperationRule rule, BehavioralOperationBodyAnalysis body, SemanticOperationOccurrence first, SemanticOperationOccurrence second)
    {
        var result = rule.Ordering == BehavioralOperationOrdering.Lexical
            ? first.IsLexicallyBefore(second)
            : body.Dominates(first, second);

        return result;
    }

    private static string FormatDeclaration(ISymbol symbol)
    {
        var result = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

        return result;
    }
}