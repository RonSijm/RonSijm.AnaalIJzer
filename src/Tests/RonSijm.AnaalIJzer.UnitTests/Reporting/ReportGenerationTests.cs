using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Parsing;
using RonSijm.AnaalIJzer.Documentation;
using RonSijm.AnaalIJzer.Violations;
using RonSijm.AnaalIJzer.UnitTests.TestSupport;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.UnitTests.Reporting;

public sealed class ReportGenerationTests
{
	[Fact]
	public async Task Analyzer_DoesNotWriteReportOrDocumentationFiles()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"arch-analyzer-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDir);

		try
		{
			var configPath = Path.Combine(tempDir, "Architecture.anl");
			var reportPath = Path.Combine(tempDir, "violations.md");
			var documentationPath = Path.Combine(tempDir, "architecture.md");

			var config = $"""
			              <ArchitecturalLevels requireRecognizedDependencies="Constructor"
			                                    enableReport="true"
			                                    reportPath="{reportPath}"
			                                    enableDocumentation="true"
			                                    documentationPath="{documentationPath}">
			                  <Layer name="Manager">
			                      <Class endsWith="Manager" />
			                  </Layer>
			                  <Layer name="Repository">
			                      <Class endsWith="Repository" />
			                  </Layer>
			                  <AllowedDependency from="Manager" to="Repository" />
			              </ArchitecturalLevels>
			              """;

			const string source = """
			                      public interface IPartnerStore { }
			                      public class PatientConsentRepository { }
			                      public class PatientManager(PatientConsentRepository repo, IPartnerStore store) { }
			                      """;

			var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config, configPath);

