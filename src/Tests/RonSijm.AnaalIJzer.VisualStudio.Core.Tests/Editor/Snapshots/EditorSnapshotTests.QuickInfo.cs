using RonSijm.AnaalIJzer.Graph;
using RonSijm.AnaalIJzer.QuickInfo;
using RonSijm.AnaalIJzer.Snapshots;

namespace RonSijm.AnaalIJzer.VisualStudio.Core.Tests.Editor.Snapshots;
public sealed partial class EditorSnapshotTests
{
	[Fact]
	public async Task QuickInfoContentBuilder_CreatesLayerContent()
	{
		const string source = "public class PizzaController { }";
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Controller" description="Receives requests."><Class endsWith="Controller" /></Layer>
		                        <Layer name="Application"><Class endsWith="Service" /></Layer>
		                        <AllowedDependency from="Controller" to="Application" />
		                        <AllowedDependency from="Application" to="Controller" allowedSites="Method" />
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);
		var content = ArchitectureQuickInfoContentBuilder.CreateLayerContent(snapshot.LayerIndicators.Single());

		content.Title.Should().Be("AnaalIJzer layer");
		content.Lines.Should().Contain("Type: PizzaController");
		content.Lines.Should().Contain("Layer: Controller");
		content.Lines.Should().Contain("Description: Receives requests.");
		content.Lines.Should().Contain("Can be called by: Application");
		content.Lines.Should().Contain("Can call: Application");
		content.ToString().Should().Contain("Palette slot: AnaalIJzer Layer 01");
	}

	[Fact]
	public async Task QuickInfoContentBuilder_HidesGlobalLayerRulesUnlessEnabled()
	{
		const string source = "public class WaiterType { }";
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Waiter"><Class typeName="WaiterType" /></Layer>
		                        <AllowedDependency from="*" to="Waiter" />
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);
		var indicator = snapshot.LayerIndicators.Single();
		var defaultContent = ArchitectureQuickInfoContentBuilder.CreateLayerContent(indicator);
		var explicitContent = ArchitectureQuickInfoContentBuilder.CreateLayerContent(
			indicator,
			new ArchitectureEditorOptions(showGlobalLayerRulesInBadges: true));

		defaultContent.Lines.Should().Contain("Can be called by: none configured");
		defaultContent.ToString().Should().NotContain("* (any layer)");
		explicitContent.Lines.Should().Contain("Can be called by: * (any layer)");
	}

	[Fact]
	public async Task QuickInfoContentBuilder_ShowsLinearCallChainWhenGraphHasNoForks()
	{
		const string source = "public class WaiterType { }";
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Customer"><Class typeName="CustomerType" /></Layer>
		                        <Layer name="Waiter"><Class typeName="WaiterType" /></Layer>
		                        <Layer name="Chef"><Class typeName="ChefType" /></Layer>
		                        <AllowedDependency from="Customer" to="Waiter" />
		                        <AllowedDependency from="Waiter" to="Chef" />
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);
		var content = ArchitectureQuickInfoContentBuilder.CreateLayerContent(snapshot.LayerIndicators.Single());

		content.Lines.Should().Contain("Call chain: Customer -> Waiter -> Chef");
	}

	[Fact]
	public async Task QuickInfoContentBuilder_DoesNotShowLinearCallChainForForkedGraph()
	{
		const string source = "public class WaiterType { }";
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Customer"><Class typeName="CustomerType" /></Layer>
		                        <Layer name="Waiter"><Class typeName="WaiterType" /></Layer>
		                        <Layer name="Chef"><Class typeName="ChefType" /></Layer>
		                        <Layer name="Cashier"><Class typeName="CashierType" /></Layer>
		                        <AllowedDependency from="Customer" to="Waiter" />
		                        <AllowedDependency from="Waiter" to="Chef" />
		                        <AllowedDependency from="Waiter" to="Cashier" />
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);
		var content = ArchitectureQuickInfoContentBuilder.CreateLayerContent(snapshot.LayerIndicators.Single());

		content.ToString().Should().NotContain("Call chain:");
	}

	[Fact]
	public async Task Snapshot_CapturesUnclassifiedTypesSeparatelyForOptionalBadges()
	{
		const string source = """
		                      public class PizzaController { }
		                      public class UnassignedHelper { }
		                      """;
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Controller"><Class endsWith="Controller" /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);
		var unclassified = snapshot.UnclassifiedTypeIndicators.Should().ContainSingle().Subject;

		snapshot.LayerIndicators.Should().ContainSingle(indicator => indicator.LayerPath == "Controller");
		unclassified.TypeName.Should().Be("UnassignedHelper");
		unclassified.IsInLayer.Should().BeFalse();
		unclassified.LayerPath.Should().Be("not in layer");
		ArchitectureQuickInfoContentBuilder.CreateLayerContent(unclassified)
			.Lines.Should().Contain("Reason: This type is not assigned to any configured AnaalIJzer layer.");
	}

	[Fact]
	public async Task QuickInfoContentBuilder_CreatesSiteContent()
	{
		const string source = """
		                      public class CallerType
		                      {
		                          public CallerType(TargetDependency dependency) { }
		                      }

		                      public class TargetDependency { }
		                      """;
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                        <Layer name="Dependency"><Class typeName="TargetDependency" /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);
		var content = ArchitectureQuickInfoContentBuilder.CreateSiteContent(snapshot.SiteIndicators.Single());

		content.Title.Should().Be("AnaalIJzer dependency site");
		content.Lines.Should().Contain("Site: Constructor");
		content.Lines.Should().Contain("Caller: CallerType (Caller)");
		content.Lines.Should().Contain("Dependency: TargetDependency (Dependency)");
		content.Lines.Should().Contain("Diagnostic: ARCH001");
		content.Lines.Should().Contain(line => line.StartsWith("Reason:", StringComparison.Ordinal));
	}

	[Fact]
	public async Task SnapshotWithExceptionMatcherConfig_ClassifiesExemptedTypeCorrectly()
	{
		// Exercises ExceptionMatcher — a type added after 0.1.4.0 that triggered a
		// TypeLoadException when VS loaded the old NuGet DLL instead of the VSIX copy.
		const string source = """
		                      public class LegacyPatientStore { }
		                      public class PatientRepository { }
		                      """;
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Data"><Class endsWith="Repository" /></Layer>
		                        <Forbidden>
		                          <Class endsWith="Store">
		                            <Exceptions>
		                              <Class startsWith="Legacy" />
		                            </Exceptions>
		                          </Class>
		                        </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		var snapshot = await CreateSnapshotAsync(source, config);

		snapshot.HasConfiguration.Should().BeTrue();
		snapshot.HasConfigurationIssues.Should().BeFalse();
		snapshot.LayerIndicators.Should().ContainSingle(i => i.LayerPath == "Data");
	}

	[Fact]
	public void EditorOptions_DefaultsMatchVisualStudioPlan()
	{
		var options = ArchitectureEditorOptions.Default;

		options.EnableInlineLayerBadges.Should().BeTrue();
		options.EnableLayerGlyphs.Should().BeTrue();
		options.EnableLayerBlockHighlight.Should().BeTrue();
		options.EnableLayerBackgroundTint.Should().BeTrue();
		options.EnableHighlightCodeInLayer.Should().BeTrue();
		options.EnableLayerTextBackgroundTint.Should().BeFalse();
		options.ShowLayerBadgesWhenNotInLayer.Should().BeFalse();
		options.ShowGlobalLayerRulesInBadges.Should().BeFalse();
		options.ShowLinearCallChainInBadges.Should().BeTrue();
		options.EnableSitesDiagnostics.Should().BeFalse();
		options.SiteDiagnostics.AnyEnabled.Should().BeFalse();
		options.EnableSiteLayerInformation.Should().BeFalse();
		options.SiteLayerInformation.AnyEnabled.Should().BeFalse();
		options.DependencyGraphFocusMode.Should().Be(ArchitectureGraphFocusMode.HighlightCurrent);
	}
}
