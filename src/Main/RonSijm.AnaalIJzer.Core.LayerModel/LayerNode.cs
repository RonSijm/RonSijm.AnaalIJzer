using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.ApiSurface.Engine.Policies;
using RonSijm.AnaalIJzer.Core.Contracts.Contracts;
using RonSijm.AnaalIJzer.Core.EntryPoints;
using RonSijm.AnaalIJzer.Core.Inheritance.Policies;
using RonSijm.AnaalIJzer.Core.Matchers;
using RonSijm.AnaalIJzer.Core.NameRules;
using RonSijm.AnaalIJzer.Core.ReturnValues.Policies;
using RonSijm.AnaalIJzer.Core.SourceLocations;
using RonSijm.AnaalIJzer.Core.Visibility;

namespace RonSijm.AnaalIJzer.Core.LayerModel;

/// <summary>A configured layer boundary together with its matchers and nested layers.</summary>
public sealed class LayerNode(
	LayerDefinition definition,
	ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> matchers,
	ImmutableArray<LayerNode> children,
	ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> allowedTypeMatchers,
	ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> forbiddenTypeMatchers,
	ImmutableArray<NameMatchingRule> nameRules,
	ImmutableArray<ContractPolicy> contractPolicies,
	ImmutableArray<InheritancePolicy> inheritancePolicies,
	ImmutableArray<VisibilityPolicy> visibilityPolicies,
	ImmutableArray<ApiSurfacePolicy> apiSurfacePolicies,
	ImmutableArray<BoundaryEntryPointPolicy> entryPointPolicies,
	ImmutableArray<SourceLocationPolicy> sourceLocationPolicies,
	ImmutableArray<ReturnValuePolicy> returnValuePolicies = default)
{
	public LayerDefinition Definition { get; } = definition;

	public ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> Matchers { get; } = matchers;

	public ImmutableArray<LayerNode> Children { get; } = children;

	public ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> AllowedTypeMatchers { get; } = allowedTypeMatchers;

	public ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> ForbiddenTypeMatchers { get; } = forbiddenTypeMatchers;

	public ImmutableArray<NameMatchingRule> NameRules { get; } = nameRules;

	public ImmutableArray<ContractPolicy> ContractPolicies { get; } = contractPolicies;

	public ImmutableArray<InheritancePolicy> InheritancePolicies { get; } = inheritancePolicies;

	public ImmutableArray<VisibilityPolicy> VisibilityPolicies { get; } = visibilityPolicies;

	public ImmutableArray<ApiSurfacePolicy> ApiSurfacePolicies { get; } = apiSurfacePolicies;

	public ImmutableArray<BoundaryEntryPointPolicy> EntryPointPolicies { get; } = entryPointPolicies;

	public ImmutableArray<SourceLocationPolicy> SourceLocationPolicies { get; } = sourceLocationPolicies;

	public ImmutableArray<ReturnValuePolicy> ReturnValuePolicies { get; } = returnValuePolicies.IsDefault ? ImmutableArray<ReturnValuePolicy>.Empty : returnValuePolicies;

	public bool HasMatchers { get; } = !matchers.IsDefaultOrEmpty;
}
