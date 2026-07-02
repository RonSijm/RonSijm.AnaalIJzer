using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Matching;

/// <summary>A configured layer boundary together with its matchers and nested layers.</summary>
internal sealed class LayerNode
{
	public LayerNode(LayerDefinition definition, ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> matchers, ImmutableArray<LayerNode> children, ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> allowedTypeMatchers, ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> forbiddenTypeMatchers)
	{
		Definition = definition;
		Matchers = matchers;
		Children = children;
		AllowedTypeMatchers = allowedTypeMatchers;
		ForbiddenTypeMatchers = forbiddenTypeMatchers;
	}

	public LayerDefinition Definition { get; }

	public ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> Matchers { get; }

	public ImmutableArray<LayerNode> Children { get; }

	public ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> AllowedTypeMatchers { get; }

	public ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> ForbiddenTypeMatchers { get; }

	public bool HasMatchers => !Matchers.IsDefaultOrEmpty;
}
