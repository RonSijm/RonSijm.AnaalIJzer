using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.Contracts;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.Engine.ApiSurface;
using RonSijm.AnaalIJzer.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Engine.EntryPoints;
using RonSijm.AnaalIJzer.Engine.LayerModel;
using RonSijm.AnaalIJzer.Engine.NameRules;
using RonSijm.AnaalIJzer.Engine.Policies;
using RonSijm.AnaalIJzer.Engine.Visibility;
using RonSijm.AnaalIJzer.Inheritance;
using RonSijm.AnaalIJzer.SourceLocations;
using RonSijm.AnaalIJzer.SymbolFacts;

namespace RonSijm.AnaalIJzer.Core.LayerModel.Tests.LayerModel;

public sealed class LayerModelCoreTests
{
	[Fact]
	public void LayerDefinition_NormalAndForbiddenPreserveMetadata()
	{
		var normal = LayerDefinition.Normal("Kitchen", "Makes pizza.");
		var forbidden = LayerDefinition.Forbidden("Store", "Use Repository instead.", "Repository");

		normal.Name.Should().Be("Kitchen");
		normal.IsForbidden.Should().BeFalse();
		normal.Comment.Should().Be("Makes pizza.");
		normal.FixSuffix.Should().BeNull();

		forbidden.Name.Should().Be("Store");
		forbidden.IsForbidden.Should().BeTrue();
		forbidden.Comment.Should().Be("Use Repository instead.");
		forbidden.FixSuffix.Should().Be("Repository");
	}

	[Fact]
	public void LayerMatch_PreservesPathAndMatcherMetadata()
	{
		var outer = LayerDefinition.Normal("Restaurant", null);
		var inner = LayerDefinition.Normal("Restaurant/Kitchen", "Cooks food.");
		var match = new LayerMatch(
			inner,
			[outer, inner],
			[new LayerMatcherMatch(inner, 14, 6, "Architecture.anl")],
			"Kitchen",
			14,
			6,
			"Architecture.anl");

		match.Layer.Name.Should().Be("Restaurant/Kitchen");
		match.Layers.Select(item => item.Name).Should().Equal("Restaurant", "Restaurant/Kitchen");
		match.MatcherMatches.Should().ContainSingle();
		match.MatcherMatches[0].XmlLineNumber.Should().Be(14);
		match.MatchedSuffix.Should().Be("Kitchen");
		match.XmlPath.Should().Be("Architecture.anl");
	}

	[Fact]
	public void TypePolicyViolation_PreservesReasonRuleAndMatchedSuffix()
	{
		var layer = LayerDefinition.Forbidden("Store", "Use Repository instead.", "Repository");
		var rule = new MatcherRule(layer, ImmutableArray<ExceptionMatcher>.Empty, 9, 4, "Architecture.anl");
		var violation = new TypePolicyViolation("forbidden type", layer.Name, layer.Comment, rule, "Store");

		violation.Reason.Should().Be("forbidden type");
		violation.DependencyLayerName.Should().Be("Store");
		violation.Comment.Should().Be("Use Repository instead.");
		violation.Rule.Should().Be(rule);
		violation.MatchedSuffix.Should().Be("Store");
	}

