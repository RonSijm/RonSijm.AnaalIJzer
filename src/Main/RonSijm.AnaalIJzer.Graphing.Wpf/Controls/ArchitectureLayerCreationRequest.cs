using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Controls;

public sealed class ArchitectureLayerCreationRequest(
    string name,
    string matcherKind,
    ImmutableDictionary<string, string> matcherAttributes)
{
    public string Name { get; } = name;

    public string MatcherKind { get; } = matcherKind;

    public ImmutableDictionary<string, string> MatcherAttributes { get; } = matcherAttributes;
}
