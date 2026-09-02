using RonSijm.AnaalIJzer.Core.Matchers.Conditions;

namespace RonSijm.AnaalIJzer.Core.Matchers.Tests.Matching;

public sealed class MatcherAttributeCatalogTests
{
	[Theory]
	[InlineData(MatcherAttributeProfile.Type, MatchOperand.Subject)]
	[InlineData(MatcherAttributeProfile.Declaration, MatchOperand.Declaration)]
	[InlineData(MatcherAttributeProfile.SemanticCodeObservation, MatchOperand.Declaration)]
	public void CreateConditions_WithAttributeUsesTheProfileOperand(MatcherAttributeProfile profile, MatchOperand expectedOperand)
	{
		var result = MatcherAttributeCatalog.CreateConditions(
			attributeName => attributeName == "withAttribute" ? "CanBeNull" : null,
			profile);

		result.Should().BeEquivalentTo(
		[
			new MatchCondition(MatchKind.HasAttribute, "CanBeNull", expectedOperand)
		],
		options => options.WithStrictOrdering());
	}

	[Theory]
	[InlineData(MatcherAttributeProfile.NamespaceOrAssembly)]
	[InlineData(MatcherAttributeProfile.CodeObservation)]
	[InlineData(MatcherAttributeProfile.ProjectOrPackage)]
	public void CreateConditions_WithAttributeOutsideSupportedProfilesCreatesNoCondition(MatcherAttributeProfile profile)
	{
		var result = MatcherAttributeCatalog.CreateConditions(
			attributeName => attributeName == "withAttribute" ? "CanBeNull" : null,
			profile);

		result.Should().BeEmpty();
		MatcherAttributeCatalog.IsSupportedAttribute("withAttribute", profile).Should().BeFalse();
	}

	[Fact]
	public void CreateConditions_LiteralValueIsOptInAndTargetsTheDeclaration()
	{
		var withoutLiteralValue = MatcherAttributeCatalog.CreateConditions(
			attributeName => attributeName == "value" ? "null" : null,
			MatcherAttributeProfile.SemanticCodeObservation);
		var withLiteralValue = MatcherAttributeCatalog.CreateConditions(
			attributeName => attributeName == "value" ? "null" : null,
			MatcherAttributeProfile.SemanticCodeObservation,
			includeLiteralValue: true);

		withoutLiteralValue.Should().BeEmpty();
		withLiteralValue.Should().BeEquivalentTo(
		[
			new MatchCondition(MatchKind.Equals, "null", MatchOperand.Declaration)
		],
		options => options.WithStrictOrdering());
		MatcherAttributeCatalog.IsSupportedAttribute("value", MatcherAttributeProfile.SemanticCodeObservation).Should().BeFalse();
		MatcherAttributeCatalog.IsSupportedAttribute("value", MatcherAttributeProfile.SemanticCodeObservation, includeLiteralValue: true).Should().BeTrue();
	}

	[Fact]
	public void GetAttributeNames_UsesTheSameProfileDefinitionsAsConditionCreation()
	{
		var typeAttributes = MatcherAttributeCatalog.GetAttributeNames(MatcherAttributeProfile.Type);
		var declarationAttributes = MatcherAttributeCatalog.GetAttributeNames(MatcherAttributeProfile.Declaration);
		var observationAttributes = MatcherAttributeCatalog.GetAttributeNames(MatcherAttributeProfile.CodeObservation);

		typeAttributes.Should().Contain("withAttribute");
		declarationAttributes.Should().Contain("withAttribute");
		observationAttributes.Should().NotContain("withAttribute");
		typeAttributes.Should().NotContain("value");
	}
}
