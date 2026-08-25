using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.SourceLocations;

namespace RonSijm.AnaalIJzer.Core.SourceLocations.Tests.SourceLocations;

public sealed class SourceLocationCoreTests
{
	[Fact]
	public void SourceLocationRule_MatchesConfiguredPathAndAssembly()
	{
		var rule = CreateRule("Features/Ordering/", assemblyName: "Shop.Application");

		var result = rule.Matches("Features/Ordering/CandyService.cs", "Shop.Application");

		result.Should().BeTrue();
	}

	[Fact]
	public void SourceLocationRule_RejectsDifferentAssemblyWhenAssemblyNameIsConfigured()
	{
		var rule = CreateRule("Features/Ordering/", assemblyName: "Shop.Application");

		var result = rule.Matches("Features/Ordering/CandyService.cs", "Shop.Infrastructure");

		result.Should().BeFalse();
	}

	[Fact]
	public void SourceLocationPolicy_MatchesWhenAnyRuleMatches()
	{
		var policy = new SourceLocationPolicy(
			"Ordering",
			SourceLocationBase.Project,
			[
				CreateRule("Features/Ordering/"),
				CreateRule("Contracts/Ordering/")
			],
			null,
			"Architecture.anl",
			7,
			3);

		var result = policy.Matches("Contracts/Ordering/CandyService.cs", "Shop.Application");

		result.Should().BeTrue();
	}

	[Fact]
	public void SourcePathNormalizer_NormalizesAbsoluteAndRelativePaths()
	{
		var absolute = SourcePathNormalizer.NormalizeAbsolute(@"D:\repo\Shop\Ordering\CandyService.cs");
		var relative = SourcePathNormalizer.NormalizeRelativeToBase(
			@"D:\repo\Shop\Ordering\CandyService.cs",
			@"D:\repo\Shop");

		absolute.Should().Be("D:/repo/Shop/Ordering/CandyService.cs");
		relative.Should().Be("Ordering/CandyService.cs");
	}

	private static SourceLocationRule CreateRule(string startsWith, string? assemblyName = null)
	{
		var result = new SourceLocationRule(
			[new MatchCondition(MatchKind.StartsWith, startsWith)],
			assemblyName,
			null,
			"Architecture.anl",
			8,
			5);

		return result;
	}
}
