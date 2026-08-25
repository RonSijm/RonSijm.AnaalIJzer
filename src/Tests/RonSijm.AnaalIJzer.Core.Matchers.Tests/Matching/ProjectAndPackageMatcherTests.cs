using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Core.Matchers.Tests.Matching;

public sealed class ProjectAndPackageMatcherTests
{
	[Fact]
	public void ProjectMatcher_UsesCaseSensitiveStringRules()
	{
		var matcher = new ProjectMatcher([
			new MatchCondition(MatchKind.StartsWith, "Core."),
			new MatchCondition(MatchKind.EndsWith, ".Tests")
		]);

		matcher.Matches("Core.Graph.Tests").Should().BeTrue();
		matcher.Matches("core.Graph.Tests").Should().BeFalse();
	}

	[Fact]
	public void PackageMatcher_UsesCaseInsensitiveStringRules()
	{
		var matcher = new PackageMatcher(
			[
				new MatchCondition(MatchKind.StartsWith, "microsoft."),
				new MatchCondition(MatchKind.Contains, "codeanalysis")
			],
			comment: null,
			description: null,
			xmlPath: "Architecture.anl",
			xmlLineNumber: 1,
			xmlLinePosition: 1);

		matcher.Matches("Microsoft.CodeAnalysis.CSharp").Should().BeTrue();
		matcher.Matches("MICROSOFT.CODEANALYSIS.WORKSPACES").Should().BeTrue();
	}

	[Fact]
	public void ProjectMatcher_RequiresAllConditions()
	{
		var matcher = new ProjectMatcher([
			new MatchCondition(MatchKind.StartsWith, "Core."),
			new MatchCondition(MatchKind.Contains, ".Graph."),
			new MatchCondition(MatchKind.EndsWith, ".Tests")
		]);

		matcher.Matches("Core.Graphing.Tests").Should().BeFalse();
		matcher.Matches("Core.Graph.Editor.Tests").Should().BeTrue();
	}

	[Fact]
	public void EmptyProjectMatcher_DoesNotMatch()
	{
		var matcher = new ProjectMatcher(ImmutableArray<MatchCondition>.Empty);

		var result = matcher.Matches("Anything");

		result.Should().BeFalse();
	}
}
