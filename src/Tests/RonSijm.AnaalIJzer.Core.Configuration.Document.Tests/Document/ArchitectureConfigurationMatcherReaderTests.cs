using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.Matchers.Declarations;
using RonSijm.AnaalIJzer.Core.Matchers.Observations;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Tests.Document;

public sealed class ArchitectureConfigurationMatcherReaderTests
{
	[Theory]
	[InlineData("Type", DeclarationMatchTarget.Type)]
	[InlineData("NestedType", DeclarationMatchTarget.NestedType)]
	[InlineData("Constructor", DeclarationMatchTarget.Constructor)]
	[InlineData("Method", DeclarationMatchTarget.Method)]
	[InlineData("Property", DeclarationMatchTarget.Property)]
	[InlineData("Field", DeclarationMatchTarget.Field)]
	[InlineData("Event", DeclarationMatchTarget.Event)]
	[InlineData("Operator", DeclarationMatchTarget.Operator)]
	[InlineData("Conversion", DeclarationMatchTarget.Conversion)]
	public void TryReadDeclarationMatcher_SupportsEveryDeclarationTarget(string elementName, DeclarationMatchTarget expectedTarget)
	{
		var element = XElement.Parse($$"""
		                               <{{elementName}} exactName="PizzaId" typeName="PizzaId" />
		                               """);

		var success = ArchitectureConfigurationMatcherReader.TryReadDeclarationMatcher(element, out var matcher);

		success.Should().BeTrue();
		matcher.Target.Should().Be(expectedTarget);
		matcher.Conditions.Should().BeEquivalentTo(
		[
			new MatchCondition(MatchKind.Equals, "PizzaId", MatchOperand.AssociatedType),
			new MatchCondition(MatchKind.Equals, "PizzaId", MatchOperand.Declaration)
		],
		options => options.WithStrictOrdering());
	}

	[Fact]
	public void TryReadMatcher_ClassMatcher_ReadsAllConfiguredConditions()
	{
		var element = XElement.Parse("""
		                             <Class typeName="PizzaKitchen"
		                                    endsWith="Kitchen"
		                                    withAccessModifier="public"
		                                    typeKind="Class" />
		                             """);

		var success = ArchitectureConfigurationMatcherReader.TryReadMatcher(element, MatchTarget.TypeName, out var matcher);

		success.Should().BeTrue();
		matcher.Target.Should().Be(MatchTarget.TypeName);
		matcher.Conditions.Should().BeEquivalentTo(
		[
			new MatchCondition(MatchKind.Equals, "PizzaKitchen"),
			new MatchCondition(MatchKind.HasAccessModifier, "public"),
			new MatchCondition(MatchKind.HasTypeKind, "Class"),
			new MatchCondition(MatchKind.EndsWith, "Kitchen")
		],
		options => options.WithStrictOrdering());
	}

	[Fact]
	public void TryReadMatcher_ClassMatcher_ReadsRequiredDeclarationMatchers()
	{
		var element = XElement.Parse("""
		                             <Class endsWith="Request">
		                               <Property exactName="PizzaId" typeName="PizzaId" />
		                               <Field exactName="_tenantId" typeName="TenantId" />
		                             </Class>
		                             """);

		var success = ArchitectureConfigurationMatcherReader.TryReadMatcher(element, MatchTarget.TypeName, out var matcher);

		success.Should().BeTrue();
		matcher.RequiredDeclarations.Should().HaveCount(2);
		matcher.RequiredDeclarations[0].Target.Should().Be(DeclarationMatchTarget.Property);
		matcher.RequiredDeclarations[0].Conditions.Should().BeEquivalentTo(
		[
			new MatchCondition(MatchKind.Equals, "PizzaId", MatchOperand.AssociatedType),
			new MatchCondition(MatchKind.Equals, "PizzaId", MatchOperand.Declaration)
		],
		options => options.WithStrictOrdering());
		matcher.RequiredDeclarations[1].Target.Should().Be(DeclarationMatchTarget.Field);
		matcher.RequiredDeclarations[1].Conditions.Should().BeEquivalentTo(
		[
			new MatchCondition(MatchKind.Equals, "TenantId", MatchOperand.AssociatedType),
			new MatchCondition(MatchKind.Equals, "_tenantId", MatchOperand.Declaration)
		],
		options => options.WithStrictOrdering());
	}

	[Fact]
	public void TryReadDeclarationMatcher_MethodMatcher_ReadsRequiredObservationMatchers()
	{
		var element = XElement.Parse("""
		                             <Method exactName="PizzaDelivery">
		                               <Throw />
		                               <Invocation exactName="LogFailure" />
		                             </Method>
		                             """);

		var success = ArchitectureConfigurationMatcherReader.TryReadDeclarationMatcher(element, out var matcher);

		success.Should().BeTrue();
		matcher.RequiredObservations.Should().HaveCount(2);
		matcher.RequiredObservations[0].Target.Should().Be(CodeObservationMatchTarget.Throw);
		matcher.RequiredObservations[0].Conditions.Should().BeEmpty();
		matcher.RequiredObservations[1].Target.Should().Be(CodeObservationMatchTarget.Invocation);
		matcher.RequiredObservations[1].Conditions.Should().BeEquivalentTo(
		[
			new MatchCondition(MatchKind.Equals, "LogFailure", MatchOperand.Declaration)
		],
		options => options.WithStrictOrdering());
	}

