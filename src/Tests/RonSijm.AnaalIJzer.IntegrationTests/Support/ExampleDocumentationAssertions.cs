using AwesomeAssertions;

namespace RonSijm.AnaalIJzer.IntegrationTests;

internal static class ExampleDocumentationAssertions
{
	public static void VerifyProjectDocumentation(string relativeProjectPath, string documentation)
	{
		documentation.Should().Contain("## Code Evidence", $"project documentation for {relativeProjectPath} should include the richest project-backed evidence");
		documentation.Should().Contain("## Input Configuration", $"project documentation for {relativeProjectPath} should show the exact config it was based on");

		if (documentation.Contains("No layers are configured.", StringComparison.Ordinal))
		{
			documentation.Should().NotContain("```mermaid", $"project documentation for {relativeProjectPath} should not emit an empty layer diagram when the configuration only contains project/package policies");
		}
		else
		{
			documentation.Should().Contain("```mermaid", $"project documentation for {relativeProjectPath} should include diagrams");
		}

		if (relativeProjectPath.Contains("Example.CombinedMatchers", StringComparison.Ordinal))
		{
			documentation.Should().Contain("Class endsWith=\"Repository\" typeKind=\"Interface\"");
			documentation.Should().Contain("IExampleRepository");
			documentation.Should().Contain("ExampleRepository");
		}
	}

	public static void VerifyStandaloneConfigurationDocumentation(string relativeConfigurationPath, string documentation)
	{
		documentation.Should().Contain("## Input Configuration", $"configuration documentation for {relativeConfigurationPath} should show the exact XML it was based on");
		documentation.Should().Contain("```mermaid", $"configuration documentation for {relativeConfigurationPath} should include diagrams");
		documentation.Should().NotContain("## Code Evidence", $"standalone configuration documentation for {relativeConfigurationPath} cannot include compiled project evidence");
	}

	public static void VerifyHealthReport(string healthReport)
	{
		healthReport.Should().Contain("Configured cycle");
		healthReport.Should().Contain("Observed dependency cycle");
		healthReport.Should().Contain("Unclassified type");
		healthReport.Should().Contain("Unmatched matcher");
		healthReport.Should().Contain("ARCH017");
		healthReport.Should().Contain("stale");
		healthReport.Should().Contain("Unused allowed edge");
	}

	public static void VerifyMergedConfiguration(string mergedConfiguration)
	{
		mergedConfiguration.Should().Contain("<Layer ");
		mergedConfiguration.Should().Contain("<AllowedDependency ");
		mergedConfiguration.Should().Contain("<BlockedDependency ");
		mergedConfiguration.Should().Contain("typeKind=\"Interface\"");
		mergedConfiguration.Should().Contain("appliesToDescendants=\"true\"");
		mergedConfiguration.Should().NotContain("<Include ");
	}

	public static void VerifyMergedDocumentation(string mergedDocumentation)
	{
		mergedDocumentation.Should().Contain("## Root Settings");
		mergedDocumentation.Should().Contain("## Exception Policy");
		mergedDocumentation.Should().Contain("### Exception Matchers");
		mergedDocumentation.Should().Contain("### Exception Reviews");
		mergedDocumentation.Should().Contain("## Dependency Flow");
		mergedDocumentation.Should().Contain("```mermaid");
		mergedDocumentation.Should().Contain("## Rules In Configuration Order");
		mergedDocumentation.Should().Contain("## Input Configuration");
		mergedDocumentation.Should().Contain("allowedSites", "site-filter labels should survive the all-examples merge and appear in documentation");
		mergedDocumentation.Should().Contain("appliesToDescendants=\"true\"", "cascading dependency rules should survive merge and documentation generation");
		mergedDocumentation.Should().Contain("applies to descendants", "cascading dependency rules should be visible in rendered documentation");
		mergedDocumentation.Should().Contain("typeKind=\"Interface\"", "combined matcher conditions should survive merge and documentation generation");
		mergedDocumentation.Should().Contain("QuerySurface", "scenario-specific layers should survive the all-examples merge");
		mergedDocumentation.Should().Contain("Persistence", "starter configuration layers should survive the all-examples merge");
		mergedDocumentation.Should().Contain("## Type Policies", "allowed and forbidden type-policy examples should survive the all-examples merge");
		mergedDocumentation.Should().Contain("| Allowed | `global` |", "global allow-list examples should be rendered with their scope");
		mergedDocumentation.Should().Contain("| Forbidden | `Query` |", "layer-scoped forbidden examples should be rendered with their scope");
	}

