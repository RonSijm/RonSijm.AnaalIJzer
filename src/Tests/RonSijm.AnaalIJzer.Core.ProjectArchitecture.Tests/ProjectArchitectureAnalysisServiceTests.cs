using RonSijm.AnaalIJzer.Core.BuildMetadata;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.Matchers.ProjectArchitecture;
using RonSijm.AnaalIJzer.Core.ProjectArchitecture.SolutionTopology;

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
	public void ProjectReferenceEvaluator_UsesSelectorsToNarrowAnAllowedProjectReference()
	{
		var config = CreateConfig(
			[
				Group("Application", ".Application"),
				Group("Contracts", ".Contracts")
			],
			[
				new ProjectReferenceRule(
					ProjectReferenceRuleKind.Allowed,
					"Application",
					"Contracts",
					null,
					"Architecture.anl",
					1,
					1,
					[ProjectExactName("Shop.Orders.Application")],
					[ProjectExactName("Shop.Orders.Contracts")])
			],
			[],
			false);

		var selectedEdge = ProjectReferenceEvaluator.Evaluate(config, "Shop.Orders.Application", "Shop.Orders.Contracts");
		var nonMatchingTarget = ProjectReferenceEvaluator.Evaluate(config, "Shop.Orders.Application", "Shop.Payments.Contracts");
		var nonMatchingSource = ProjectReferenceEvaluator.Evaluate(config, "Shop.Payments.Application", "Shop.Orders.Contracts");

		selectedEdge.IsAllowed.Should().BeTrue();
		nonMatchingTarget.IsAllowed.Should().BeFalse();
		nonMatchingTarget.ViolationReason.Should().Contain("no AllowedProjectReference permits");
		nonMatchingSource.IsAllowed.Should().BeTrue();
	}

	[Fact]
	public void ProjectReferenceEvaluator_SelectedBlockedRuleWinsOverBroadAllowedRule()
	{
		var config = CreateConfig(
			[
				Group("Application", ".Application"),
				Group("Contracts", ".Contracts")
			],
			[
				new ProjectReferenceRule(ProjectReferenceRuleKind.Allowed, "Application", "Contracts", null, "Architecture.anl", 1, 1),
				new ProjectReferenceRule(
					ProjectReferenceRuleKind.Blocked,
					"Application",
					"Contracts",
					null,
					"Architecture.anl",
					2,
					1,
					[ProjectExactName("Shop.Legacy.Application")],
					[ProjectExactName("Shop.Legacy.Contracts")])
			],
			[],
			false);

		var blockedEvaluation = ProjectReferenceEvaluator.Evaluate(config, "Shop.Legacy.Application", "Shop.Legacy.Contracts");
		var allowedEvaluation = ProjectReferenceEvaluator.Evaluate(config, "Shop.New.Application", "Shop.New.Contracts");

		blockedEvaluation.IsAllowed.Should().BeFalse();
		blockedEvaluation.ViolationReason.Should().Contain("BlockedProjectReference");
		allowedEvaluation.IsAllowed.Should().BeTrue();
	}

	[Fact]
	public void ProjectReferenceEvaluator_WildcardRuleStillHonorsProjectSelectors()
	{
		var config = CreateConfig(
			[
				Group("Application", ".Application"),
				Group("Worker", ".Worker"),
				Group("Contracts", ".Contracts")
			],
			[
				new ProjectReferenceRule(
					ProjectReferenceRuleKind.Allowed,
					"*",
					"Contracts",
					null,
					"Architecture.anl",
					1,
					1,
					[ProjectExactName("Shop.Orders.Application")],
					[ProjectExactName("Shop.Orders.Contracts")])
			],
			[],
			false);

		var selectedEvaluation = ProjectReferenceEvaluator.Evaluate(config, "Shop.Orders.Application", "Shop.Orders.Contracts");
		var differentSourceEvaluation = ProjectReferenceEvaluator.Evaluate(config, "Shop.Notifications.Worker", "Shop.Orders.Contracts");
		var differentTargetEvaluation = ProjectReferenceEvaluator.Evaluate(config, "Shop.Orders.Application", "Shop.Notifications.Contracts");

		selectedEvaluation.IsAllowed.Should().BeTrue();
		differentSourceEvaluation.IsAllowed.Should().BeTrue();
		differentTargetEvaluation.IsAllowed.Should().BeFalse();
	}

	[Fact]
	public void SolutionTopologyEvaluator_EnforcesAllowedAndBlockedModuleEdges()
	{
		var config = new SolutionTopologyConfig(
			[
				Module("Web", "Shop.Web"),
				Module("Application", "Shop.Application"),
				Module("Infrastructure", "Shop.Infrastructure")
			],
			[
				new SolutionModuleReferenceRule(SolutionModuleReferenceRuleKind.Allowed, "Web", "Application", null, "Architecture.anl", 1, 1),
				new SolutionModuleReferenceRule(SolutionModuleReferenceRuleKind.Blocked, "Application", "Infrastructure", null, "Architecture.anl", 2, 1)
			],
			true,
			false);

		var allowedEvaluation = SolutionTopologyEvaluator.Evaluate(config, "Shop.Web", "Shop.Application");
		var blockedEvaluation = SolutionTopologyEvaluator.Evaluate(config, "Shop.Application", "Shop.Infrastructure");
		var missingEdgeEvaluation = SolutionTopologyEvaluator.Evaluate(config, "Shop.Web", "Shop.Infrastructure");

		allowedEvaluation.IsAllowed.Should().BeTrue();
		blockedEvaluation.IsAllowed.Should().BeFalse();
		blockedEvaluation.ViolationReason.Should().Contain("BlockedModuleReference");
		missingEdgeEvaluation.IsAllowed.Should().BeFalse();
		missingEdgeEvaluation.ViolationReason.Should().Contain("no AllowedModuleReference permits");
	}

	[Fact]
	public void SolutionTopologyAnalysisService_ReportsUnrecognizedProjectsWhenRequired()
	{
		var config = new SolutionTopologyConfig(
			[
				Module("Application", "Shop.Application")
			],
			[],
			true,
			false);

		var result = SolutionTopologyAnalysisService.Analyze(
			config,
			[
				new SolutionProjectReference("Shop.Application.csproj", "Shop.Application", "Shop.Unknown.csproj", "Shop.Unknown")
			]);

		var violation = result.ReferenceViolations.Should().ContainSingle().Subject;
		violation.SourceModule.Should().Be("Application");
		violation.TargetModule.Should().BeNull();
		violation.ViolationReason.Should().Contain("not assigned to a configured solution module");
	}

	[Fact]
	public void SolutionTopologyCycleDetector_FindsConfiguredLogicalModuleCycles()
	{
		var config = new SolutionTopologyConfig(
			[
				Module("Web", "Shop.Web"),
				Module("Application", "Shop.Application"),
				Module("Contracts", "Shop.Contracts")
			],
			[
				new SolutionModuleReferenceRule(SolutionModuleReferenceRuleKind.Allowed, "Web", "Application", null, "Architecture.anl", 1, 1),
				new SolutionModuleReferenceRule(SolutionModuleReferenceRuleKind.Allowed, "Application", "Contracts", null, "Architecture.anl", 2, 1),
				new SolutionModuleReferenceRule(SolutionModuleReferenceRuleKind.Allowed, "Contracts", "Web", null, "Architecture.anl", 3, 1)
			],
			false,
			true);

		var cycles = SolutionTopologyCycleDetector.FindConfiguredCycles(config);

		cycles.Should().ContainSingle();
		cycles[0].GetDisplayPath().Should().Be("Application -> Contracts -> Web -> Application");
		cycles[0].Rules.Should().HaveCount(3);
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
	public void AssemblyReferenceEvaluator_UsesCaseInsensitiveForbiddenAndAllowlistRules()
	{
		var config = CreateConfig(
			[
				Group("Domain", ".Domain")
			],
			[],
			[],
			false,
			[
				new AssemblyReferencePolicy(
					"Domain",
					[ReferenceStartsWith("system.")],
					[ReferenceExactName("SYSTEM.XML")],
					null,
					"Architecture.anl",
					1,
					1)
			]);

		var forbiddenEvaluation = AssemblyReferenceEvaluator.Evaluate(config, "Shop.Domain", "System.Xml");
		var allowlistEvaluation = AssemblyReferenceEvaluator.Evaluate(config, "Shop.Domain", "Company.Legacy");

		forbiddenEvaluation.IsAllowed.Should().BeFalse();
		forbiddenEvaluation.ViolationReason.Should().Contain("Forbidden policy");
		allowlistEvaluation.IsAllowed.Should().BeFalse();
		allowlistEvaluation.ViolationReason.Should().Contain("Allowed assembly list");
	}

	[Fact]
	public void AnalysisService_CollectsProjectPackageAndAssemblyViolations()
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
			true,
			[
				new AssemblyReferencePolicy(
					"Domain",
					[],
					[ReferenceExactName("Legacy.Transport")],
					null,
					"Architecture.anl",
					1,
					1)
			]);
		var manifest = new ArchitectureReferenceManifest(
			[
				new ProjectReferenceManifestRecord(@"D:\src\Shop.Domain.csproj", @"D:\src\Shop.Infrastructure.csproj")
			],
			[
				new ArchitecturePackageReference(@"D:\src\Shop.Domain.csproj", "Microsoft.Extensions.Logging", "9.0.0", PackageReferenceKind.Direct)
			],
			[
				new ArchitectureAssemblyReference(@"D:\src\Shop.Domain.csproj", "Legacy.Transport", @"lib\Legacy.Transport.dll")
			]);

		var result = ProjectArchitectureAnalysisService.Analyze(config, manifest);

		result.ProjectReferenceViolations.Should().ContainSingle();
		result.ProjectReferenceViolations[0].SourceProjectName.Should().Be("Shop.Domain");
		result.ProjectReferenceViolations[0].TargetProjectName.Should().Be("Shop.Infrastructure");
		result.ProjectReferenceViolations[0].SourceProjectGroup.Should().Be("Domain");
		result.PackageReferenceViolations.Should().ContainSingle();
		result.PackageReferenceViolations[0].SourceProjectName.Should().Be("Shop.Domain");
		result.PackageReferenceViolations[0].SourceProjectGroup.Should().Be("Domain");
		result.AssemblyReferenceViolations.Should().ContainSingle();
		result.AssemblyReferenceViolations[0].SourceProjectName.Should().Be("Shop.Domain");
		result.AssemblyReferenceViolations[0].HintPath.Should().Be(@"lib\Legacy.Transport.dll");
	}

	[Fact]
	public void AnalysisService_NormalizesMixedPlatformPathsBeforeApplyingExactProjectSelectors()
	{
		var config = CreateConfig(
			[
				Group("Application", ".Application"),
				Group("Contracts", ".Contracts")
			],
			[
				new ProjectReferenceRule(
					ProjectReferenceRuleKind.Allowed,
					"Application",
					"Contracts",
					null,
					"Architecture.anl",
					1,
					1,
					[ProjectExactName("Shop.Orders.Application")],
					[ProjectExactName("Shop.Orders.Contracts")])
			],
			[],
			true);
		var manifest = new ArchitectureReferenceManifest(
			[
				new ProjectReferenceManifestRecord(@"D:\src\Shop.Orders.Application.csproj", "/src/Shop.Payments.Contracts.csproj")
			],
			[]);

		var result = ProjectArchitectureAnalysisService.Analyze(config, manifest);

		var violation = result.ProjectReferenceViolations.Should().ContainSingle().Subject;
		violation.SourceProjectName.Should().Be("Shop.Orders.Application");
		violation.TargetProjectName.Should().Be("Shop.Payments.Contracts");
		violation.ViolationReason.Should().Contain("no AllowedProjectReference permits");
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
		var assemblyViolation = new AssemblyReferenceViolationFinding(
			@"D:\src\Shop.Domain.csproj",
			"Shop.Domain",
			"Domain",
			"Legacy.Transport",
			@"lib\Legacy.Transport.dll",
			"the assembly matches a Forbidden policy for project group 'Domain'",
			null,
			null);

		var projectFinding = projectViolation.ToArchitectureFinding();
		var packageFinding = packageViolation.ToArchitectureFinding();
		var assemblyFinding = assemblyViolation.ToArchitectureFinding();

		projectFinding.Code.Should().Be(ArchitecturalDiagnosticIds.ProjectReferenceNotAllowed);
		projectFinding.Properties[ArchitectureDiagnosticProperties.PropertySourceProjectGroup].Should().Be("Presentation");
		packageFinding.Code.Should().Be(ArchitecturalDiagnosticIds.PackageReferenceNotAllowed);
		packageFinding.Properties[ArchitectureDiagnosticProperties.PropertyPackageId].Should().Be("Microsoft.Extensions.Logging");
		assemblyFinding.Code.Should().Be(ArchitectureFindingCodes.AssemblyReferencePolicyViolation);
		assemblyFinding.Properties[ArchitectureDiagnosticProperties.PropertyAssemblyIdentity].Should().Be("Legacy.Transport");
	}

	private static ProjectArchitectureConfig CreateConfig(
		ImmutableArray<ProjectGroup> groups,
		ImmutableArray<ProjectReferenceRule> rules,
		ImmutableArray<PackagePolicy> packagePolicies,
		bool requireRecognizedProjects,
		ImmutableArray<AssemblyReferencePolicy> assemblyReferencePolicies = default)
	{
		var normalizedAssemblyReferencePolicies = assemblyReferencePolicies.IsDefault ? [] : assemblyReferencePolicies;
		var result = new ProjectArchitectureConfig(groups, rules, packagePolicies, normalizedAssemblyReferencePolicies, requireRecognizedProjects);

		return result;
	}

	private static ProjectGroup Group(string name, string projectSuffix)
	{
		var result = new ProjectGroup(name, [new ProjectMatcher([new MatchCondition(MatchKind.EndsWith, projectSuffix)])], null, "Architecture.anl", 1, 1);

		return result;
	}

	private static ProjectMatcher ProjectExactName(string projectName)
	{
		var result = new ProjectMatcher([new MatchCondition(MatchKind.Equals, projectName)]);

		return result;
	}

	private static SolutionModule Module(string name, string projectName)
	{
		var result = new SolutionModule(name, [ProjectExactName(projectName)], null, "Architecture.anl", 1, 1);

		return result;
	}

	private static ReferenceIdentityMatcher PackageStartsWith(string prefix)
	{
		var result = new ReferenceIdentityMatcher([new MatchCondition(MatchKind.StartsWith, prefix)], null, null, "Architecture.anl", 1, 1);

		return result;
	}

	private static ReferenceIdentityMatcher PackageExactName(string packageId)
	{
		var result = new ReferenceIdentityMatcher([new MatchCondition(MatchKind.Equals, packageId)], null, null, "Architecture.anl", 1, 1);

		return result;
	}

	private static ReferenceIdentityMatcher ReferenceStartsWith(string prefix)
	{
		var result = new ReferenceIdentityMatcher([new MatchCondition(MatchKind.StartsWith, prefix)], null, null, "Architecture.anl", 1, 1);

		return result;
	}

	private static ReferenceIdentityMatcher ReferenceExactName(string identity)
	{
		var result = new ReferenceIdentityMatcher([new MatchCondition(MatchKind.Equals, identity)], null, null, "Architecture.anl", 1, 1);

		return result;
	}
}
