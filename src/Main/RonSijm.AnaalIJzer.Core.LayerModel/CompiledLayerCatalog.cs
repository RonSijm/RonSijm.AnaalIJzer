using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Matchers;

namespace RonSijm.AnaalIJzer.Core.LayerModel;

public readonly struct CompiledLayerCatalog(
	ImmutableArray<LayerNode> roots,
	ImmutableDictionary<string, LayerNode> nodesByPath,
	ImmutableDictionary<string, MatcherRule> forbiddenTypeNames,
	ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> forbiddenMatchers,
	ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> allowedTypeMatchers)
{
	public static readonly CompiledLayerCatalog Empty = new(
		ImmutableArray<LayerNode>.Empty,
		ImmutableDictionary<string, LayerNode>.Empty,
		ImmutableDictionary<string, MatcherRule>.Empty,
		ImmutableArray<(PatternMatcher, MatcherRule)>.Empty,
		ImmutableArray<(PatternMatcher, MatcherRule)>.Empty);

	public ImmutableArray<LayerNode> Roots { get; } = roots;
	public ImmutableDictionary<string, LayerNode> NodesByPath { get; } = nodesByPath;
	public ImmutableDictionary<string, MatcherRule> ForbiddenTypeNames { get; } = forbiddenTypeNames;
	public ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> ForbiddenMatchers { get; } = forbiddenMatchers;
	public ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> AllowedTypeMatchers { get; } = allowedTypeMatchers;

	public bool HasLayers { get; } = !roots.IsDefaultOrEmpty;
	public bool HasContractPolicies { get; } = Contains(node => !node.ContractPolicies.IsDefaultOrEmpty, roots);
	public bool HasInheritancePolicies { get; } = Contains(node => !node.InheritancePolicies.IsDefaultOrEmpty, roots);
	public bool HasReturnValuePolicies { get; } = Contains(node => !node.ReturnValuePolicies.IsDefaultOrEmpty, roots);
	public bool HasVisibilityPolicies { get; } = Contains(node => !node.VisibilityPolicies.IsDefaultOrEmpty, roots);
	public bool HasApiSurfacePolicies { get; } = Contains(node => !node.ApiSurfacePolicies.IsDefaultOrEmpty, roots);
	public bool HasEntryPointPolicies { get; } = Contains(node => !node.EntryPointPolicies.IsDefaultOrEmpty, roots);
	public bool HasSourceLocationPolicies { get; } = Contains(node => !node.SourceLocationPolicies.IsDefaultOrEmpty, roots);

	private static bool Contains(Func<LayerNode, bool> predicate, ImmutableArray<LayerNode> nodes)
	{
		foreach (var node in nodes)
		{
			if (predicate(node) || Contains(predicate, node.Children))
			{
				return true;
			}
		}

		return false;
	}
}
