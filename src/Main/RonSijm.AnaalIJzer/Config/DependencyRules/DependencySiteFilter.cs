using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.DependencyRules;

internal readonly struct DependencySiteFilter(
    ImmutableHashSet<string> allowedSites,
    ImmutableHashSet<string> blockedSites)
{
	private static readonly ImmutableHashSet<string> EmptySites = ImmutableHashSet.Create<string>(StringComparer.Ordinal);
	private readonly ImmutableHashSet<string>? _allowedSites = allowedSites;
	private readonly ImmutableHashSet<string>? _blockedSites = blockedSites;

    public static DependencySiteFilter All { get; } = new(EmptySites, EmptySites);

	public ImmutableHashSet<string> AllowedSites
    {
        get { return _allowedSites ?? EmptySites; }
    }

    public ImmutableHashSet<string> BlockedSites
    {
        get { return _blockedSites ?? EmptySites; }
    }

    public bool HasFilter
    {
        get { return AllowedSites.Count > 0 || BlockedSites.Count > 0; }
    }

	public bool Allows(string site)
	{
		var result = AllowedSites.Count > 0 ? AllowedSites.Contains(site) : !BlockedSites.Contains(site);

		return result;
	}

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

	private static string FormatSites(ImmutableHashSet<string> sites)
	{
		var result = string.Join(", ", DependencySites.All.Where(sites.Contains));

		return result;
	}
}
