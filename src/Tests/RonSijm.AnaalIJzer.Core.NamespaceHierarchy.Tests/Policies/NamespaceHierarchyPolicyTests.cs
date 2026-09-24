using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.NamespaceHierarchy.Policies;

namespace RonSijm.AnaalIJzer.Core.NamespaceHierarchy.Tests.Policies;

public sealed class NamespaceHierarchyPolicyTests
{
    [Theory]
    [InlineData("Shop.Requests", "Shop", NamespaceHierarchyRelation.DescendantToAncestor)]
    [InlineData("Shop", "Shop.Requests", NamespaceHierarchyRelation.AncestorToDescendant)]
    [InlineData("Shop.Requests", "Shop.Queries", NamespaceHierarchyRelation.SiblingToSibling)]
    [InlineData("Shop.Requests", "Shop.Requests", NamespaceHierarchyRelation.SameNamespace)]
    public void Evaluate_BlocksTheConfiguredRelationship(string callerNamespace, string dependencyNamespace, NamespaceHierarchyRelation relation)
    {
        var policy = CreatePolicy(relation);

        var evaluation = policy.Evaluate(callerNamespace, dependencyNamespace, "Constructor");

        evaluation.Should().NotBeNull();
        evaluation.Value.Relation.Should().Be(relation);
        evaluation.Value.Reason.Should().Contain(relation.ToString());
    }

    [Fact]
    public void Evaluate_DoesNotTreatSimilarNamespacePrefixesAsAncestors()
    {
        var policy = CreatePolicy(NamespaceHierarchyRelation.DescendantToAncestor);

        var evaluation = policy.Evaluate("Shop.RequestsArchive", "Shop.Requests", "Constructor");

        evaluation.Should().BeNull();
    }

    [Fact]
    public void Evaluate_IgnoresNamespacesOutsideTheConfiguredRoot()
    {
        var policy = CreatePolicy(NamespaceHierarchyRelation.DescendantToAncestor);

        var evaluation = policy.Evaluate("Other.Requests", "Other", "Constructor");

        evaluation.Should().BeNull();
    }

    [Fact]
    public void Evaluate_UsesSiteFiltersToScopeTheBlockedRelation()
    {
        var filter = new DependencySiteFilter(
            ImmutableHashSet.Create(StringComparer.Ordinal, "Field"),
            ImmutableHashSet<string>.Empty);
        var policy = CreatePolicy(NamespaceHierarchyRelation.DescendantToAncestor, filter);

        var constructorEvaluation = policy.Evaluate("Shop.Requests", "Shop", "Constructor");
        var fieldEvaluation = policy.Evaluate("Shop.Requests", "Shop", "Field");

        constructorEvaluation.Should().BeNull();
        fieldEvaluation.Should().NotBeNull();
    }

    [Fact]
    public void Evaluate_UsesBlockedSitesToExcludeTheNamedSite()
    {
        var filter = new DependencySiteFilter(
            ImmutableHashSet<string>.Empty,
            ImmutableHashSet.Create(StringComparer.Ordinal, "Field"));
        var policy = CreatePolicy(NamespaceHierarchyRelation.DescendantToAncestor, filter);

        var constructorEvaluation = policy.Evaluate("Shop.Requests", "Shop", "Constructor");
        var fieldEvaluation = policy.Evaluate("Shop.Requests", "Shop", "Field");

        constructorEvaluation.Should().NotBeNull();
        fieldEvaluation.Should().BeNull();
    }

    [Fact]
    public void Evaluate_UsesTheFirstMatchingRuleInDocumentOrder()
    {
        NamespaceHierarchyPath.TryParse("Shop", out var rootPath).Should().BeTrue();
        var firstRule = new NamespaceHierarchyRule(
            NamespaceHierarchyRelation.DescendantToAncestor,
            DependencySiteFilter.All,
            "The first matching block wins.",
            "Architecture.anl",
            4,
            5);
        var secondRule = new NamespaceHierarchyRule(
            NamespaceHierarchyRelation.DescendantToAncestor,
            DependencySiteFilter.All,
            "The later block must not replace the first one.",
            "Architecture.anl",
            5,
            5);
        var policy = new NamespaceHierarchyPolicy(rootPath, [firstRule, secondRule], null, "Architecture.anl", 2, 3);

        var evaluation = policy.Evaluate("Shop.Requests", "Shop", "Constructor");

        evaluation.Should().NotBeNull();
        evaluation.Value.Rule.Description.Should().Be("The first matching block wins.");
    }

    private static NamespaceHierarchyPolicy CreatePolicy(NamespaceHierarchyRelation relation, DependencySiteFilter? filter = null)
    {
        NamespaceHierarchyPath.TryParse("Shop", out var rootPath).Should().BeTrue();
        var rule = new NamespaceHierarchyRule(
            relation,
            filter ?? DependencySiteFilter.All,
            null,
            "Architecture.anl",
            4,
            5);
        var result = new NamespaceHierarchyPolicy(rootPath, [rule], null, "Architecture.anl", 2, 3);

        return result;
    }
}