	public static void VerifyFeatureMatrixDocumentation(string documentation)
	{
		documentation.Should().Contain("# Architecture Documentation");
		documentation.Should().Contain("Feature matrix configuration for documentation coverage.");
		documentation.Should().Contain("## Root Settings");
		documentation.Should().Contain("## Exception Policy");
		documentation.Should().Contain("| `requireReason` | `true` | Each exception matcher must declare a human-readable reason. |");
		documentation.Should().Contain("| `warnBeforeDays` | `30` | Expiring exceptions warn this many days before expiry. |");
		documentation.Should().Contain("### Exception Matchers");
		documentation.Should().Contain("Legacy migration is still in progress.");
		documentation.Should().Contain("Ordering Team");
		documentation.Should().Contain("2026-08-30");
		documentation.Should().Contain("| `enforceObservedAcyclic` | `true` | Observed source dependencies may not form a cycle. |");
		documentation.Should().Contain("## Dependency Flow");
		documentation.Should().Contain("```mermaid");
		documentation.Should().Contain("subgraph SG_Ordering");
		documentation.Should().Contain("Ordering/Application");
		documentation.Should().Contain("Ordering/Repository");
		documentation.Should().Contain("### Universal Rules");
		documentation.Should().Contain("all layers");
		documentation.Should().Contain("allowed sites: Constructor, Method");
		documentation.Should().Contain("allowed sites: MethodReturn");
		documentation.Should().Contain("blocked sites: Field; applies to descendants");
		documentation.Should().Contain("appliesToDescendants=\"true\"");
		documentation.Should().Contain("## Type Policies");
		documentation.Should().Contain("| Allowed | `global` | `Class startsWith=\"I\" endsWith=\"Contract\" typeKind=\"Interface\"` | Interface contracts are globally approved. |");
		documentation.Should().Contain("| Allowed | `Ordering/Application` | `Class endsWith=\"Contract\" typeKind=\"Interface\"` | Application code may consume contract interfaces. |");
		documentation.Should().Contain("| Forbidden | `global` | `Class endsWith=\"Store\" typeKind=\"Class\"` | Use Repository instead. |");
		documentation.Should().Contain("| Forbidden | `Ordering/Repository` | `Namespace contains=\".Legacy\"` | Legacy persistence namespace is blocked. |");
		documentation.Should().Contain("## Rules In Configuration Order");
		documentation.Should().Contain("- **Layer** `Ordering`");
		documentation.Should().Contain("Ordering boundary with nested application and repository roles.");
		documentation.Should().Contain("- **Assembly** `Assembly exactName=\"CandyShop.Persistence\"`");
		documentation.Should().Contain("- **Exceptions** `Exceptions`");
		documentation.Should().Contain("Legacy store names are grandfathered.");
		documentation.Should().Contain("- **Fix** `Fix Repository`");
		documentation.Should().Contain("Rename=\"Repository\"");
		documentation.Should().Contain("## Input Configuration");
		documentation.Should().Contain("This documentation was generated from the following architecture configuration: `Architecture.anl`.");
		documentation.Should().Contain("enableDocumentation=\"true\"");
		documentation.Should().Contain("documentationPath=\"docs\\architecture-documentation.md\"");
		documentation.Should().Contain("enableReport=\"true\"");
		documentation.Should().Contain("requireRecognizedDependencies=\"Constructor, Local\"");
		documentation.Should().Contain("requireRecognizedDependencies=\"MethodReturn\"");
		documentation.Should().Contain("enforceAcyclic=\"true\"");
		documentation.Should().Contain("enforceObservedAcyclic=\"true\"");
	}
}
