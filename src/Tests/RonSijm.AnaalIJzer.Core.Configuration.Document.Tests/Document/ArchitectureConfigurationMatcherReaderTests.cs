using System.Xml.Linq;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Tests.Document;

public sealed class ArchitectureConfigurationMatcherReaderTests
{
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
