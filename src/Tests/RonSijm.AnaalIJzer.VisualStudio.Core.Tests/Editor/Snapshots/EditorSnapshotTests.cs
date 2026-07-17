using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Indicators;
using RonSijm.AnaalIJzer.Snapshots;

namespace RonSijm.AnaalIJzer.VisualStudio.Core.Tests.Editor.Snapshots;

public sealed partial class EditorSnapshotTests
{
	private static readonly MetadataReference[] BasicReferences =
		CreateBasicReferences();

	[Fact]
	public async Task SnapshotWithoutConfig_ReturnsEmptySnapshot()
	{
		const string source = "public class PizzaController { }";

		var snapshot = await CreateSnapshotAsync(source);

		snapshot.HasConfiguration.Should().BeFalse();
		snapshot.HasConfigurationIssues.Should().BeFalse();
		snapshot.LayerIndicators.Should().BeEmpty();
		snapshot.SiteIndicators.Should().BeEmpty();
	}

	[Fact]
	public async Task SnapshotWithInvalidConfig_ReturnsConfigurationIssuesWithoutVisuals()
	{
		const string source = "public class PizzaController { }";
		const string config = "<ArchitecturalLevels><Layer name=\"Broken\"><Class /></Layer></ArchitecturalLevels>";

		var snapshot = await CreateSnapshotAsync(source, config);

		snapshot.HasConfiguration.Should().BeTrue();
		snapshot.HasConfigurationIssues.Should().BeTrue();
		snapshot.LayerIndicators.Should().BeEmpty();
		snapshot.SiteIndicators.Should().BeEmpty();
		snapshot.ConfigurationIssueMessages.Should().NotBeEmpty();
	}

	[Fact]
	public async Task SnapshotWithPartiallyParsedInvalidConfig_ReturnsNoVisuals()
	{
		const string source = "public class PizzaController { }";
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Controller"><Class endsWith="Controller" /></Layer>
		                        <Layer name="Broken"><Class /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);