	[Fact]
	public void CompiledLayerCatalog_DetectsNestedPolicyFamilies()
	{
		var node = new LayerNode(
			LayerDefinition.Normal("Kitchen", null),
			ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)>.Empty,
			ImmutableArray.Create(
				new LayerNode(
					LayerDefinition.Normal("Kitchen/Sauce", null),
					ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)>.Empty,
					ImmutableArray<LayerNode>.Empty,
					ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)>.Empty,
					ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)>.Empty,
					ImmutableArray<NameMatchingRule>.Empty,
					ImmutableArray.Create(CreateContractPolicy()),
					ImmutableArray.Create(CreateInheritancePolicy()),
					ImmutableArray.Create(CreateVisibilityPolicy()),
					ImmutableArray.Create(CreateApiSurfacePolicy()),
					ImmutableArray.Create(CreateEntryPointPolicy()),
					ImmutableArray.Create(CreateSourceLocationPolicy()))),
			ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)>.Empty,
			ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)>.Empty,
			ImmutableArray<NameMatchingRule>.Empty,
			ImmutableArray<ContractPolicy>.Empty,
			ImmutableArray<InheritancePolicy>.Empty,
			ImmutableArray<VisibilityPolicy>.Empty,
			ImmutableArray<ApiSurfacePolicy>.Empty,
			ImmutableArray<BoundaryEntryPointPolicy>.Empty,
			ImmutableArray<SourceLocationPolicy>.Empty);
		var catalog = new CompiledLayerCatalog(
			[node],
			ImmutableDictionary<string, LayerNode>.Empty.Add("Kitchen", node),
			ImmutableDictionary<string, MatcherRule>.Empty,
			ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)>.Empty,
			ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)>.Empty);

		catalog.HasLayers.Should().BeTrue();
		catalog.HasContractPolicies.Should().BeTrue();
		catalog.HasInheritancePolicies.Should().BeTrue();
		catalog.HasVisibilityPolicies.Should().BeTrue();
		catalog.HasApiSurfacePolicies.Should().BeTrue();
		catalog.HasEntryPointPolicies.Should().BeTrue();
		catalog.HasSourceLocationPolicies.Should().BeTrue();
	}

	private static ContractPolicy CreateContractPolicy()
	{
		var result = new ContractPolicy(
			"Kitchen",
			ImmutableHashSet.Create("Interface"),
			ImmutableHashSet<ContractMemberKind>.Empty,
			ImmutableHashSet<ContractPropertyAccessor>.Empty,
			false,
			false,
			false,
			false,
			null,
			"Architecture.anl",
			10,
			3);

		return result;
	}

	private static VisibilityPolicy CreateVisibilityPolicy()
	{
		var result = new VisibilityPolicy(
			"Kitchen",
			ImmutableHashSet.Create(VisibilityPolicyTarget.Method),
			true,
			ImmutableHashSet.Create(ArchitectureAccessibility.Public),
			null,
			"Architecture.anl",
			11,
			3);

		return result;
	}

	private static InheritancePolicy CreateInheritancePolicy()
	{
		var result = new InheritancePolicy(
			"Kitchen",
			ImmutableHashSet.Create("Class"),
			ImmutableHashSet.Create("Entity"),
			ImmutableHashSet<string>.Empty,
			null,
			"Architecture.anl",
			10,
			4);

		return result;
	}

	private static ApiSurfacePolicy CreateApiSurfacePolicy()
	{
		var result = new ApiSurfacePolicy(
			"Kitchen",
			false,
			ImmutableArray.Create(new ApiSurfaceLayerRule("DiningRoom", "/DiningRoom", new DependencySiteFilter(ImmutableHashSet<string>.Empty, ImmutableHashSet<string>.Empty), null, "Architecture.anl", 12, 3)),
			ImmutableArray<ApiSurfaceLayerRule>.Empty,
			null,
			null,
			"Architecture.anl",
			12,
			3);

		return result;
	}

	private static BoundaryEntryPointPolicy CreateEntryPointPolicy()
	{
		var result = new BoundaryEntryPointPolicy(
			"Kitchen",
			ImmutableArray<BoundaryEntryPointRule>.Empty,
			null,
			"Architecture.anl",
			13,
			3);

		return result;
	}

	private static SourceLocationPolicy CreateSourceLocationPolicy()
	{
		var rule = new SourceLocationRule(
			[new MatchCondition(MatchKind.StartsWith, "Features/Kitchen/")],
			null,
			null,
			"Architecture.anl",
			14,
			3);
		var result = new SourceLocationPolicy(
			"Kitchen",
			SourceLocationBase.Project,
			[rule],
			null,
			"Architecture.anl",
			14,
			3);

		return result;
	}
}
