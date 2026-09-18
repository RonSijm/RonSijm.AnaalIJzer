using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Violations;
using RonSijm.AnaalIJzer.Outputs.Documentation;
using RonSijm.AnaalIJzer.Outputs.Violations;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Outputs.Tests.Reporting;

public sealed class NamespaceHierarchyPolicyOutputTests
{
	[Fact]
	public void DocumentationGenerator_RendersNamespaceHierarchyPoliciesInConfigurationOrder()
	{
		var config = ParseConfig("""
			<ArchitecturalLevels>
			  <NamespaceHierarchyPolicy rootNamespace="Restaurant" description="Feature namespaces own their recipes.">
			    <BlockedRelation relation="DescendantToAncestor" allowedSites="Constructor, Field" description="Orders do not reach back into the restaurant root." />
			    <BlockedRelation relation="SiblingToSibling" blockedSites="Method" />
			  </NamespaceHierarchyPolicy>
			</ArchitecturalLevels>
			""");

		var markdown = ArchitectureDocumentationGenerator.GenerateMarkdown(config, null);

		markdown.Should().Contain("## Namespace Hierarchy Policies");
		markdown.Should().Contain("| `Restaurant` | `DescendantToAncestor` | Only Constructor, Field | Orders do not reach back into the restaurant root. |");
		markdown.Should().Contain("| `Restaurant` | `SiblingToSibling` | All except Method | Feature namespaces own their recipes. |");
		markdown.IndexOf("DescendantToAncestor", StringComparison.Ordinal).Should().BeLessThan(markdown.IndexOf("SiblingToSibling", StringComparison.Ordinal));
		markdown.Should().Contain("- **NamespaceHierarchyPolicy** `Namespace hierarchy for Restaurant`");
		markdown.Should().Contain("- **BlockedRelation** `Block DescendantToAncestor`");
	}

	[Fact]
	public void ViolationReporter_RendersNamespaceHierarchyViolation()
	{
		var report = ArchitecturalViolationReporter.GenerateMarkdownReport(
			[
				new ViolationRecord(
					ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement,
					"PizzaRequest",
					"Restaurant.Orders",
					"RestaurantRecipe",
					"Restaurant",
					"NamespaceHierarchyPolicy 'Restaurant' blocks DescendantToAncestor dependencies.",
					null,
					"Constructor")
			],
			AnalyzerConfiguration.Empty,
			null);

		report.Should().Contain("| Namespace | BoundaryPlacement | `ARCH_NS_007` — Namespace hierarchy dependency violation | 1 |");
		report.Should().Contain("## ARCH_NS_007 — Namespace Boundary Placement Violations");
		report.Should().Contain("`PizzaRequest` (Restaurant.Orders)");
		report.Should().Contain("`RestaurantRecipe` (Restaurant)");
		report.Should().Contain("`Constructor`");
	}

	private static AnalyzerConfiguration ParseConfig(string config)
	{
		var additionalText = new TestAdditionalText("Architecture.anl", config);
		var result = ArchitecturalConfigParser.Parse([additionalText], CancellationToken.None);

		return result;
	}

	private sealed class TestAdditionalText(string path, string content) : AdditionalText
	{
		private readonly SourceText _text = SourceText.From(content);

		public override string Path { get; } = path;

		public override SourceText GetText(CancellationToken cancellationToken = default)
		{
			var result = _text;

			return result;
		}
	}
}
