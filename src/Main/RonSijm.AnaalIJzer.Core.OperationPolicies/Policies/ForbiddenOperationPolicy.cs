using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Model;

namespace RonSijm.AnaalIJzer.Core.OperationPolicies.Policies;

public readonly struct ForbiddenOperationPolicy(
    string ownerLayerPath,
    ImmutableArray<ForbiddenOperationRule> rules,
    string? description,
    string xmlPath,
    int xmlLineNumber,
    int xmlLinePosition)
{
    public string OwnerLayerPath { get; } = ownerLayerPath;

    public ImmutableArray<ForbiddenOperationRule> Rules { get; } = rules;

    public string? Description { get; } = description;

    public string XmlPath { get; } = xmlPath;

    public int XmlLineNumber { get; } = xmlLineNumber;

    public int XmlLinePosition { get; } = xmlLinePosition;

    public ForbiddenOperationPolicyEvaluation? Evaluate(SemanticOperation operation)
    {
        foreach (var rule in Rules)
        {
            if (!rule.Matches(operation))
            {
                continue;
            }

            var result = new ForbiddenOperationPolicyEvaluation(
                this,
                rule,
                $"the ForbiddenOperations policy in layer '{OwnerLayerPath}' blocks {operation.DisplayName} at {operation.Site}");

            return result;
        }

        return null;
    }
}