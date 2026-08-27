using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace RonSijm.AnaalIJzer.Core.Indicators.Tests.Indicators;

public sealed class ArchitectureIndicatorCoreTests
{
	[Fact]
	public void DependencySites_All_ContainsEachSupportedSiteExactlyOnce()
	{
		var result = ArchitectureDependencySites.All;

		result.Should().Equal(
			ArchitectureDependencySites.Constructor,
			ArchitectureDependencySites.Method,
			ArchitectureDependencySites.MethodReturn,
			ArchitectureDependencySites.Field,
			ArchitectureDependencySites.Property,
			ArchitectureDependencySites.Local,
			ArchitectureDependencySites.New,
			ArchitectureDependencySites.GenericInvocation,
			ArchitectureDependencySites.GenericArgument,
			ArchitectureDependencySites.Inheritance,
			ArchitectureDependencySites.InterfaceImplementation,
			ArchitectureDependencySites.Attribute,
			ArchitectureDependencySites.StaticMember);
		result.Distinct(StringComparer.Ordinal).Should().HaveCount(result.Length);
	}

	[Fact]
	public void SiteDiagnosticOptions_None_DisablesEverySite()
	{
		var result = ArchitectureSiteDiagnosticOptions.None;

		result.AnyEnabled.Should().BeFalse();
		foreach (var site in ArchitectureDependencySites.All)
		{
			result.IsEnabled(site).Should().BeFalse();
		}
	}

	[Fact]
	public void SiteDiagnosticOptions_All_EnablesEverySite()
	{
		var result = ArchitectureSiteDiagnosticOptions.All;

		result.AnyEnabled.Should().BeTrue();
		foreach (var site in ArchitectureDependencySites.All)
		{
			result.IsEnabled(site).Should().BeTrue();
		}
	}

	[Fact]
	public void SiteDiagnosticOptions_IsEnabled_UsesConfiguredPerSiteFlags()
	{
		var result = new ArchitectureSiteDiagnosticOptions(
			showMethodSiteDiagnostics: true,
			showPropertySiteDiagnostics: true,
			showStaticMemberSiteDiagnostics: true);

		result.AnyEnabled.Should().BeTrue();
		result.IsEnabled(ArchitectureDependencySites.Method).Should().BeTrue();
		result.IsEnabled(ArchitectureDependencySites.Property).Should().BeTrue();
		result.IsEnabled(ArchitectureDependencySites.StaticMember).Should().BeTrue();
		result.IsEnabled(ArchitectureDependencySites.Constructor).Should().BeFalse();
		result.IsEnabled("Unknown").Should().BeFalse();
	}

	[Fact]
	public void SiteLayerInformationOptions_None_DisablesEverySite()
	{
		var result = ArchitectureSiteLayerInformationOptions.None;

		result.AnyEnabled.Should().BeFalse();
		foreach (var site in ArchitectureDependencySites.All)
		{
			result.IsEnabled(site).Should().BeFalse();
		}
	}

	[Fact]
	public void SiteLayerInformationOptions_All_EnablesEverySite()
	{
		var result = ArchitectureSiteLayerInformationOptions.All;

		result.AnyEnabled.Should().BeTrue();
		foreach (var site in ArchitectureDependencySites.All)
		{
			result.IsEnabled(site).Should().BeTrue();
		}
	}

	[Fact]
	public void SiteLayerInformationOptions_IsEnabled_UsesConfiguredPerSiteFlags()
	{
		var result = new ArchitectureSiteLayerInformationOptions(
			showFieldLayerInformation: true,
			showLocalLayerInformation: true,
			showInterfaceImplementationLayerInformation: true);

		result.AnyEnabled.Should().BeTrue();
		result.IsEnabled(ArchitectureDependencySites.Field).Should().BeTrue();
		result.IsEnabled(ArchitectureDependencySites.Local).Should().BeTrue();
		result.IsEnabled(ArchitectureDependencySites.InterfaceImplementation).Should().BeTrue();
		result.IsEnabled(ArchitectureDependencySites.MethodReturn).Should().BeFalse();
		result.IsEnabled("Unknown").Should().BeFalse();
	}

	[Fact]
	public void LayerIndicator_NormalizesDefaultCollections_ToEmptyArrays()
	{
		var result = new ArchitectureLayerIndicator(
			new TextSpan(0, 20),
			new TextSpan(7, 6),
			"WaiterService",
			"Restaurant/Waiter",
			["Restaurant", "Restaurant/Waiter"],
			"Takes orders.",
			3);

		result.LayersThatCanCallThisLayer.Should().BeEmpty();
		result.LayersThisLayerCanCall.Should().BeEmpty();
		result.LinearCallChain.Should().BeEmpty();
		result.ExceptionReviewSummaries.Should().BeEmpty();
		result.IsInLayer.Should().BeTrue();
	}

