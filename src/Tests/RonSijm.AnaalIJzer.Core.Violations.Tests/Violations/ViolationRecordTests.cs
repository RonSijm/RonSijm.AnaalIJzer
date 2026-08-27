namespace RonSijm.AnaalIJzer.Core.Violations.Tests.Violations;

public sealed class ViolationRecordTests
{
	[Fact]
	public void Constructor_PreservesCoreDependencyViolationFields()
	{
		var result = new ViolationRecord(
			"ARCH001",
			"WaiterService",
			"Waiter",
			"PantryIngredient",
			"Pantry",
			"Waiters may not reach into the pantry directly.",
			"Ask the chef instead.");

		result.DiagnosticId.Should().Be("ARCH001");
		result.CallerTypeName.Should().Be("WaiterService");
		result.CallerLayerName.Should().Be("Waiter");
		result.DependencyTypeName.Should().Be("PantryIngredient");
		result.DepLayerName.Should().Be("Pantry");
		result.ViolationReason.Should().Be("Waiters may not reach into the pantry directly.");
		result.Comment.Should().Be("Ask the chef instead.");
		result.DeclarationTarget.Should().BeNull();
		result.SourceProjectName.Should().BeNull();
		result.CycleLayers.Should().BeNull();
	}

	[Fact]
	public void Constructor_PreservesApiSurfaceAndProjectMetadata()
	{
		var result = new ViolationRecord(
			"ARCH014",
			"OrderService",
			"Application",
			"OrderQuery",
			"QuerySurface",
			"Query surfaces may not escape the application layer.",
			null,
			declarationTarget: "MethodReturn",
			declaredAccessibility: "Public",
			apiMemberName: "OrderService.GetRawOrder",
			exposurePath: "OrderReceipt.Query -> OrderQuery",
			exposureDepth: 1,
			nestedMemberName: "OrderReceipt.Query",
			sourceProjectPath: @"D:\repo\Shop.Application\Shop.Application.csproj",
			sourceProjectName: "Shop.Application",
			sourceProjectGroup: "Application",
			targetProjectPath: @"D:\repo\Shop.Persistence\Shop.Persistence.csproj",
			targetProjectName: "Shop.Persistence",
			targetProjectGroup: "Persistence");

		result.DeclarationTarget.Should().Be("MethodReturn");
		result.DeclaredAccessibility.Should().Be("Public");
		result.ApiMemberName.Should().Be("OrderService.GetRawOrder");
		result.ExposurePath.Should().Be("OrderReceipt.Query -> OrderQuery");
		result.ExposureDepth.Should().Be(1);
		result.NestedMemberName.Should().Be("OrderReceipt.Query");
		result.SourceProjectPath.Should().EndWith("Shop.Application.csproj");
		result.SourceProjectName.Should().Be("Shop.Application");
		result.SourceProjectGroup.Should().Be("Application");
		result.TargetProjectPath.Should().EndWith("Shop.Persistence.csproj");
		result.TargetProjectName.Should().Be("Shop.Persistence");
		result.TargetProjectGroup.Should().Be("Persistence");
	}

	[Fact]
	public void Constructor_PreservesPackageSourceLocationBoundaryAndCycleMetadata()
	{
		var result = new ViolationRecord(
			"ARCH018",
			"Ordering -> Notifications",
			"Ordering",
			"Ordering -> Notifications -> Ordering",
			string.Empty,
			"Observed cycle",
			null,
			packageId: "Microsoft.Extensions.Logging",
			packageVersion: "9.0.0",
			packageReferenceKind: "Direct",
			sourceFilePath: @"D:\repo\Shop\Application\Ordering\OrderService.cs",
			normalizedSourcePath: "Application/Ordering/OrderService.cs",
			sourceAssemblyName: "Shop.Application",
			boundaryLayerName: "Ordering",
			matchedEntryPoint: "Ordering/Contracts",
			cycleLayers: "Ordering|Notifications",
			cycleLength: 2,
			observedSites: "Constructor, Method",
			cycleScope: "Project");

		result.PackageId.Should().Be("Microsoft.Extensions.Logging");
		result.PackageVersion.Should().Be("9.0.0");
		result.PackageReferenceKind.Should().Be("Direct");
		result.SourceFilePath.Should().EndWith("OrderService.cs");
		result.NormalizedSourcePath.Should().Be("Application/Ordering/OrderService.cs");
		result.SourceAssemblyName.Should().Be("Shop.Application");
		result.BoundaryLayerName.Should().Be("Ordering");
		result.MatchedEntryPoint.Should().Be("Ordering/Contracts");
		result.CycleLayers.Should().Be("Ordering|Notifications");
		result.CycleLength.Should().Be(2);
		result.ObservedSites.Should().Be("Constructor, Method");
		result.CycleScope.Should().Be("Project");
	}
}
