using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Definitions;

namespace RonSijm.AnaalIJzer.DependencyRules;

/// <summary>
///     Owns the allowed-dependency edge rules: explicit directed edges,
///     wildcard sources/targets, the all-allowed flag, and optional site filters.
///     Provides named query methods so callers do not need to inline the
///     wildcard and site-filter logic.
/// </summary>
internal readonly struct DependencyGraph(ImmutableArray<DependencyEdge> dependencyEdges)
{
	/// <summary>All valid dependency edges declared by <c>&lt;AllowedDependency /&gt;</c>.</summary>
	public ImmutableArray<DependencyEdge> DependencyEdges { get; } = dependencyEdges;

	/// <summary>
	///     Explicit directed edges declared via <c>&lt;AllowedDependency from="A" to="B"/&gt;</c>.
	///     Exposed for tests and the ARCH004 reverse-direction check.
	/// </summary>
	public ImmutableHashSet<(string From, string To)> AllowedEdges
    {
        get
        {
            return DependencyEdges.Where(edge => edge.IsAllowed && edge.IsExplicit).Select(edge => (edge.From, edge.To))
                .ToImmutableHashSet();
        }
    }

    /// <summary>
	///     Layer names reachable via <c>&lt;AllowedDependency from="*" to="..."&gt;</c>.
	///     Any layer may depend on these when the matching edge allows the current dependency site.
	/// </summary>
	public ImmutableHashSet<string> WildcardTargets
    {
        get
        {
            return DependencyEdges.Where(edge => edge.IsAllowed && edge.IsWildcardTarget).Select(edge => edge.To)
                .ToImmutableHashSet();
        }
    }

    /// <summary>
	///     Layer names declared via <c>&lt;AllowedDependency from="..." to="*"&gt;</c>.
	///     Types in these layers may depend on any other configured layer when the matching edge
	///     allows the current dependency site.
	/// </summary>
	public ImmutableHashSet<string> WildcardSources
    {
        get
        {
            return DependencyEdges.Where(edge => edge.IsAllowed && edge.IsWildcardSource).Select(edge => edge.From)
                .ToImmutableHashSet();
        }
    }

    /// <summary>
	///     When <see langword="true" /> the config declared <c>&lt;AllowedDependency from="*" to="*"/&gt;</c>.
	/// </summary>
	public bool AllowAnyDependency
    {
        get { return DependencyEdges.Any(edge => edge.IsAllowed && edge.IsAllowAny); }
    }

    /// <summary>
	///     Returns <see langword="true" /> when a dependency from <paramref name="from" /> to
	///     <paramref name="to" /> is permitted by an explicit edge, wildcard edge, or all-allowed edge
	///     that also permits <paramref name="site" />.
	/// </summary>
	public bool IsEdgeAllowed(string from, string to, string site)
	{
		var result = EvaluateEdge(from, to, site).IsAllowed;

		return result;
	}

    /// <summary>Evaluates every boundary gate between two hierarchical layer matches.</summary>
	public DependencyEdgeEvaluation EvaluateDependency(LayerMatch from, LayerMatch to, string site)
	{
		var result = EvaluateDependency(from.Layer.Name, to.Layer.Name, site);

		return result;
	}

	public DependencyEdgeEvaluation EvaluateDependency(string from, string to, string site)
	{
		foreach (var scopePath in GetGateScopes(from, to))
		{
			var evaluation = EvaluateGate(scopePath, from, to, site);
			if (!evaluation.IsAllowed)
			{
				return evaluation;
			}
		}

		return DependencyEdgeEvaluation.Allowed;
	}

	/// <summary>
	///     Evaluates whether a dependency is allowed, including why a matching site-filtered edge
	///     rejected the dependency site.
	/// </summary>
	public DependencyEdgeEvaluation EvaluateEdge(string from, string to, string site)
	{
		var result = EvaluateGate(string.Empty, from, to, site);

		return result;
	}

	private DependencyEdgeEvaluation EvaluateGate(string scopePath, string from, string to, string site)
	{
		DependencyEdge? rejectedEdge = null;
		var boundary = FormatBoundary(scopePath);

		foreach (var edge in DependencyEdges)
		{
			if (EdgeAppliesAtScope(edge, scopePath) && edge.IsBlocked && EdgeMatches(edge, from, to) && edge.AllowsSite(site))
			{
				return DependencyEdgeEvaluation.Denied($"{edge.ToXmlText()} explicitly blocks this dependency at {site} in {boundary}", DependencyDenialKind.BlockedEdge, scopePath, from, to);
			}
		}

		foreach (var edge in DependencyEdges)
		{
			if (!EdgeAppliesAtScope(edge, scopePath) || !edge.IsAllowed || !EdgeMatches(edge, from, to))
			{
				continue;
			}

			if (edge.AllowsSite(site))
			{
				return DependencyEdgeEvaluation.Allowed;
			}

			rejectedEdge ??= edge;
		}

		if (rejectedEdge is { } edgeRejectedBySite)
		{
			return DependencyEdgeEvaluation.Denied($"{edgeRejectedBySite.ToXmlText()} is configured, but {edgeRejectedBySite.SiteFilter.GetDenialReason(site)} in {boundary}", DependencyDenialKind.SiteFilter, scopePath, from, to);
		}

		return DependencyEdgeEvaluation.Denied($"no allowed dependency gate from '{from}' to '{to}' is configured in {boundary}", DependencyDenialKind.MissingEdge, scopePath, from, to);
	}

	/// <summary>
	///     Returns <see langword="true" /> when an explicit directed edge from
	///     <paramref name="from" /> to <paramref name="to" /> is configured, regardless of its site
	///     filter. Used by ARCH004 so a reversed dependency still reports as wrong-direction.
	/// </summary>
	public bool HasEdge(string from, string to)
	{
		var result = DependencyEdges.Any(edge => edge.IsAllowed && edge.IsExplicit && edge.From == from && edge.To == to);

		return result;
	}

    public bool HasEdge(string scopePath, string from, string to)
    {
        return DependencyEdges.Any(edge =>
            EdgeAppliesAtScope(edge, scopePath) && edge.IsAllowed && EdgeMatches(edge, from, to));
    }

	public bool Matches(DependencyEdge edge, string from, string to)
	{
		var result = GetGateScopes(from, to).Any(scopePath => EdgeAppliesAtScope(edge, scopePath)) && EdgeMatches(edge, from, to);

		return result;
	}

	private static bool EdgeAppliesAtScope(DependencyEdge edge, string scopePath)
	{
		var result = edge.ScopePath == scopePath
		             || edge.AppliesToDescendants && IsAncestorScope(edge.ScopePath, scopePath);

		return result;
	}

	private static bool IsAncestorScope(string ancestorScopePath, string scopePath)
	{
		if (scopePath.Length == 0)
		{
			return false;
		}

		var result = ancestorScopePath.Length == 0
			? true
			: scopePath.StartsWith(ancestorScopePath + "/", StringComparison.Ordinal);

		return result;
	}

    private static bool EdgeMatches(DependencyEdge edge, string from, string to)
	{
		if (edge.IsAllowAny)
		{
			return true;
		}

		if (edge.IsWildcardTarget)
		{
			return PathMatches(to, edge.To);
		}

		if (edge.IsWildcardSource)
		{
			return PathMatches(from, edge.From);
		}

		return PathMatches(from, edge.From) && PathMatches(to, edge.To);
	}

	private static bool PathMatches(string actualPath, string configuredPath)
	{
		var result = actualPath == configuredPath || actualPath.StartsWith(configuredPath + "/", StringComparison.Ordinal);

		return result;
	}

	private static string FormatBoundary(string scopePath)
	{
		var result = string.IsNullOrEmpty(scopePath) ? "the root boundary" : $"boundary '{scopePath}'";

		return result;
	}

    private static ImmutableArray<string> GetGateScopes(string from, string to)
	{
		var fromParts = from.Split('/');
		var toParts = to.Split('/');
		var commonLength = 0;
		while (commonLength < fromParts.Length && commonLength < toParts.Length && fromParts[commonLength] == toParts[commonLength])
		{
			commonLength++;
		}

		if (commonLength == fromParts.Length && commonLength == toParts.Length)
		{
			return [JoinPath(fromParts, fromParts.Length - 1)];
		}

		var scopes = ImmutableArray.CreateBuilder<string>();
		AddScope(scopes, JoinPath(fromParts, commonLength));
		for (var length = commonLength + 1; length < fromParts.Length; length++)
		{
			AddScope(scopes, JoinPath(fromParts, length));
		}
		for (var length = commonLength + 1; length < toParts.Length; length++)
		{
			AddScope(scopes, JoinPath(toParts, length));
		}

		return scopes.ToImmutable();
	}

	private static string JoinPath(string[] parts, int length)
	{
		var result = length <= 0 ? string.Empty : string.Join("/", parts, 0, length);

		return result;
	}

    private static void AddScope(ImmutableArray<string>.Builder scopes, string scope)
	{
		if (!scopes.Contains(scope))
		{
			scopes.Add(scope);
		}
	}
}

