using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.NameRules;

/// <summary>A value movement recovered through an unambiguous local alias in one bounded body analysis.</summary>
public readonly struct NameRuleProvenanceFlow(NameRuleSubject source, NameRuleSubject? immediateSource, NameRuleSubject target, string site, Location location)
{
    public NameRuleSubject Source { get; } = source;
    public NameRuleSubject? ImmediateSource { get; } = immediateSource;
    public NameRuleSubject Target { get; } = target;
    public string Site { get; } = site;
    public Location Location { get; } = location;
}