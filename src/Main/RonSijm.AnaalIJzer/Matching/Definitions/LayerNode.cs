using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Conditions;

namespace RonSijm.AnaalIJzer.Definitions;

/// <summary>A configured layer boundary together with its matchers and nested layers.</summary>
internal sealed class LayerNode(
    LayerDefinition definition,
    ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> matchers,
    ImmutableArray<LayerNode> children,
    ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> allowedTypeMatchers,
    ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> forbiddenTypeMatchers)
{
    public LayerDefinition Definition { get; } = definition;

    public ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> Matchers { get; } = matchers;

    public ImmutableArray<LayerNode> Children { get; } = children;

    public ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> AllowedTypeMatchers { get; } = allowedTypeMatchers;

    public ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> ForbiddenTypeMatchers { get; } = forbiddenTypeMatchers;

    public bool HasMatchers
    {
        get { return !Matchers.IsDefaultOrEmpty; }
    }
}
