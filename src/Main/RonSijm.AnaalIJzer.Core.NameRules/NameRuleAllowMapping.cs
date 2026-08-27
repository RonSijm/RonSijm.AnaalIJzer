using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.Matchers;

namespace RonSijm.AnaalIJzer.Core.NameRules;

public sealed class NameRuleAllowMapping(
	ImmutableArray<PatternMatcher> sourceMatchers,
	ImmutableArray<PatternMatcher> targetMatchers,
	DependencySiteFilter siteFilter,
	string? description)
{
	public ImmutableArray<PatternMatcher> SourceMatchers { get; } = sourceMatchers;
	public ImmutableArray<PatternMatcher> TargetMatchers { get; } = targetMatchers;
	public DependencySiteFilter SiteFilter { get; } = siteFilter;
	public string? Description { get; } = description;

	public bool MatchesPair(NameRuleSubject source, NameRuleSubject target)
	{
		var result = MatchesAny(SourceMatchers, source) && MatchesAny(TargetMatchers, target);

		return result;
	}

	public bool AllowsSite(string site)
	{
		var result = SiteFilter.Allows(site);

		return result;
	}

	public string GetSiteDenialReason(string site)
	{
		var result = SiteFilter.GetDenialReason(site);

		return result;
	}

	private static bool MatchesAny(ImmutableArray<PatternMatcher> matchers, NameRuleSubject subject)
	{
		if (matchers.IsDefaultOrEmpty)
		{
			return false;
		}

		var result = matchers.Any(subject.Matches);

		return result;
	}
}
