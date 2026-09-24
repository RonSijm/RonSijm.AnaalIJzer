using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.AssemblyAttributes.Model;

/// <summary>One independently composable allow/block policy for assembly-level attributes.</summary>
public readonly struct AssemblyAttributePolicy(
    ImmutableArray<AssemblyAttributeRule> allowedRules,
    ImmutableArray<AssemblyAttributeRule> forbiddenRules,
    string? description,
    string xmlPath,
    int xmlLineNumber,
    int xmlLinePosition)
{
    public ImmutableArray<AssemblyAttributeRule> AllowedRules { get; } = allowedRules.IsDefault ? [] : allowedRules;

    public ImmutableArray<AssemblyAttributeRule> ForbiddenRules { get; } = forbiddenRules.IsDefault ? [] : forbiddenRules;

    public string? Description { get; } = description;

    public string XmlPath { get; } = xmlPath;

    public int XmlLineNumber { get; } = xmlLineNumber;

    public int XmlLinePosition { get; } = xmlLinePosition;

    public AssemblyAttributePolicyEvaluation? Evaluate(AttributeData attribute)
    {
        foreach (var rule in ForbiddenRules)
        {
            if (!rule.Matches(attribute))
            {
                continue;
            }

            var blockedResult = new AssemblyAttributePolicyEvaluation(this, rule, $"the AssemblyAttributePolicy blocks {rule.DisplayName}");

            return blockedResult;
        }

        var applicableAllowedRules = AllowedRules
            .Where(rule => rule.MatchesAttribute(attribute))
            .ToImmutableArray();
        if (applicableAllowedRules.IsDefaultOrEmpty || applicableAllowedRules.Any(rule => rule.Matches(attribute)))
        {
            return null;
        }

        var allowedRule = applicableAllowedRules[0];
        var allowedResult = new AssemblyAttributePolicyEvaluation(this, allowedRule, $"the AssemblyAttributePolicy has no allowed rule for this attribute value; expected {allowedRule.DisplayName}");

        return allowedResult;
    }
}