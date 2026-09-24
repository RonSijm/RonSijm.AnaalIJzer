using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Matchers;

namespace RonSijm.AnaalIJzer.Core.AssemblyAttributes.Model;

/// <summary>Selects one assembly attribute and, optionally, all of its required argument values.</summary>
public readonly struct AssemblyAttributeRule(
    PatternMatcher attributeMatcher,
    ImmutableArray<AssemblyAttributeArgumentMatcher> argumentMatchers,
    string displayName,
    string? description,
    string xmlPath,
    int xmlLineNumber,
    int xmlLinePosition)
{
    public PatternMatcher AttributeMatcher { get; } = attributeMatcher;

    public ImmutableArray<AssemblyAttributeArgumentMatcher> ArgumentMatchers { get; } = argumentMatchers.IsDefault ? [] : argumentMatchers;

    public string DisplayName { get; } = displayName;

    public string? Description { get; } = description;

    public string XmlPath { get; } = xmlPath;

    public int XmlLineNumber { get; } = xmlLineNumber;

    public int XmlLinePosition { get; } = xmlLinePosition;

    public bool Matches(AttributeData attribute)
    {
        if (!MatchesAttribute(attribute))
        {
            return false;
        }

        var result = ArgumentMatchers.All(matcher => matcher.Matches(attribute));

        return result;
    }

    public bool MatchesAttribute(AttributeData attribute)
    {
        var attributeType = attribute.AttributeClass;
        if (attributeType is null)
        {
            return false;
        }

        var namespaceName = attributeType.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : attributeType.ContainingNamespace.ToDisplayString();
        var result = AttributeMatcher.TryMatch(attributeType.Name, namespaceName, attributeType) is not null;

        return result;
    }
}