internal enum DependencyDenialKind
{
	None,
	MissingEdge,
	SiteFilter,
	BlockedEdge
}

internal readonly struct DependencyEdgeEvaluation(bool isAllowed, string denialReason, DependencyDenialKind denialKind, string scopePath, string fromPath, string toPath)
{
	public static DependencyEdgeEvaluation Allowed { get; } = new(true, string.Empty, DependencyDenialKind.None, string.Empty, string.Empty, string.Empty);

	public bool IsAllowed { get; } = isAllowed;

	public string DenialReason { get; } = denialReason;

	public DependencyDenialKind DenialKind { get; } = denialKind;

	public string ScopePath { get; } = scopePath;

	public string FromPath { get; } = fromPath;

	public string ToPath { get; } = toPath;

	public bool IsDeniedBySiteFilter
    {
        get { return DenialKind == DependencyDenialKind.SiteFilter; }
    }

    public bool IsDeniedByBlockedEdge
    {
        get { return DenialKind == DependencyDenialKind.BlockedEdge; }
    }

	public static DependencyEdgeEvaluation Denied(string reason, DependencyDenialKind denialKind, string scopePath, string fromPath, string toPath)
	{
		var result = new DependencyEdgeEvaluation(false, reason, denialKind, scopePath, fromPath, toPath);

		return result;
	}
}