			diagnostics.Should().Contain(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency);
			File.Exists(reportPath).Should().BeFalse("the analyzer no longer writes report files during compilation");
			File.Exists(documentationPath).Should().BeFalse("Arse owns documentation generation");
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public async Task ViolationReporter_RendersAnalyzerDiagnostics()
	{
		const string config = """
		                      <ArchitecturalLevels requireRecognizedDependencies="Constructor">
		                          <Forbidden>
		                              <Class endsWith="Store" comment="Use Repository instead." />
		                          </Forbidden>
		                          <Layer name="Manager">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Layer name="Repository">
		                              <Class endsWith="Repository" />
		                          </Layer>
		                          <AllowedDependency from="Manager" to="Repository" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class MysteryTopping { }
		                      public class CheeseStore { }
		                      public class PatientManager(MysteryTopping topping, CheeseStore cheeseStore) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);
		var report = ArchitecturalViolationReporter.GenerateMarkdownReport(diagnostics, ParseConfig(config), "Test.Assembly");

		report.Should().Contain("**Assembly**: `Test.Assembly`");
		report.Should().Contain("| ARCH002 — Unrecognized dependency | 1 |");
		report.Should().Contain("| ARCH003 — Type policy violation | 1 |");
		report.Should().Contain("| `PatientManager` (Manager) | `MysteryTopping` |");
		report.Should().Contain("| `PatientManager` (Manager) | `CheeseStore` | the type matches a global &lt;Forbidden&gt; rule: Use Repository instead. |");

		var unrecognizedDiagnostic = diagnostics.Single(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency);
		unrecognizedDiagnostic.Properties[ArchitecturalDiagnostics.PropertyCallerTypeName].Should().Be("PatientManager");
		unrecognizedDiagnostic.Properties[ArchitecturalDiagnostics.PropertyCallerLayerName].Should().Be("Manager");
		unrecognizedDiagnostic.Properties[ArchitecturalDiagnostics.PropertyDepTypeName].Should().Be("MysteryTopping");
	}

	[Fact]
	public void DocumentationGenerator_RendersDescriptionsWildcardsAndEscapesMermaidLabels()
	{
		var config = ParseConfig("""
		                         <ArchitecturalLevels description="Rules for the pizzeria">
		                             <Allowed description="Only approved dependency names.">
		                                 <Class startsWith="Order" endsWith="Contract" typeKind="Interface" description="Order contract interfaces are approved." />
		                             </Allowed>
		                             <Forbidden description="Names that make the kitchen nervous">
		                                 <Class endsWith="Store" comment="Use Repository instead." description="Infrastructure should be a repository." />
		                                 <Namespace contains="Bad &quot;Ns&quot;" comment="Do not use internals." description="The basement is not public API." />
		                             </Forbidden>
		                             <Layer name="Controller" description="Waiters taking orders.">
		                                 <Class endsWith="Controller" description="Controller classes live here." />
		                             </Layer>
		                             <Layer name="Data | &quot;Storage&quot;" description="Cheese fridge.">
		                                 <Class endsWith="Repository" description="Storage access lives here." />
		                             </Layer>
		                             <Layer name="Crosscutting">
		                                 <Class typeName="ILogger" />
		                             </Layer>
		                             <Layer name="Diagnostics">
		                                 <Class endsWith="Diagnostics" />
		                             </Layer>
		                             <AllowedDependency from="Controller" to="Data | &quot;Storage&quot;" allowedSites="Constructor, Local" description="Waiters can receive the fridge in approved places only." />
		                             <BlockedDependency from="Controller" to="Data | &quot;Storage&quot;" allowedSites="Field" description="Controllers may not retain storage." />
		                             <AllowedDependency from="*" to="Crosscutting" description="Logging is available everywhere." />
		                             <AllowedDependency from="Diagnostics" to="*" description="Diagnostics can inspect every configured layer." />
		                             <AllowedDependency from="*" to="*" blockedSites="Field, Property" description="Legacy anything-to-anything except long-lived state." />
		                         </ArchitecturalLevels>
		                         """);

		var markdown = ArchitectureDocumentationGenerator.GenerateMarkdown(config, null);

		markdown.Should().NotContain("**Assembly**");
		markdown.Should().Contain("# Architecture Documentation");
		markdown.Should().Contain("Rules for the pizzeria");
		markdown.Should().Contain("L_Data____Storage_[\"Data &#124; &quot;Storage&quot;\"]");
		markdown.Should().Contain("Any([\"all layers\"])");
		markdown.Should().Contain("L_Controller -->|\"allowed sites: Constructor, Local\"| L_Data____Storage_");
		markdown.Should().Contain("L_Controller -. \"blocked: allowed sites: Field\" .-> L_Data____Storage_");
		markdown.Should().Contain("Any --> L_Crosscutting");
		markdown.Should().Contain("L_Diagnostics --> Any");
		markdown.Should().Contain("Any -->|\"blocked sites: Field, Property\"| Any");
		markdown.Should().Contain("style Any fill:#fff4cc");
		markdown.Should().Contain("| `Controller` | Waiters taking orders. |");
		markdown.Should().Contain("| Allowed | `root` | `Controller -> Data \\| \"Storage\"` | allowed sites: Constructor, Local | Waiters can receive the fridge in approved places only. |");
		markdown.Should().Contain("| Blocked | `root` | `Controller -> Data \\| \"Storage\"` | allowed sites: Field | Controllers may not retain storage. |");
		markdown.Should().Contain("## Type Policies");
		markdown.Should().Contain("| Allowed | `global` | `Class startsWith=\"Order\" endsWith=\"Contract\" typeKind=\"Interface\"` | Order contract interfaces are approved. |");
		markdown.Should().Contain("| Forbidden | `global` | `Class endsWith=\"Store\"` | Use Repository instead. |");
		markdown.Should().Contain("| Forbidden | `global` | `Namespace contains=\"Bad \"Ns\"\"` | Do not use internals. |");
		markdown.Should().Contain("- **AllowedDependency** `Controller -> Data | \"Storage\"`");
		markdown.Should().Contain("- **BlockedDependency** `Controller -x-> Data | \"Storage\"`");
		markdown.Should().Contain("Infrastructure should be a repository.");
	}

	[Fact]
	public void DocumentationGenerator_RendersNestedBoundariesAndScopedDescriptions()
	{
		var config = ParseConfig("""
		                         <ArchitecturalLevels description="A modular candy shop.">
		                           <Layer name="Ordering" description="Owns ordering.">
		                             <Namespace startsWith="CandyShop.Ordering" />
		                             <Layer name="Application" description="Ordering use cases."><Class endsWith="Service" /></Layer>
		                             <Layer name="Repository" description="Ordering storage."><Class endsWith="Repository" /></Layer>
		                             <AllowedDependency from="Application" to="Repository" description="Use cases may store orders." />
		                             <AllowedDependency from="Application" to="/Billing/Contracts" description="Ordering egress." />
		                           </Layer>
		                           <Layer name="Billing" description="Owns billing.">
		                             <Namespace startsWith="CandyShop.Billing" />
		                             <Layer name="Application" description="Billing use cases."><Class endsWith="Service" /></Layer>
		                             <Layer name="Contracts" description="Billing entry point."><Class endsWith="Contract" /></Layer>
		                             <AllowedDependency from="/Ordering/Application" to="Contracts" description="Billing ingress." />
		                           </Layer>
		                           <AllowedDependency from="Ordering" to="Billing" description="Module relationship." />
		                         </ArchitecturalLevels>
		                         """);

		var markdown = ArchitectureDocumentationGenerator.GenerateMarkdown(config, null);

		markdown.Should().Contain("subgraph SG_Ordering[\"Ordering\"]");
		markdown.Should().Contain("subgraph SG_Billing[\"Billing\"]");
		markdown.Should().NotContain("(boundary)");
		markdown.Should().Contain("SG_Ordering --> SG_Billing");
		markdown.Should().Contain("L_Ordering_Application[\"Application\"]");
		markdown.Should().Contain("L_Billing_Application[\"Application\"]");
		markdown.Should().Contain("L_Ordering_Application --> L_Ordering_Repository");
		markdown.Should().Contain("L_Ordering_Application --> L_Billing_Contracts");
		markdown.Should().Contain("| `Ordering/Application` | Ordering use cases. |");
		markdown.Should().Contain("| Allowed | `Ordering` | `Ordering/Application -> Ordering/Repository` | all sites | Use cases may store orders. |");
		markdown.Should().Contain("Billing ingress.");
	}

	[Fact]
	public void ViolationReporter_RendersEveryDiagnosticSection()
	{
		var violations = new[]
		{
			new ViolationRecord(ArchitecturalDiagnosticIds.IllegalLevelDependency, "MenuController", "Controller", "ICheeseRepository", "Repository", "no Controller -> Repository edge", null),
			new ViolationRecord(ArchitecturalDiagnosticIds.UnrecognizedDependency, "OvenCoordinator", "Application", "MysteryTopping", string.Empty, string.Empty, "unknown ingredient"),
			new ViolationRecord(ArchitecturalDiagnosticIds.ForbiddenDependency, "ToppingManager", "Application", "CheeseStore", string.Empty, string.Empty, "Use Repository instead."),
			new ViolationRecord(ArchitecturalDiagnosticIds.WrongDirectionDependency, "CheeseRepository", "Repository", "IPizzaKitchen", "Application", "reverse edge", null),
			new ViolationRecord(ArchitecturalDiagnosticIds.SameLayerDependency, "PizzaKitchen", "Application", "ISauceKitchen", "Application", "same layer", null)
		};

		var report = ArchitecturalViolationReporter.GenerateMarkdownReport(
			violations,
			AnalyzerConfiguration.Empty,
			null);

		report.Should().NotContain("**Assembly**");
		report.Should().Contain("| **Total** | **5** |");
		report.Should().Contain("## ARCH001");
		report.Should().Contain("| `MenuController` (Controller) | `ICheeseRepository` (Repository) | no Controller -> Repository edge |");
		report.Should().Contain("## ARCH002");
		report.Should().Contain("| `OvenCoordinator` (Application) | `MysteryTopping` | unknown ingredient |");
		report.Should().Contain("<Layer name=\"MysteryTopping\">");
		report.Should().Contain("<AllowedDependency from=\"Application\" to=\"MysteryTopping\" />");
		report.Should().Contain("## ARCH003");
		report.Should().Contain("| `ToppingManager` (Application) | `CheeseStore` | Use Repository instead. |");
		report.Should().Contain("## ARCH004");
		report.Should().Contain("| `CheeseRepository` (Repository) | `IPizzaKitchen` (Application) | reverse edge |");
		report.Should().Contain("## ARCH005");
		report.Should().Contain("| `PizzaKitchen` (Application) | `ISauceKitchen` | same layer |");
	}

	private static AnalyzerConfiguration ParseConfig(string config)
	{
		var additionalText = new TestAdditionalText("Architecture.anl", config);

		var result = ArchitecturalConfigParser.Parse(
			ImmutableArray.Create<AdditionalText>(additionalText),
			CancellationToken.None);

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
