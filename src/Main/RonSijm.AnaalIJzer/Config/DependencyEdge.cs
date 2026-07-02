namespace RonSijm.AnaalIJzer.Config;

internal enum DependencyRuleKind
{
	Allowed,
	Blocked
}

internal readonly struct DependencyEdge(string scopePath, string from, string to, string configuredFrom, string configuredTo, DependencySiteFilter siteFilter, DependencyRuleKind kind, string xmlPath, int xmlLineNumber, int xmlLinePosition)
{
	public string ScopePath { get; } = scopePath;

	public string From { get; } = from;

	public string To { get; } = to;

	public string ConfiguredFrom { get; } = configuredFrom;

	public string ConfiguredTo { get; } = configuredTo;

	public DependencySiteFilter SiteFilter { get; } = siteFilter;

	public DependencyRuleKind Kind { get; } = kind;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public bool IsAllowed => Kind == DependencyRuleKind.Allowed;

	public bool IsBlocked => Kind == DependencyRuleKind.Blocked;

	public bool IsExplicit => From != "*" && To != "*";

	public bool IsWildcardTarget => From == "*" && To != "*";

	public bool IsWildcardSource => From != "*" && To == "*";

	public bool IsAllowAny => From == "*" && To == "*";

	public bool AllowsSite(string site) => SiteFilter.Allows(site);

	public string ToXmlText() => $"<{(IsBlocked ? "BlockedDependency" : "AllowedDependency")} from=\"{ConfiguredFrom}\" to=\"{ConfiguredTo}\"/>";
}
