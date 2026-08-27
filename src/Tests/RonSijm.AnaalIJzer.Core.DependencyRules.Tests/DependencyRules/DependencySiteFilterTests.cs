using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.DependencyRules.Tests.DependencyRules;

public sealed class DependencySiteFilterTests
{
	[Fact]
	public void All_AllowsEverySiteAndRendersNoText()
	{
		var filter = DependencySiteFilter.All;

		filter.HasFilter.Should().BeFalse();
		filter.Allows("Constructor").Should().BeTrue();
		filter.GetDenialReason("Constructor").Should().BeEmpty();
		filter.ToDisplayText().Should().BeEmpty();
	}

	[Fact]
	public void AllowedSites_FilterOnlyAllowsConfiguredSites()
	{
		var filter = new DependencySiteFilter(
			ImmutableHashSet.Create(StringComparer.Ordinal, "Local", "MethodReturn"),
			ImmutableHashSet<string>.Empty);

		filter.HasFilter.Should().BeTrue();
		filter.Allows("Local").Should().BeTrue();
		filter.Allows("Constructor").Should().BeFalse();
		filter.GetDenialReason("Constructor").Should().Be("allowedSites does not include Constructor");
		filter.ToDisplayText().Should().Be("allowed sites: MethodReturn, Local");
	}

	[Fact]
	public void BlockedSites_FilterBlocksConfiguredSites()
	{
		var filter = new DependencySiteFilter(
			ImmutableHashSet<string>.Empty,
			ImmutableHashSet.Create(StringComparer.Ordinal, "Field", "Property"));

		filter.HasFilter.Should().BeTrue();
		filter.Allows("Method").Should().BeTrue();
		filter.Allows("Field").Should().BeFalse();
		filter.GetDenialReason("Field").Should().Be("blockedSites blocks Field");
		filter.ToDisplayText().Should().Be("blocked sites: Field, Property");
	}
}