		snapshot.HasConfiguration.Should().BeTrue();
		snapshot.HasConfigurationIssues.Should().BeTrue();
		snapshot.LayerIndicators.Should().BeEmpty();
		snapshot.SiteIndicators.Should().BeEmpty();
	}

	[Fact]
	public async Task XmlConfig_RendersLayerIndicator()
	{
		const string source = "namespace Demo; public class PizzaController { }";
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Controller" description="Talks to the application layer.">
		                          <Class endsWith="Controller" />
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);
		var indicator = snapshot.LayerIndicators.Should().ContainSingle().Subject;

		indicator.TypeName.Should().Be("PizzaController");
		indicator.LayerPath.Should().Be("Controller");
		indicator.LayerAncestry.Should().Equal("Controller");
		indicator.Description.Should().Be("Talks to the application layer.");
		indicator.PaletteSlot.Should().Be(1);
	}

	[Fact]
	public async Task XmlConfig_RendersDependencyGraphSnapshot()
	{
		const string source = """
		                      public class CustomerType { }
		                      public class WaiterType { }
		                      public class ChefType { }
		                      """;
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Customer" description="Orders food."><Class typeName="CustomerType" /></Layer>
		                        <Layer name="Waiter" description="Talks to customers."><Class typeName="WaiterType" /></Layer>
		                        <Layer name="Chef"><Class typeName="ChefType" /></Layer>
		                        <AllowedDependency from="Customer" to="Waiter" />
		                        <AllowedDependency from="Waiter" to="Chef" allowedSites="Constructor" appliesToDescendants="true" />
		                        <BlockedDependency from="Chef" to="Customer" blockedSites="Method" />
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);

		snapshot.GraphSnapshot.HasConfiguration.Should().BeTrue();
		snapshot.GraphSnapshot.Layers.Select(layer => layer.Path).Should().Equal("Customer", "Waiter", "Chef");
		snapshot.GraphSnapshot.Layers[0].Description.Should().Be("Orders food.");
		snapshot.GraphSnapshot.ActiveLayerPaths.Should().Equal("Customer", "Waiter", "Chef");
		snapshot.GraphSnapshot.Rules.Should().HaveCount(3);
		snapshot.GraphSnapshot.Rules.Should().Contain(rule => rule.From == "Waiter" && rule.To == "Chef" && rule.SiteText == "allowed sites: Constructor" && rule.AppliesToDescendants);
		snapshot.GraphSnapshot.Rules.Should().Contain(rule => rule.Kind == "BlockedDependency" && rule.SiteText == "blocked sites: Method");
	}

	[Fact]
	public async Task ProjectEvidence_AddsTypesAndObservedDependencyViolationsToGraphSnapshot()
	{
		const string source = """
		                      public class CustomerType
		                      {
		                          public CustomerType(WaiterType waiter, ChefType chef) { }
		                      }

		                      public class WaiterType { }
		                      public class ChefType { }
		                      """;
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Customer"><Class typeName="CustomerType" /></Layer>
		                        <Layer name="Waiter"><Class typeName="WaiterType" /></Layer>
		                        <Layer name="Chef"><Class typeName="ChefType" /></Layer>
		                        <AllowedDependency from="Customer" to="Waiter" />
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config, includeProjectEvidence: true);

		snapshot.GraphSnapshot.Evidence.Types.Select(type => type.LayerPath).Should().Equal("Customer", "Waiter", "Chef");
		snapshot.GraphSnapshot.Evidence.Dependencies.Should().Contain(dependency =>
			dependency.CallerLayerPath == "Customer"
			&& dependency.DependencyLayerPath == "Waiter"
			&& dependency.DiagnosticId == null);
		snapshot.GraphSnapshot.Evidence.Dependencies.Should().Contain(dependency =>
			dependency.CallerLayerPath == "Customer"
			&& dependency.DependencyLayerPath == "Chef"
			&& dependency.DiagnosticId == ArchitecturalDiagnosticIds.IllegalLevelDependency);
	}

	[Fact]
	public async Task InlineMetadataConfig_RendersLayerIndicator()
	{
		const string source = """"
		                      using System.Reflection;

		                      [assembly: AssemblyMetadata("AnaalIJzerSettings", $"""
		                      <ArchitecturalLevels>
		                        <Layer name="Controller">
		                          <Class endsWith="Controller" />
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """)]

		                      public class PizzaController { }
		                      """";

		var snapshot = await CreateSnapshotAsync(source);

		snapshot.HasConfiguration.Should().BeTrue();
		snapshot.LayerIndicators.Should().ContainSingle()
			.Which.LayerPath.Should().Be("Controller");
	}

	[Fact]
	public async Task FileConfig_TakesPrecedenceOverInlineMetadataConfig()
	{
		const string source = """"
		                      using System.Reflection;

		                      [assembly: AssemblyMetadata("AnaalIJzerSettings", $"""
		                      <ArchitecturalLevels>
		                        <Layer name="InlineController">
		                          <Class endsWith="Controller" />
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """)]

		                      public class PizzaController { }
		                      """";
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="FileController">
		                          <Class endsWith="Controller" />
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);

		snapshot.LayerIndicators.Should().ContainSingle()
			.Which.LayerPath.Should().Be("FileController");
	}

	[Fact]
	public async Task GeneratedFile_ReturnsEmptySnapshot()
	{
		const string source = "public class PizzaController { }";
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Controller"><Class endsWith="Controller" /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config, "PizzaController.g.cs");

		snapshot.Should().BeSameAs(ArchitectureEditorSnapshot.Empty);
	}

	[Fact]
	public async Task NestedLayers_UseCanonicalPathAndDeterministicPaletteSlot()
	{
		const string source = """
		                      namespace Restaurant.Ordering;

		                      public class PizzaOrderService { }
		                      public class PizzaOrderRepository { }
		                      """;
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Ordering">
		                          <Namespace startsWith="Restaurant.Ordering" />
		                          <Layer name="Application">
		                            <Class endsWith="Service" />
		                          </Layer>
		                          <Layer name="Repository">
		                            <Class endsWith="Repository" />
		                          </Layer>
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);

		snapshot.LayerIndicators.Select(indicator => indicator.LayerPath).Should().Equal("Ordering/Application", "Ordering/Repository");
		snapshot.LayerIndicators.Select(indicator => indicator.PaletteSlot).Should().Equal(2, 3);
		snapshot.LayerIndicators[0].LayerAncestry.Should().Equal("Ordering", "Ordering/Application");
	}

	[Fact]
	public async Task RepeatedChildNames_KeepDistinctCanonicalPaletteSlots()
	{
		const string source = """
		                      namespace Restaurant.Ordering
		                      {
		                          public class OrderingService { }
		                      }

		                      namespace Restaurant.Billing
		                      {
		                          public class BillingService { }
		                      }
		                      """;
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Ordering">
		                          <Namespace startsWith="Restaurant.Ordering" />
		                          <Layer name="Application"><Class endsWith="Service" /></Layer>
		                        </Layer>
		                        <Layer name="Billing">
		                          <Namespace startsWith="Restaurant.Billing" />
		                          <Layer name="Application"><Class endsWith="Service" /></Layer>
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);

		snapshot.LayerIndicators.Select(indicator => indicator.LayerPath).Should().Equal("Ordering/Application", "Billing/Application");
		snapshot.LayerIndicators.Select(indicator => indicator.PaletteSlot).Should().Equal(2, 4);
	}

	[Fact]
	public async Task CompositeTypeKindMatchers_ClassifyInterfaceAndImplementationSeparately()
	{
		const string source = """
		                      public interface IExampleRepository { }
		                      public class ExampleRepository : IExampleRepository { }
		                      """;
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="DataContracts">
		                          <Class startsWith="I" endsWith="Repository" typeKind="Interface" />
		                        </Layer>
		                        <Layer name="DataImplementation">
		                          <Class endsWith="Repository" typeKind="Class" />
		                        </Layer>
		                        <AllowedDependency from="DataImplementation" to="DataContracts" allowedSites="InterfaceImplementation" />
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);

		snapshot.LayerIndicators.Select(indicator => indicator.LayerPath).Should().Equal("DataContracts", "DataImplementation");
		snapshot.SiteIndicators.Should().ContainSingle(indicator => indicator.Site == ArchitectureDependencySites.InterfaceImplementation)
			.Which.Status.Should().Be(ArchitectureDependencySiteStatus.Allowed);
	}

	[Fact]
	public async Task PartialTypes_RenderOneLayerIndicatorPerDeclaration()
	{
		const string source = """
		                      public partial class PizzaController { }
		                      public partial class PizzaController { }
		                      """;
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Controller"><Class typeName="PizzaController" /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);

		snapshot.LayerIndicators.Should().HaveCount(2);
		snapshot.LayerIndicators.Should().OnlyContain(indicator => indicator.LayerPath == "Controller");
	}

}
