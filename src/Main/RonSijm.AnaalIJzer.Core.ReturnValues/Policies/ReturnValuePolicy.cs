using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RonSijm.AnaalIJzer.Core.ReturnValues.Policies;

public readonly struct ReturnValuePolicy(
    string ownerLayerPath,
    ImmutableArray<ReturnValueRule> rules,
    string? description,
    string xmlPath,
    int xmlLineNumber,
    int xmlLinePosition,
    ImmutableArray<ReturnValueRule> allowedRules = default,
    bool isGlobal = false)
{
    public string OwnerLayerPath { get; } = ownerLayerPath;

    public bool IsGlobal { get; } = isGlobal;

    /// <summary>Direct returned expressions that this policy rejects.</summary>
    public ImmutableArray<ReturnValueRule> Rules { get; } = rules;

    /// <summary>Direct returned expressions that this policy permits when it acts as an allow-list.</summary>
    public ImmutableArray<ReturnValueRule> AllowedRules { get; } = allowedRules.IsDefault ? ImmutableArray<ReturnValueRule>.Empty : allowedRules;

    public string? Description { get; } = description;

    public string XmlPath { get; } = xmlPath;

    public int XmlLineNumber { get; } = xmlLineNumber;

    public int XmlLinePosition { get; } = xmlLinePosition;

    public ReturnValuePolicyEvaluation? Evaluate(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        foreach (var rule in Rules)
        {
            if (!rule.Matches(expression, semanticModel, cancellationToken))
            {
                continue;
            }

            var result = new ReturnValuePolicyEvaluation(
                this,
                rule,
                ReturnValuePolicyRuleMode.Forbidden,
                $"the ReturnValuePolicy in {GetOwnerScopeDescription()} blocks returned {rule.DisplayName}");

            return result;
        }

        if (!AllowedRules.IsDefaultOrEmpty && !AllowedRules.Any(rule => rule.Matches(expression, semanticModel, cancellationToken)))
        {
            var result = new ReturnValuePolicyEvaluation(
                this,
                AllowedRules[0],
                ReturnValuePolicyRuleMode.Allowed,
                $"the ReturnValuePolicy in {GetOwnerScopeDescription()} permits only direct returned {CreateAllowedRuleDescription()}");

            return result;
        }

        return null;
    }

    private string GetOwnerScopeDescription()
    {
        var result = IsGlobal ? "the global configuration" : $"layer '{OwnerLayerPath}'";

        return result;
    }

    private string CreateAllowedRuleDescription()
    {
        var descriptions = AllowedRules
            .Select(rule => rule.DisplayName.StartsWith("any ", StringComparison.Ordinal) ? rule.DisplayName.Substring("any ".Length) : rule.DisplayName)
            .ToArray();
        var result = descriptions.Length == 1
            ? descriptions[0]
            : string.Join(" or ", descriptions);

        return result;
    }
}