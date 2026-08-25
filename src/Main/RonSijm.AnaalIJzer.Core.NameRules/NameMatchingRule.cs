using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.Engine.DependencyRules;

namespace RonSijm.AnaalIJzer.Engine.NameRules;

public sealed class NameMatchingRule(
	NameRuleKind kind,
	NameRuleTrigger trigger,
	ImmutableArray<PatternMatcher> nameMatchers,
	ImmutableArray<PatternMatcher> sourceMatchers,
	ImmutableArray<PatternMatcher> targetMatchers,
	ImmutableArray<NameRuleAllowMapping> allowMappings,
	DependencySiteFilter siteFilter,
	string layerName,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public NameRuleKind Kind { get; } = kind;
	public NameRuleTrigger Trigger { get; } = trigger;
	public ImmutableArray<PatternMatcher> NameMatchers { get; } = nameMatchers;
	public ImmutableArray<PatternMatcher> SourceMatchers { get; } = sourceMatchers;
	public ImmutableArray<PatternMatcher> TargetMatchers { get; } = targetMatchers;
	public ImmutableArray<NameRuleAllowMapping> AllowMappings { get; } = allowMappings;
	public DependencySiteFilter SiteFilter { get; } = siteFilter;
	public string LayerName { get; } = layerName;
	public string? Description { get; } = description;
	public string XmlPath { get; } = xmlPath;
	public int XmlLineNumber { get; } = xmlLineNumber;
	public int XmlLinePosition { get; } = xmlLinePosition;

	public NameRuleViolation? Evaluate(NameRuleSubject source, NameRuleSubject target, string site)
	{
		if (!SiteFilter.Allows(site) || !AppliesTo(source, target))
		{
			return null;
		}

		if (string.Equals(source.NormalizedName, target.NormalizedName, StringComparison.Ordinal))
		{
			return null;
		}

		string? siteRejectedAllowReason = null;
		foreach (var allowMapping in AllowMappings)
		{
			if (!allowMapping.MatchesPair(source, target))
			{
				continue;
			}

			if (allowMapping.AllowsSite(site))
			{
				return null;
			}

			siteRejectedAllowReason ??= allowMapping.GetSiteDenialReason(site);
		}

		var reason = CreateReason(source, target, siteRejectedAllowReason);
		var result = new NameRuleViolation(Kind, source.DisplayName, target.DisplayName, source.NormalizedName, target.NormalizedName, site, LayerName, reason, XmlPath, XmlLineNumber, XmlLinePosition);

		return result;
	}

	private bool AppliesTo(NameRuleSubject source, NameRuleSubject target)
	{
		if (!MatchesAllConfigured(SourceMatchers, source))
		{
			return false;
		}

		if (!MatchesAllConfigured(TargetMatchers, target))
		{
			return false;
		}

		var result = NameMatchers.IsDefaultOrEmpty || MatchesAny(NameMatchers, source) || MatchesAny(NameMatchers, target);

		return result;
	}

	private static bool MatchesAllConfigured(ImmutableArray<PatternMatcher> matchers, NameRuleSubject subject)
	{
		var result = matchers.IsDefaultOrEmpty || MatchesAny(matchers, subject);

		return result;
	}

	private static bool MatchesAny(ImmutableArray<PatternMatcher> matchers, NameRuleSubject subject)
	{
		var result = matchers.Any(subject.Matches);

		return result;
	}

	private string CreateReason(NameRuleSubject source, NameRuleSubject target, string? siteRejectedAllowReason)
	{
		var reason = Kind == NameRuleKind.RequireDeclarationNameMatchesType
			? $"type '{source.DisplayName}' normalizes to '{source.NormalizedName}', declaration name '{target.DisplayName}' normalizes to '{target.NormalizedName}'"
			: $"source '{source.DisplayName}' normalizes to '{source.NormalizedName}', target '{target.DisplayName}' normalizes to '{target.NormalizedName}'";
		if (!string.IsNullOrWhiteSpace(siteRejectedAllowReason))
		{
			reason += $"; a matching <Allow> mapping is configured, but {siteRejectedAllowReason}";
		}

		return reason;
	}
}
