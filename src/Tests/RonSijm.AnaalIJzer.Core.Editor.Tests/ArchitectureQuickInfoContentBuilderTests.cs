using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Core.Editor.QuickInfo;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;
using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.Core.Editor.Tests;

public sealed class ArchitectureQuickInfoContentBuilderTests
{
	[Fact]
	public void CreateLayerContent_FiltersGlobalRules_WhenDisabled()
	{
		var indicator = new ArchitectureLayerIndicator(
			new TextSpan(0, 20),
			new TextSpan(7, 6),
			"WaiterService",
			"Waiter",
			["Restaurant", "Waiter"],
			"Takes orders from customers.",
			3,
			true,
			["*", "Customer"],
			["Chef", "* Framework"],
			["Customer", "Waiter", "Chef"]);
		var options = new ArchitectureEditorOptions(
			showGlobalLayerRulesInBadges: false,
			showLinearCallChainInBadges: true);

		var content = ArchitectureQuickInfoContentBuilder.CreateLayerContent(indicator, options);

		content.Lines.Should().Contain("Call chain: Customer -> Waiter -> Chef");
		content.Lines.Should().Contain("Can be called by: Customer");
		content.Lines.Should().Contain("Can call: Chef");
		content.Lines.Should().NotContain(line => line.Contains("*", StringComparison.Ordinal));
	}

	[Fact]
	public void CreateLayerContent_ForUnclassifiedType_ShowsReasonWithoutConfiguredCallLists()
	{
		var indicator = new ArchitectureLayerIndicator(
			new TextSpan(0, 18),
			new TextSpan(6, 6),
			"MysteryThing",
			"Unclassified",
			ImmutableArray<string>.Empty,
			"Not matched by any layer.",
			0,
			false);

		var content = ArchitectureQuickInfoContentBuilder.CreateLayerContent(indicator);

		content.Lines.Should().Contain("Type: MysteryThing");
		content.Lines.Should().Contain("Layer: Unclassified");
		content.Lines.Should().Contain("Reason: Not matched by any layer.");
		content.Lines.Should().NotContain(line => line.StartsWith("Can call:", StringComparison.Ordinal));
	}

	[Fact]
	public void CreateSiteContent_UsesUnclassifiedLabel_ForUnknownDependencyLayer()
	{
		var indicator = new ArchitectureDependencySiteIndicator(
			new TextSpan(0, 5),
			ArchitectureDependencySites.MethodReturn,
			"WaiterService",
			"Waiter",
			"RawIngredient",
			null,
			0,
			ArchitectureDependencySiteStatus.MissingAllowedDependency,
			"ARCH001",
			"Raw ingredients cannot be returned to the waiter.");

		var content = ArchitectureQuickInfoContentBuilder.CreateSiteContent(indicator);

		content.Title.Should().Be("AnaalIJzer dependency site");
		content.Lines.Should().Contain("Dependency: RawIngredient (unclassified)");
		content.Lines.Should().Contain("Diagnostic: ARCH001");
	}

	[Fact]
	public void CreateSiteContent_ExplainsForbiddenOperationPolicy()
	{
		var indicator = new ArchitectureDependencySiteIndicator(
			new TextSpan(0, 16),
			ArchitectureDependencySites.StaticMember,
			"PizzaKitchen",
			"Kitchen",
			"System.DateTime.UtcNow",
			null,
			0,
			ArchitectureDependencySiteStatus.TypePolicyViolation,
			"ARCH021",
			"The kitchen may not read the system clock.",
			"the ForbiddenOperations policy in layer 'Kitchen' blocks System.DateTime.UtcNow at StaticMember");

		var content = ArchitectureQuickInfoContentBuilder.CreateSiteContent(indicator);

		content.Title.Should().Be("AnaalIJzer forbidden operation");
		content.Lines.Should().Contain("Operation: System.DateTime.UtcNow");
		content.Lines.Should().Contain("Diagnostic: ARCH021");
		content.Lines.Should().Contain("Reason: the ForbiddenOperations policy in layer 'Kitchen' blocks System.DateTime.UtcNow at StaticMember");
	}

	[Fact]
	public void CreateSiteContent_ExplainsBehavioralOperationPolicy()
	{
		var indicator = new ArchitectureDependencySiteIndicator(
			new TextSpan(0, 16),
			ArchitectureDependencySites.Method,
			"PizzaKitchen",
			"Kitchen",
			"RequiredOperationBefore",
			null,
			0,
			ArchitectureDependencySiteStatus.TypePolicyViolation,
			"ARCH022",
			"The kitchen must validate before saving a pizza.",
			"the BehavioralOperations policy in layer 'Kitchen' requires PizzaValidator.Validate before PizzaRepository.Save in declaration 'PizzaKitchen.Submit()'");

		var content = ArchitectureQuickInfoContentBuilder.CreateSiteContent(indicator);

		content.Title.Should().Be("AnaalIJzer behavioral operation");
		content.Lines.Should().Contain("Behavioral policy: RequiredOperationBefore");
		content.Lines.Should().Contain("Diagnostic: ARCH022");
		content.Lines.Should().Contain("Reason: the BehavioralOperations policy in layer 'Kitchen' requires PizzaValidator.Validate before PizzaRepository.Save in declaration 'PizzaKitchen.Submit()'");
	}

	[Fact]
	public void CreateSiteContent_ExplainsOperationContractViolation()
	{
		var indicator = new ArchitectureDependencySiteIndicator(
			new TextSpan(0, 16),
			ArchitectureDependencySites.Method,
			"PizzaOrderController",
			"Controller",
			"PlacePizzaOrder",
			null,
			0,
			ArchitectureDependencySiteStatus.TypePolicyViolation,
			"ARCH023",
			"The entry-point declaration does not directly invoke the configured owner.");

		var content = ArchitectureQuickInfoContentBuilder.CreateSiteContent(indicator);

		content.Title.Should().Be("AnaalIJzer operation contract");
		content.Lines.Should().Contain("Operation contract: PlacePizzaOrder");
		content.Lines.Should().Contain("Diagnostic: ARCH023");
	}

	[Fact]
	public void ArchitectureEditorOptions_DerivesFeatureFlags_FromPassedSiteOptions()
	{
		var options = new ArchitectureEditorOptions(
			enableSitesDiagnostics: false,
			siteDiagnostics: new ArchitectureSiteDiagnosticOptions(showMethodSiteDiagnostics: true),
			siteLayerInformation: new ArchitectureSiteLayerInformationOptions(showFieldLayerInformation: true));

		options.EnableSitesDiagnostics.Should().BeTrue();
		options.EnableSiteLayerInformation.Should().BeTrue();
		options.IsSiteDiagnosticEnabled(ArchitectureDependencySites.Method).Should().BeTrue();
		options.IsSiteDiagnosticEnabled(ArchitectureDependencySites.Property).Should().BeFalse();
		options.IsSiteLayerInformationEnabled(ArchitectureDependencySites.Field).Should().BeTrue();
		options.IsSiteLayerInformationEnabled(ArchitectureDependencySites.Local).Should().BeFalse();
	}
}
