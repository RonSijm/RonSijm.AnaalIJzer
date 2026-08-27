using RonSijm.AnaalIJzer.Core.BuildMetadata;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.Matchers.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture.Tests;

public sealed class ProjectArchitectureAnalysisServiceTests
{
	[Fact]
	public void ProjectReferenceEvaluator_UsesAllowlistMode_WhenSourceHasAllowedRules()
	{
		var config = CreateConfig(
			[
				Group("Presentation", ".Web"),
				Group("Application", ".Application"),
				Group("Domain", ".Domain")
			],
			[
				new ProjectReferenceRule(ProjectReferenceRuleKind.Allowed, "Presentation", "Application", null, "Architecture.anl", 1, 1)
			],
			[],
			false);

		var evaluation = ProjectReferenceEvaluator.Evaluate(config, "Shop.Web", "Shop.Domain");

		evaluation.IsAllowed.Should().BeFalse();
		evaluation.SourceProjectGroup.Should().Be("Presentation");
		evaluation.TargetProjectGroup.Should().Be("Domain");
		evaluation.ViolationReason.Should().Contain("no AllowedProjectReference permits");
	}

	[Fact]
	public void ProjectReferenceEvaluator_RemainsBlocklistOnly_WhenSourceHasNoAllowedRules()
	{
		var config = CreateConfig(
			[
				Group("Infrastructure", ".Infrastructure"),
				Group("Domain", ".Domain")
			],
			[
				new ProjectReferenceRule(ProjectReferenceRuleKind.Blocked, "Domain", "Infrastructure", null, "Architecture.anl", 1, 1)
			],
			[],
			false);

		var allowedEvaluation = ProjectReferenceEvaluator.Evaluate(config, "Shop.Infrastructure", "Shop.Domain");
		var blockedEvaluation = ProjectReferenceEvaluator.Evaluate(config, "Shop.Domain", "Shop.Infrastructure");

		allowedEvaluation.IsAllowed.Should().BeTrue();
		blockedEvaluation.IsAllowed.Should().BeFalse();
		blockedEvaluation.ViolationReason.Should().Contain("BlockedProjectReference");
	}

	[Fact]
	public void ProjectReferenceEvaluator_RequiresExplicitSelfEdge_InAllowlistMode()
	{
		var config = CreateConfig(
			[
				Group("Tests", ".Tests")
			],
			[
				new ProjectReferenceRule(ProjectReferenceRuleKind.Allowed, "Tests", "*", null, "Architecture.anl", 1, 1)
			],
			[],
			false);

		var evaluation = ProjectReferenceEvaluator.Evaluate(config, "Shop.Tests", "Other.Tests");

		evaluation.IsAllowed.Should().BeFalse();
		evaluation.ViolationReason.Should().Contain("same-group reference");
	}

	[Fact]
	public void PackageReferenceEvaluator_UsesCaseInsensitiveForbiddenAndAllowlistRules()
	{
		var config = CreateConfig(
			[
				Group("Domain", ".Domain")
			],
			[],
			[
				new PackagePolicy(
					"Domain",
					[
						PackageStartsWith("microsoft.extensions.")
					],
					[
						PackageExactName("MICROSOFT.EXTENSIONS.LOGGING")
					],
					false,
					null,
					"Architecture.anl",
					1,
					1)
			],
			false);

		var evaluation = PackageReferenceEvaluator.Evaluate(config, "Shop.Domain", "Microsoft.Extensions.Logging", "9.0.0", PackageReferenceKind.Direct);

		evaluation.IsAllowed.Should().BeFalse();
		evaluation.ViolationReason.Should().Contain("Forbidden policy");
	}

	[Fact]
	public void PackageReferenceEvaluator_IgnoresTransitivePackagesUnlessEnabled()
	{
		var config = CreateConfig(
			[
				Group("Domain", ".Domain")
			],
			[],
			[
				new PackagePolicy(
					"Domain",
					[],
					[
						PackageExactName("Microsoft.Extensions.Logging.Abstractions")
					],
					false,
					null,
					"Architecture.anl",
					1,
					1)
			],
			false);

		var evaluation = PackageReferenceEvaluator.Evaluate(config, "Shop.Domain", "Microsoft.Extensions.Logging.Abstractions", "9.0.0", PackageReferenceKind.Transitive);

		evaluation.IsAllowed.Should().BeTrue();
	}

