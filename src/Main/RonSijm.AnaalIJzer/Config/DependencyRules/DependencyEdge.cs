namespace RonSijm.AnaalIJzer.DependencyRules;

internal enum DependencyRuleKind
{
	Allowed,
	Blocked
}

internal readonly struct DependencyEdge(string scopePath, string from, string to, string configuredFrom, string configuredTo, DependencySiteFilter siteFilter, bool appliesToDescendants, DependencyRuleKind kind, string xmlPath, int xmlLineNumber, int xmlLinePosition)
{
	public string ScopePath { get; } = scopePath;

	public string From { get; } = from;

	public string To { get; } = to;

	public string ConfiguredFrom { get; } = configuredFrom;

	public string ConfiguredTo { get; } = configuredTo;

	public DependencySiteFilter SiteFilter { get; } = siteFilter;

	public bool AppliesToDescendants { get; } = appliesToDescendants;

	public DependencyRuleKind Kind { get; } = kind;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public bool IsAllowed
    {
        get { return Kind == DependencyRuleKind.Allowed; }
    }

    public bool IsBlocked
    {
        get { return Kind == DependencyRuleKind.Blocked; }
    }

    public bool IsExplicit
    {
        get { return From != "*" && To != "*"; }
    }

    public bool IsWildcardTarget
    {
        get { return From == "*" && To != "*"; }
    }

    public bool IsWildcardSource
    {
        get { return From != "*" && To == "*"; }
    }

    public bool IsAllowAny
    {
        get { return From == "*" && To == "*"; }
    }

	public bool AllowsSite(string site)
	{
		var result = SiteFilter.Allows(site);

		return result;
	}

    public string ToXmlText()
    {
        var appliesToDescendantsText = AppliesToDescendants ? " appliesToDescendants=\"true\"" : string.Empty;
        var result =
            $"<{(IsBlocked ? "BlockedDependency" : "AllowedDependency")} from=\"{ConfiguredFrom}\" to=\"{ConfiguredTo}\"{appliesToDescendantsText}/>";

        return result;
    }
}
