using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Definitions;

namespace RonSijm.AnaalIJzer.Engine.DependencyRules;

/// <summary>
///     Owns the allowed-dependency edge rules: explicit directed edges,
///     wildcard sources/targets, the all-allowed flag, and optional site filters.
///     Provides named query methods so callers do not need to inline the
///     wildcard and site-filter logic.
/// </summary>
public readonly partial struct DependencyGraph(ImmutableArray<DependencyEdge> dependencyEdges)
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
}

