using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Config.Parsing;
using RonSijm.AnaalIJzer.Documentation;
using RonSijm.AnaalIJzer.Outputs.Tests.TestSupport;
using RonSijm.AnaalIJzer.Violations;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Outputs.Tests.Reporting;

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

			var diagnostics = await AnalyzerOutputTestHelper.GetDiagnosticsAsync(source, config, configPath);

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

		var diagnostics = await AnalyzerOutputTestHelper.GetDiagnosticsAsync(source, config);
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
			new ViolationRecord(ArchitecturalDiagnosticIds.SameLayerDependency, "PizzaKitchen", "Application", "ISauceKitchen", "Application", "same layer", null),
			new ViolationRecord(ArchitecturalDiagnosticIds.ApiSurfaceLeakage, "CandyOrderingService", "Application", "LollyQueryable", "RepositoryQuerySurface", "query surfaces are blocked", null, "MethodReturn", null, "CandyOrderingService.OrderRawLolly"),
			new ViolationRecord(ArchitecturalDiagnosticIds.ProjectReferenceViolation, "Shop.Web", "Presentation", "Shop.Domain", "Domain", "no AllowedProjectReference permits project group 'Presentation' to reference project group 'Domain'", null, sourceProjectName: "Shop.Web", sourceProjectGroup: "Presentation", targetProjectName: "Shop.Domain", targetProjectGroup: "Domain"),
			new ViolationRecord(ArchitecturalDiagnosticIds.PackageReferenceViolation, "Shop.Domain", "Domain", "Microsoft.Extensions.Logging", "9.0.0", "Domain may not reference infrastructure packages", null, sourceProjectName: "Shop.Domain", sourceProjectGroup: "Domain", packageId: "Microsoft.Extensions.Logging", packageVersion: "9.0.0", packageReferenceKind: "Direct"),
			new ViolationRecord(ArchitecturalDiagnosticIds.VisibilityPolicyViolation, "LollyQueryable", "RepositoryQuerySurface", "LollyQueryable.CurrentQuery", string.Empty, "public properties are blocked", null, "Property", "Public"),
			new ViolationRecord(ArchitecturalDiagnosticIds.ContractPurityViolation, "IPizzaContract.Name", "Contracts", "IPizzaContract.Name", string.Empty, "contracts expose getters only", null, "DisallowedPropertyAccessor"),
			new ViolationRecord(ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure, "CandyOrderingService", "Application", "LollyQueryable", "RepositoryQuerySurface", "nested query surfaces are blocked", null, "Property", null, "CandyOrderingService.OrderRawLolly", "CandyOrderingService.OrderRawLolly -> CandyReceipt.RawQuery -> LollyQueryable", 1, "CandyReceipt.RawQuery"),
			new ViolationRecord(ArchitecturalDiagnosticIds.SourceLocationViolation, "CandyOrderingService", "Ordering/Application", string.Empty, string.Empty, "source file 'Infrastructure/CandyOrderingService.cs' does not match an allowed SourceLocations rule for layer 'Ordering'", null, sourceFilePath: @"D:\repo\Shop\Infrastructure\CandyOrderingService.cs", normalizedSourcePath: "Infrastructure/CandyOrderingService.cs", sourceAssemblyName: "Shop.Application"),
			new ViolationRecord(ArchitecturalDiagnosticIds.BoundaryEntryPointViolation, "CandyAdminController", "Presentation", "CandyOrderingService", "Ordering/Implementation", "boundary 'Ordering': the boundary permits entry only through Ordering/Contracts", null, boundaryLayerName: "Ordering", matchedEntryPoint: "Ordering/Contracts"),
			new ViolationRecord(ArchitecturalDiagnosticIds.ObservedDependencyCycle, "Ordering -> Notifications", "Ordering", "Ordering -> Notifications -> Ordering", string.Empty, "Ordering -> Notifications -> Ordering", null, sourceProjectName: "Candy.Shop", cycleLayers: "Ordering|Notifications", cycleLength: 2, observedSites: "Constructor, Method", cycleScope: "Project"),
			new ViolationRecord(ArchitecturalDiagnosticIds.InheritancePolicyViolation, "SyrupEntity", "PersistenceEntities", "SyrupEntity", string.Empty, "persistence entities must inherit Entity", null, "MissingRequiredBaseType")
		};

		var report = ArchitecturalViolationReporter.GenerateMarkdownReport(
			violations,
			AnalyzerConfiguration.Empty,
			null);

		report.Should().NotContain("**Assembly**");
		report.Should().Contain("| **Total** | **15** |");
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
		report.Should().Contain("## ARCH009");
		report.Should().Contain("| `Application` | `CandyOrderingService.OrderRawLolly` | `LollyQueryable` (`RepositoryQuerySurface`) | `MethodReturn` | query surfaces are blocked |");
		report.Should().Contain("## ARCH010");
		report.Should().Contain("| `Shop.Web` (Presentation) | `Shop.Domain` (Domain) | no AllowedProjectReference permits project group 'Presentation' to reference project group 'Domain' |");
		report.Should().Contain("## ARCH011");
		report.Should().Contain("| `Shop.Domain` (Domain) | `Microsoft.Extensions.Logging` | `9.0.0` | `Direct` | Domain may not reference infrastructure packages |");
		report.Should().Contain("## ARCH012");
		report.Should().Contain("| `RepositoryQuerySurface` | `LollyQueryable.CurrentQuery` | `Property` | `Public` | public properties are blocked |");
		report.Should().Contain("## ARCH013");
		report.Should().Contain("| `Contracts` | `IPizzaContract.Name` | `DisallowedPropertyAccessor` | contracts expose getters only |");
		report.Should().Contain("## ARCH014");
		report.Should().Contain("| `Application` | `CandyOrderingService.OrderRawLolly` | `CandyOrderingService.OrderRawLolly -&gt; CandyReceipt.RawQuery -&gt; LollyQueryable` | 1 | `Property` | nested query surfaces are blocked |");
		report.Should().Contain("## ARCH015");
		report.Should().Contain("| `Ordering/Application` | `CandyOrderingService` | `D:\\repo\\Shop\\Infrastructure\\CandyOrderingService.cs` | `Infrastructure/CandyOrderingService.cs` | `Shop.Application` | source file 'Infrastructure/CandyOrderingService.cs' does not match an allowed SourceLocations rule for layer 'Ordering' |");
		report.Should().Contain("## ARCH016");
		report.Should().Contain("| `CandyAdminController` (Presentation) | `Ordering` | `CandyOrderingService` (Ordering/Implementation) | `Ordering/Contracts` | boundary 'Ordering': the boundary permits entry only through Ordering/Contracts |");
		report.Should().Contain("## ARCH018");
		report.Should().Contain("| `Project` | `Ordering -&gt; Notifications -&gt; Ordering` | 2 | `Constructor, Method` | `Candy.Shop` |");
		report.Should().Contain("## ARCH019");
		report.Should().Contain("| `PersistenceEntities` | `SyrupEntity` | `MissingRequiredBaseType` | persistence entities must inherit Entity |");
	}

	[Fact]
	public void DocumentationGenerator_RendersVisibilityPolicyTable()
	{
		var config = ParseConfig("""
			<ArchitecturalLevels>
			  <Layer name="RepositoryQuerySurface">
			    <Class endsWith="Queryable" />
			    <VisibilityPolicy targets="Type" allowedAccessibilities="Internal, File" description="Keep query surfaces internal." />
			    <VisibilityPolicy targets="Field, Property" blockedAccessibilities="Public, Protected" description="Do not expose query state." />
			  </Layer>
			</ArchitecturalLevels>
			""");

		var markdown = ArchitectureDocumentationGenerator.GenerateMarkdown(config, null);

		markdown.Should().Contain("## Visibility Policies");
		markdown.Should().Contain("| `RepositoryQuerySurface` | Type | Allow only | Internal, File | Keep query surfaces internal. |");
		markdown.Should().Contain("| `RepositoryQuerySurface` | Field, Property | Block | Public, Protected | Do not expose query state. |");
		markdown.Should().Contain("- **VisibilityPolicy** `Visibility Type`");
	}

	[Fact]
	public void DocumentationGenerator_RendersApiSurfacePolicyTable()
	{
		var config = ParseConfig("""
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface requireRecognizedTypes="true" description="Public contracts only.">
			      <TransitiveExposure maxDepth="4" description="Inspect public contracts." />
			      <AllowedLayer path="/Contracts" allowedSites="MethodReturn" description="Return DTOs." />
			      <BlockedLayer path="/QuerySurface" blockedSites="Method" description="Never leak query surfaces." />
			    </ApiSurface>
			  </Layer>
			  <Layer name="Contracts"><Class endsWith="Projection" /></Layer>
			  <Layer name="QuerySurface"><Class endsWith="Queryable" /></Layer>
			</ArchitecturalLevels>
			""");

		var markdown = ArchitectureDocumentationGenerator.GenerateMarkdown(config, null);

		markdown.Should().Contain("## API Surface Policies");
		markdown.Should().Contain("| `Application` | Required | 4 | Allow | `/Contracts` | Only MethodReturn | Return DTOs. |");
		markdown.Should().Contain("| `Application` | Required | 4 | Block | `/QuerySurface` | Except Method | Never leak query surfaces. |");
		markdown.Should().Contain("- **ApiSurface** `API surface`");
		markdown.Should().Contain("- **TransitiveExposure** `Traverse public object graph to depth 4`");
		markdown.Should().Contain("- **BlockedLayer** `Block exposure of /QuerySurface`");
	}

	[Fact]
	public void DocumentationGenerator_RendersContractPolicyTable()
	{
		var config = ParseConfig("""
			<ArchitecturalLevels>
			  <Layer name="Contracts">
			    <Class endsWith="Contract" typeKind="Interface" />
			    <ContractPolicy
			      allowedTypeKinds="Interface"
			      allowedMemberKinds="Method, Property"
			      allowedPropertyAccessors="Get, Init"
			      allowMethodBodies="false"
			      allowStaticMembers="false"
			      allowNestedTypes="false"
			      description="Contracts stay abstract." />
			  </Layer>
			</ArchitecturalLevels>
			""");

		var markdown = ArchitectureDocumentationGenerator.GenerateMarkdown(config, null);

		markdown.Should().Contain("## Contract Policies");
		markdown.Should().Contain("| `Contracts` | Interface | Method, Property | Get, Init | false | false | false | Contracts stay abstract. |");
		markdown.Should().Contain("- **ContractPolicy** `ContractPolicy`");
	}

	[Fact]
	public void DocumentationGenerator_RendersInheritancePolicyTable()
	{
		var config = ParseConfig("""
			<ArchitecturalLevels>
			  <Layer name="PersistenceEntities">
			    <Namespace startsWith="Shop.Persistence" />
			    <InheritancePolicy
			      typeKinds="Class, Record"
			      requiredBaseTypes="Entity, AggregateRoot"
			      requiredInterfaces="IAuditedEntity"
			      description="Persistence entities use the shared entity contract." />
			  </Layer>
			</ArchitecturalLevels>
			""");

		var markdown = ArchitectureDocumentationGenerator.GenerateMarkdown(config, null);

		markdown.Should().Contain("## Inheritance Policies");
		markdown.Should().Contain("| `PersistenceEntities` | Class, Record | Entity, AggregateRoot | IAuditedEntity | Persistence entities use the shared entity contract. |");
		markdown.Should().Contain("- **InheritancePolicy** `InheritancePolicy`");
	}

	[Fact]
	public void DocumentationGenerator_RendersProjectArchitectureTable()
	{
		var config = ParseConfig("""
			<ArchitecturalLevels>
			  <ProjectArchitecture requireRecognizedProjects="true" description="Solution-level reference rules.">
			    <ProjectGroup name="Presentation" description="UI projects.">
			      <Project endsWith=".Web" />
			    </ProjectGroup>
			    <ProjectGroup name="Application">
			      <Project endsWith=".Application" />
			    </ProjectGroup>
			    <AllowedProjectReference from="Presentation" to="Application" description="Presentation calls application." />
			  </ProjectArchitecture>
			</ArchitecturalLevels>
			""");

		var markdown = ArchitectureDocumentationGenerator.GenerateMarkdown(config, null);

		markdown.Should().Contain("## Project Architecture");
		markdown.Should().Contain("`requireRecognizedProjects`: `true`");
		markdown.Should().Contain("### Project Groups");
		markdown.Should().Contain("| `Presentation` | Project endsWith=\".Web\" | UI projects. |");
		markdown.Should().Contain("### Project Reference Rules");
		markdown.Should().Contain("| Allowed | `Presentation -> Application` | Presentation calls application. |");
		markdown.Should().Contain("- **ProjectArchitecture** `Project topology`");
	}

	[Fact]
	public void DocumentationGenerator_RendersPackagePolicyTable()
	{
		var config = ParseConfig("""
			<ArchitecturalLevels>
			  <ProjectArchitecture requireRecognizedProjects="true" description="Solution-level boundaries.">
			    <ProjectGroup name="Domain"><Project endsWith=".Domain" /></ProjectGroup>
			    <ProjectGroup name="Data"><Project endsWith=".Data" /></ProjectGroup>
			    <PackagePolicy projectGroup="Domain" includeTransitive="true" description="Domain stays package-light.">
			      <Allowed>
			        <Package startsWith="System." description="BCL packages are fine." />
			      </Allowed>
			      <Forbidden>
			        <Package exactName="Microsoft.Extensions.Logging" comment="Infrastructure logging belongs outside Domain." />
			      </Forbidden>
			    </PackagePolicy>
			  </ProjectArchitecture>
			</ArchitecturalLevels>
			""");

		var markdown = ArchitectureDocumentationGenerator.GenerateMarkdown(config, null);

		markdown.Should().Contain("### Package Policies");
		markdown.Should().Contain("| `Domain` | true | Allowed | `Package startsWith=\"System.\"` | BCL packages are fine. |");
		markdown.Should().Contain("| `Domain` | true | Forbidden | `Package exactName=\"Microsoft.Extensions.Logging\"` | Infrastructure logging belongs outside Domain. |");
		markdown.Should().Contain("- **PackagePolicy** `Package policy for Domain`");
		markdown.Should().Contain("- **Package** `Package exactName=\"Microsoft.Extensions.Logging\"`");
	}

	[Fact]
	public void DocumentationGenerator_RendersSourceLocationPolicies()
	{
		var config = ParseConfig("""
			<ArchitecturalLevels>
			  <Layer name="Ordering" description="Ordering boundary.">
			    <Class endsWith="Service" />
			    <SourceLocations relativeTo="Project" description="Application services stay in the ordering folder.">
			      <Source startsWith="Ordering/" assemblyName="Shop.Application" description="Implementation files." />
			      <Source startsWith="Contracts/Ordering/" description="Contract files." />
			    </SourceLocations>
			  </Layer>
			</ArchitecturalLevels>
			""");

		var markdown = ArchitectureDocumentationGenerator.GenerateMarkdown(config, null);

		markdown.Should().Contain("## Source Locations");
		markdown.Should().Contain("| `Ordering` | Project | `Source startsWith=\"Ordering/\"` | Shop.Application | Implementation files. |");
		markdown.Should().Contain("| `Ordering` | Project | `Source startsWith=\"Contracts/Ordering/\"` |  | Contract files. |");
		markdown.Should().Contain("- **SourceLocations** `Source ownership (Project)`");
	}

	[Fact]
	public void DocumentationGenerator_RendersBoundaryEntryPointPolicies()
	{
		var config = ParseConfig("""
			<ArchitecturalLevels>
			  <Layer name="Presentation">
			    <Class endsWith="Controller" />
			  </Layer>
			  <AllowedDependency from="Presentation" to="Ordering" appliesToDescendants="true" />
			  <Layer name="Ordering" description="Ordering boundary.">
			    <Namespace startsWith="CandyShop.Ordering" />
			    <EntryPoints description="Outside callers must enter through contracts.">
			      <EntryPoint layer="Contracts" description="Public ingress for other boundaries." />
			      <EntryPoint allowedSites="Constructor">
			        <Class endsWith="OrderingFacade" description="Facade-based entry for constructor injection." />
			      </EntryPoint>
			    </EntryPoints>
			    <Layer name="Contracts">
			      <Class endsWith="Contract" />
			    </Layer>
			    <Layer name="Implementation">
			      <Class endsWith="Service" />
			    </Layer>
			  </Layer>
			</ArchitecturalLevels>
			""");

		var markdown = ArchitectureDocumentationGenerator.GenerateMarkdown(config, null);

		markdown.Should().Contain("## Boundary Entry Points");
		markdown.Should().Contain("| `Ordering` | `Contracts` | All | Public ingress for other boundaries. |");
		markdown.Should().Contain("| `Ordering` | `Class endsWith=\"OrderingFacade\"` | Only Constructor | Outside callers must enter through contracts. |");
		markdown.Should().Contain("L_Ordering_Contracts[\"Contracts\\nentry\"]");
		markdown.Should().Contain("- **EntryPoints** `Boundary entry points`");
		markdown.Should().Contain("- **EntryPoint** `Entry via Contracts`");
		markdown.Should().Contain("- **EntryPoint** `Entry via matcher`");
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