	[Fact]
	public void PackageReferenceEvaluator_ChecksTransitivePackagesWhenEnabled()
	{
		var config = CreateConfig(
			[
				Group("Domain", ".Domain")
			],
			[],
			[
				new PackagePolicy(
					"Domain",
					[],
					[
						PackageExactName("Microsoft.Extensions.Logging.Abstractions")
					],
					true,
					null,
					"Architecture.anl",
					1,
					1)
			],
			false);

		var evaluation = PackageReferenceEvaluator.Evaluate(config, "Shop.Domain", "Microsoft.Extensions.Logging.Abstractions", "9.0.0", PackageReferenceKind.Transitive);

		evaluation.IsAllowed.Should().BeFalse();
		evaluation.ViolationReason.Should().Contain("Forbidden policy");
	}

	[Fact]
	public void AnalysisService_CollectsProjectAndPackageViolations()
	{
		var config = CreateConfig(
			[
				Group("Domain", ".Domain"),
				Group("Infrastructure", ".Infrastructure")
			],
			[
				new ProjectReferenceRule(ProjectReferenceRuleKind.Blocked, "Domain", "Infrastructure", null, "Architecture.anl", 1, 1)
			],
			[
				new PackagePolicy(
					"Domain",
					[],
					[
						PackageExactName("Microsoft.Extensions.Logging")
					],
					false,
					null,
					"Architecture.anl",
					1,
					1)
			],
			true);
		var manifest = new ArchitectureReferenceManifest(
			[
				new ProjectReferenceManifestRecord(@"D:\src\Shop.Domain.csproj", @"D:\src\Shop.Infrastructure.csproj")
			],
			[
				new ArchitecturePackageReference(@"D:\src\Shop.Domain.csproj", "Microsoft.Extensions.Logging", "9.0.0", PackageReferenceKind.Direct)
			]);

		var result = ProjectArchitectureAnalysisService.Analyze(config, manifest);

		result.ProjectReferenceViolations.Should().ContainSingle();
		result.ProjectReferenceViolations[0].SourceProjectGroup.Should().Be("Domain");
		result.PackageReferenceViolations.Should().ContainSingle();
		result.PackageReferenceViolations[0].SourceProjectGroup.Should().Be("Domain");
	}

	[Fact]
	public void ViolationFindings_UseSharedDiagnosticContracts()
	{
		var projectViolation = new ProjectReferenceViolationFinding(
			@"D:\src\Shop.Web.csproj",
			"Shop.Web",
			"Presentation",
			@"D:\src\Shop.Domain.csproj",
			"Shop.Domain",
			"Domain",
			"no AllowedProjectReference permits project group 'Presentation' to reference project group 'Domain'",
			null);
		var packageViolation = new PackageReferenceViolationFinding(
			@"D:\src\Shop.Domain.csproj",
			"Shop.Domain",
			"Domain",
			"Microsoft.Extensions.Logging",
			"9.0.0",
			PackageReferenceKind.Direct,
			"the package matches a Forbidden policy for project group 'Domain'",
			null,
			null);

		var projectFinding = projectViolation.ToArchitectureFinding();
		var packageFinding = packageViolation.ToArchitectureFinding();

		projectFinding.Code.Should().Be(ArchitecturalDiagnosticIds.ProjectReferenceViolation);
		projectFinding.Properties[ArchitectureDiagnosticProperties.PropertySourceProjectGroup].Should().Be("Presentation");
		packageFinding.Code.Should().Be(ArchitecturalDiagnosticIds.PackageReferenceViolation);
		packageFinding.Properties[ArchitectureDiagnosticProperties.PropertyPackageId].Should().Be("Microsoft.Extensions.Logging");
	}

	private static ProjectArchitectureConfig CreateConfig(
		ImmutableArray<ProjectGroup> groups,
		ImmutableArray<ProjectReferenceRule> rules,
		ImmutableArray<PackagePolicy> packagePolicies,
		bool requireRecognizedProjects)
	{
		var result = new ProjectArchitectureConfig(groups, rules, packagePolicies, requireRecognizedProjects);

		return result;
	}

	private static ProjectGroup Group(string name, string projectSuffix)
	{
		var result = new ProjectGroup(name, [new ProjectMatcher([new MatchCondition(MatchKind.EndsWith, projectSuffix)])], null, "Architecture.anl", 1, 1);

		return result;
	}

	private static PackageMatcher PackageStartsWith(string prefix)
	{
		var result = new PackageMatcher([new MatchCondition(MatchKind.StartsWith, prefix)], null, null, "Architecture.anl", 1, 1);

		return result;
	}

	private static PackageMatcher PackageExactName(string packageId)
	{
		var result = new PackageMatcher([new MatchCondition(MatchKind.Equals, packageId)], null, null, "Architecture.anl", 1, 1);

		return result;
	}
}
