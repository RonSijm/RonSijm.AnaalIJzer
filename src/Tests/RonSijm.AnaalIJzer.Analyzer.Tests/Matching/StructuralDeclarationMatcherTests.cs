using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Matching;

public sealed class StructuralDeclarationMatcherTests
{
	[Fact]
	public async Task ClassMatcherWithDeclarationMatchers_CanDriveInheritancePolicy()
	{
		const string source = """
			namespace Demo.Requests
			{
				public interface IPizzaProvider { }
				public sealed class PizzaId { }
				public sealed class DrinkId { }
				public sealed class TenantId { }
				
				public sealed class GetPizzaRequest : IPizzaProvider
				{
					private readonly TenantId _tenantId = new();
					
					public PizzaId PizzaId { get; } = new();
				}
				
				public sealed class GetDrinkRequest
				{
					private readonly TenantId _tenantId = new();
					
					public DrinkId DrinkId { get; } = new();
				}
				
				public sealed class PublicPizzaRequest
				{
					public PizzaId PizzaId { get; } = new();
				}
				
				public sealed class CreatePizzaRequest
				{
					private readonly TenantId _tenantId = new();
					
					public PizzaId PizzaId { get; } = new();
				}
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="PizzaProviderRequests">
			    <Class endsWith="Request">
			      <Property exactName="PizzaId" typeName="PizzaId" />
			      <Field exactName="_tenantId" typeName="TenantId" />
			    </Class>
			    <InheritancePolicy
			      typeKinds="Class"
			      requiredInterfaces="IPizzaProvider"
			      description="Requests with both PizzaId and _tenantId expose the Pizza provider contract." />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.InheritancePolicyViolation).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertyDeclaredSymbolName].Should().Be("CreatePizzaRequest");
		violation.GetMessage().Should().Contain("requires implemented interface IPizzaProvider");
	}

	[Fact]
	public async Task ExactClassMatcherWithMissingRequiredDeclaration_FallsThroughToLaterMatcher()
	{
		const string source = """
			public sealed class PizzaRequest { }
			public sealed class Caller(PizzaRequest request) { }
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Caller">
			    <Class typeName="Caller" />
			  </Layer>
			  <Layer name="Wrong">
			    <Class typeName="PizzaRequest">
			      <Property exactName="PizzaId" typeName="PizzaId" />
			    </Class>
			  </Layer>
			  <Layer name="Correct">
			    <Class endsWith="Request" typeKind="Class" />
			  </Layer>
			  <AllowedDependency from="Caller" to="Correct" />
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task MethodDeclarationMatcherWithThrowObservation_CanDriveInheritancePolicy()
	{
		const string source = """
			using System;
			
			namespace Demo.Deliveries
			{
				public interface IPizzaFallback { }
				
				public sealed class RecoveringPizzaDeliveryService : IPizzaFallback
				{
					public void PizzaDelivery()
					{
						throw new InvalidOperationException();
					}
				}
				
				public sealed class CrashingPizzaDeliveryService
				{
					public void PizzaDelivery()
					{
						throw new InvalidOperationException();
					}
				}
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="FallbackServices">
			    <Class endsWith="Service">
			      <Method exactName="PizzaDelivery">
			        <Throw />
			      </Method>
			    </Class>
			    <InheritancePolicy
			      typeKinds="Class"
			      requiredInterfaces="IPizzaFallback"
			      description="Services whose PizzaDelivery method throws must implement the fallback contract." />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.InheritancePolicyViolation).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertyDeclaredSymbolName].Should().Be("CrashingPizzaDeliveryService");
	}

	[Fact]
	public async Task PropertyDeclarationMatcherWithThrowObservation_CanDriveInheritancePolicy()
	{
		const string source = """
			using System;
			
			namespace Demo.Catalogs
			{
				public interface IPizzaCatalogGuard { }
				public sealed class PizzaId { }
				
				public sealed class GuardedPizzaCatalog : IPizzaCatalogGuard
				{
					public PizzaId PizzaId => throw new InvalidOperationException();
				}
				
				public sealed class ExplosivePizzaCatalog
				{
					public PizzaId PizzaId => throw new InvalidOperationException();
				}
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="GuardedCatalogs">
			    <Class endsWith="Catalog">
			      <Property exactName="PizzaId">
			        <Throw typeName="InvalidOperationException" />
			      </Property>
			    </Class>
			    <InheritancePolicy
			      typeKinds="Class"
			      requiredInterfaces="IPizzaCatalogGuard"
			      description="Catalogs with a throwing PizzaId property must implement the guard contract." />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.InheritancePolicyViolation).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertyDeclaredSymbolName].Should().Be("ExplosivePizzaCatalog");
	}
}
