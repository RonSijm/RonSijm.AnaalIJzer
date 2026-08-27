using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.LayerModel;

namespace RonSijm.AnaalIJzer.Core.PolicyEvaluation.Engine.DependencyRules;

public readonly partial struct DependencyGraph
{
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
}