	[Fact]
	public void LayerIndicator_PreservesPassedCollections_AndFlags()
	{
		var result = new ArchitectureLayerIndicator(
			new TextSpan(0, 20),
			new TextSpan(7, 6),
			"MysteryThing",
			"Unclassified",
			ImmutableArray<string>.Empty,
			"Not matched by any layer.",
			0,
			false,
			["Customer"],
			["Chef"],
			["Customer", "Waiter", "Chef"],
			2,
			["Review by lead architect"]);

		result.IsInLayer.Should().BeFalse();
		result.LayersThatCanCallThisLayer.Should().Equal("Customer");
		result.LayersThisLayerCanCall.Should().Equal("Chef");
		result.LinearCallChain.Should().Equal("Customer", "Waiter", "Chef");
		result.ExceptionReviewCount.Should().Be(2);
		result.ExceptionReviewSummaries.Should().Equal("Review by lead architect");
	}

	[Fact]
	public void DependencySiteIndicator_UsesTooltipAsFallbackReason()
	{
		var result = new ArchitectureDependencySiteIndicator(
			new TextSpan(4, 12),
			ArchitectureDependencySites.MethodReturn,
			"WaiterService",
			"Waiter",
			"RawIngredient",
			null,
			0,
			ArchitectureDependencySiteStatus.SiteFiltered,
			"ARCH001",
			"Raw ingredients cannot be returned to the waiter.");

		result.Reason.Should().Be("Raw ingredients cannot be returned to the waiter.");
	}

	[Fact]
	public void DependencySiteIndicator_PreservesExplicitReason()
	{
		var result = new ArchitectureDependencySiteIndicator(
			new TextSpan(4, 12),
			ArchitectureDependencySites.Field,
			"WaiterService",
			"Waiter",
			"PantryIngredient",
			"Pantry",
			5,
			ArchitectureDependencySiteStatus.Blocked,
			"ARCH001",
			"Waiters should not store pantry ingredients.",
			"blockedSites blocks Field");

		result.Reason.Should().Be("blockedSites blocks Field");
		result.DependencyLayerPath.Should().Be("Pantry");
		result.DependencyLayerPaletteSlot.Should().Be(5);
	}

	[Fact]
	public void ApiSurfaceIndicator_NormalizesDefaultSegments_AndDetectsDirectExposure()
	{
		var result = new ArchitectureApiSurfaceIndicator(
			new TextSpan(0, 6),
			"WaiterService.GetIngredient",
			"WaiterService",
			"Waiter",
			"RawIngredient",
			"Pantry",
			ArchitectureDependencySites.MethodReturn,
			"Raw ingredients may not be exposed.",
			"Use a plated DTO instead.",
			"Architecture.anl",
			12);

		result.ExposureSegments.Should().BeEmpty();
		result.IsTransitive.Should().BeFalse();
		result.DiagnosticId.Should().Be("ARCH009");
	}

	[Fact]
	public void ApiSurfaceIndicator_DetectsTransitiveExposure_WhenPathOrDepthExists()
	{
		var result = new ArchitectureApiSurfaceIndicator(
			new TextSpan(0, 6),
			"WaiterService.GetIngredient",
			"WaiterService",
			"Waiter",
			"RawIngredient",
			"Pantry",
			ArchitectureDependencySites.Property,
			"Nested raw ingredients may not be exposed.",
			null,
			"Architecture.anl",
			18,
			exposurePath: "Receipt.RawIngredient -> RawIngredient",
			exposureDepth: 1,
			exposureSegments:
			[
				new ArchitectureExposurePathSegment("Receipt.RawIngredient", @"D:\repo\Receipt.cs", new TextSpan(10, 8)),
				new ArchitectureExposurePathSegment("RawIngredient", @"D:\repo\Ingredient.cs", new TextSpan(3, 6))
			]);

		result.IsTransitive.Should().BeTrue();
		result.ExposureSegments.Should().HaveCount(2);
	}

	[Fact]
	public void ExposurePathSegment_CanNavigate_RequiresPathAndSpan()
	{
		var navigable = new ArchitectureExposurePathSegment("Receipt.RawIngredient", @"D:\repo\Receipt.cs", new TextSpan(10, 8));
		var missingPath = new ArchitectureExposurePathSegment("Receipt.RawIngredient", null, new TextSpan(10, 8));
		var missingSpan = new ArchitectureExposurePathSegment("Receipt.RawIngredient", @"D:\repo\Receipt.cs", null);

		navigable.CanNavigate.Should().BeTrue();
		missingPath.CanNavigate.Should().BeFalse();
		missingSpan.CanNavigate.Should().BeFalse();
	}

	[Fact]
	public void NameRuleIndicator_UsesStableDiagnosticId()
	{
		var result = new ArchitectureNameRuleIndicator(
			new TextSpan(0, 5),
			ArchitectureDependencySites.Property,
			"RequireNameMatchesType",
			"PatientEndpoint",
			"Asp",
			"doctorId",
			"PatientId",
			"doctorid",
			"patientid",
			"Property names must match their type.");

		result.DiagnosticId.Should().Be("ARCH008");
		result.Reason.Should().Be("Property names must match their type.");
	}

	[Fact]
	public void VisibilityPolicyIndicator_UsesStableDiagnosticId()
	{
		var result = new ArchitectureVisibilityPolicyIndicator(
			new TextSpan(0, 5),
			"PantryIngredient.SecretSauce",
			"Property",
			"Public",
			true,
			"Pantry",
			"Pantry internals must stay private.",
			"Use a projected dish instead.",
			"Architecture.anl",
			22);

		result.DiagnosticId.Should().Be("ARCH012");
		result.IsEffectivelyExternallyVisible.Should().BeTrue();
	}
}
