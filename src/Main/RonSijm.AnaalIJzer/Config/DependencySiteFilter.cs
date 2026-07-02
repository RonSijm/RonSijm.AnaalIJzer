using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Diagnostics;

namespace RonSijm.AnaalIJzer.Config;

internal readonly struct DependencySiteFilter
{
	private static readonly ImmutableHashSet<string> EmptySites = ImmutableHashSet.Create<string>(StringComparer.Ordinal);
	private readonly ImmutableHashSet<string>? _allowedSites;
	private readonly ImmutableHashSet<string>? _blockedSites;

	public DependencySiteFilter(ImmutableHashSet<string> allowedSites, ImmutableHashSet<string> blockedSites)
	{
		_allowedSites = allowedSites;
		_blockedSites = blockedSites;
	}

	public static DependencySiteFilter All { get; } = new(EmptySites, EmptySites);

	public ImmutableHashSet<string> AllowedSites => _allowedSites ?? EmptySites;

	public ImmutableHashSet<string> BlockedSites => _blockedSites ?? EmptySites;

	public bool HasFilter => AllowedSites.Count > 0 || BlockedSites.Count > 0;

	public bool Allows(string site) =>
		AllowedSites.Count > 0 ? AllowedSites.Contains(site) : !BlockedSites.Contains(site);

	public string GetDenialReason(string site)
	{
		if (AllowedSites.Count > 0 && !AllowedSites.Contains(site))
		{
			return $"allowedSites does not include {site}";
		}

		if (BlockedSites.Contains(site))
		{
			return $"blockedSites blocks {site}";
		}

		return string.Empty;
	}

	public string ToDisplayText()
	{
		if (AllowedSites.Count > 0)
		{
			return "allowed sites: " + FormatSites(AllowedSites);
		}

		if (BlockedSites.Count > 0)
		{
			return "blocked sites: " + FormatSites(BlockedSites);
		}

		return string.Empty;
	}

	private static string FormatSites(ImmutableHashSet<string> sites) =>
		string.Join(", ", DependencySites.All.Where(sites.Contains));
}