	[Theory]
	[InlineData("Throw", CodeObservationMatchTarget.Throw)]
	[InlineData("Invocation", CodeObservationMatchTarget.Invocation)]
	[InlineData("New", CodeObservationMatchTarget.New)]
	[InlineData("Identifier", CodeObservationMatchTarget.Identifier)]
	[InlineData("MemberAccess", CodeObservationMatchTarget.MemberAccess)]
	[InlineData("Literal", CodeObservationMatchTarget.Literal)]
	public void TryReadCodeObservationMatcher_SupportsEveryObservationTarget(string elementName, CodeObservationMatchTarget expectedTarget)
	{
		var element = XElement.Parse($$"""
		                               <{{elementName}} exactName="PizzaDelivery" />
		                               """);

		var success = ArchitectureConfigurationMatcherReader.TryReadCodeObservationMatcher(element, out var matcher);

		success.Should().BeTrue();
		matcher.Target.Should().Be(expectedTarget);
		matcher.Conditions.Should().BeEquivalentTo(
		[
			new MatchCondition(MatchKind.Equals, "PizzaDelivery", MatchOperand.Declaration)
		],
		options => options.WithStrictOrdering());
	}

	[Fact]
	public void TryReadCodeObservationMatcher_WithSemanticConditionsUsesTheDeclarationSymbol()
	{
		var element = XElement.Parse("""<Invocation withAttribute="CanBeNull" />""");

		var success = ArchitectureConfigurationMatcherReader.TryReadCodeObservationMatcher(element, allowSemanticConditions: true, out var matcher);

		success.Should().BeTrue();
		matcher.Conditions.Should().BeEquivalentTo(
		[
			new MatchCondition(MatchKind.HasAttribute, "CanBeNull", MatchOperand.Declaration)
		],
		options => options.WithStrictOrdering());
	}

	[Fact]
	public void TryReadCodeObservationMatcher_LiteralValueUsesTheDeclarationSymbol()
	{
		var element = XElement.Parse("""<Literal value="null" />""");

		var success = ArchitectureConfigurationMatcherReader.TryReadCodeObservationMatcher(element, out var matcher);

		success.Should().BeTrue();
		matcher.Conditions.Should().BeEquivalentTo(
		[
			new MatchCondition(MatchKind.Equals, "null", MatchOperand.Declaration)
		],
		options => options.WithStrictOrdering());
	}

	[Fact]
	public void TryReadMatcher_NamespaceMatcher_ReadsExactAndPatternConditions()
	{
		var element = XElement.Parse("""<Namespace exactName="Shop.Ordering" startsWith="Shop." />""");

		var success = ArchitectureConfigurationMatcherReader.TryReadMatcher(element, MatchTarget.Namespace, out var matcher);

		success.Should().BeTrue();
		matcher.Target.Should().Be(MatchTarget.Namespace);
		matcher.Conditions.Should().BeEquivalentTo(
		[
			new MatchCondition(MatchKind.Equals, "Shop.Ordering"),
			new MatchCondition(MatchKind.StartsWith, "Shop.")
		],
		options => options.WithStrictOrdering());
	}

	[Fact]
	public void GetMatcherDisplayName_ReturnsAllMatcherAttributes()
	{
		var element = XElement.Parse("""<Class typeName="PizzaKitchen" endsWith="Kitchen" typeKind="Class" description="ignored" />""");

		var result = ArchitectureConfigurationMatcherReader.GetMatcherDisplayName(element);

		result.Should().Be("typeName=\"PizzaKitchen\" endsWith=\"Kitchen\" typeKind=\"Class\"");
	}

	[Fact]
	public void GetPrimaryMatcherValue_PrefersSpecificMatcherAttributes()
	{
		var element = XElement.Parse("""<Class typeKind="Class" endsWith="Kitchen" typeName="PizzaKitchen" />""");

		var result = ArchitectureConfigurationMatcherReader.GetPrimaryMatcherValue(element);

		result.Should().Be("PizzaKitchen");
	}

	[Fact]
	public void TryReadMatcher_WithoutMatcherAttributes_ReturnsFalse()
	{
		var element = XElement.Parse("""<Class description="No matcher" />""");

		var success = ArchitectureConfigurationMatcherReader.TryReadMatcher(element, MatchTarget.TypeName, out var matcher);

		success.Should().BeFalse();
		matcher.Conditions.Should().BeEmpty();
	}
}
