using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

public sealed class CycleDependencyCodeFixTests
{
	private const string Configuration = """
		<ArchitecturalLevels enforceAcyclic="true">
		  <Layer name="Ordering"><Class typeName="OrderingService" /></Layer>
		  <Layer name="Inventory"><Class typeName="InventoryService" /></Layer>
		  <Layer name="Billing"><Class typeName="BillingService" /></Layer>
		  <AllowedDependency from="Ordering" to="Inventory" />
		  <AllowedDependency from="Inventory" to="Billing" />
		  <AllowedDependency from="Billing" to="Ordering" />
		</ArchitecturalLevels>
		""";

	private const string Source = """
		public sealed class OrderingService;
		public sealed class InventoryService;
		public sealed class BillingService;
		""";

	[Fact]
	public async Task ConfiguredCycle_OffersAUserSelectedBlockOrRemovalForEveryCycleEdge()
	{
		var titles = await AnalyzerTestHelper.GetCodeFixTitlesAsync(Source, Configuration, ArchitecturalDiagnosticIds.CyclicDependencyGraph);

		titles.Should().Contain("Break configured cycle by blocking 'Ordering' -> 'Inventory'");
		titles.Should().Contain("Break configured cycle by removing allowed dependency 'Ordering' -> 'Inventory'");
		titles.Should().Contain("Break configured cycle by blocking 'Inventory' -> 'Billing'");
		titles.Should().Contain("Break configured cycle by removing allowed dependency 'Inventory' -> 'Billing'");
		titles.Should().Contain("Break configured cycle by blocking 'Billing' -> 'Ordering'");
		titles.Should().Contain("Break configured cycle by removing allowed dependency 'Billing' -> 'Ordering'");
	}

	[Fact]
	public async Task ConfiguredCycle_BlockProposal_AddsBlockingDependency()
	{
		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			Source,
			Configuration,
			ArchitecturalDiagnosticIds.CyclicDependencyGraph,
			"Break configured cycle by blocking 'Ordering' -> 'Inventory'");

		updatedConfig.Should().Contain("<BlockedDependency from=\"Ordering\" to=\"Inventory\" />");
	}

	[Fact]
	public async Task ConfiguredCycle_RemoveProposal_RemovesOnlyTheSelectedAllowedDependency()
	{
		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			Source,
			Configuration,
			ArchitecturalDiagnosticIds.CyclicDependencyGraph,
			"Break configured cycle by removing allowed dependency 'Ordering' -> 'Inventory'");

		updatedConfig.Should().NotContain("<AllowedDependency from=\"Ordering\" to=\"Inventory\" />");
		updatedConfig.Should().Contain("<AllowedDependency from=\"Inventory\" to=\"Billing\" />");
	}

	[Fact]
	public async Task ConfiguredCycle_IncludedRule_UpdatesTheOwningConfigurationFile()
	{
		const string rootConfiguration = """
			<ArchitecturalLevels enforceAcyclic="true">
			  <Include path="cycle-rules.anl" />
			</ArchitecturalLevels>
			""";
		const string includedConfiguration = """
			<ArchitecturalLevels>
			  <Layer name="Ordering"><Class typeName="OrderingService" /></Layer>
			  <Layer name="Inventory"><Class typeName="InventoryService" /></Layer>
			  <Layer name="Billing"><Class typeName="BillingService" /></Layer>
			  <AllowedDependency from="Ordering" to="Inventory" />
			  <AllowedDependency from="Inventory" to="Billing" />
			  <AllowedDependency from="Billing" to="Ordering" />
			</ArchitecturalLevels>
			""";

		var updatedConfiguration = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			Source,
			[("Architecture.anl", rootConfiguration), ("cycle-rules.anl", includedConfiguration)],
			ArchitecturalDiagnosticIds.CyclicDependencyGraph,
			"Break configured cycle by blocking 'Ordering' -> 'Inventory'",
			"cycle-rules.anl");

		updatedConfiguration.Should().Contain("<BlockedDependency from=\"Ordering\" to=\"Inventory\" />");
	}

	[Fact]
	public async Task ConfiguredCycle_InlineSettings_UpdatesAssemblyMetadata()
	{
		var source = """"
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", """
			<ArchitecturalLevels enforceAcyclic="true">
			  <Layer name="Ordering"><Class typeName="OrderingService" /></Layer>
			  <Layer name="Inventory"><Class typeName="InventoryService" /></Layer>
			  <Layer name="Billing"><Class typeName="BillingService" /></Layer>
			  <AllowedDependency from="Ordering" to="Inventory" />
			  <AllowedDependency from="Inventory" to="Billing" />
			  <AllowedDependency from="Billing" to="Ordering" />
			</ArchitecturalLevels>
			""")]

			public sealed class OrderingService;
			public sealed class InventoryService;
			public sealed class BillingService;
			"""";

		var updatedSource = await AnalyzerTestHelper.ApplyCodeFixAsync(
			source,
			ArchitecturalDiagnosticIds.CyclicDependencyGraph,
			"Break configured cycle by blocking 'Ordering' -> 'Inventory'");

		updatedSource.Should().Contain("<BlockedDependency from=\"Ordering\" to=\"Inventory\" />");
		updatedSource.Should().Contain("AssemblyMetadata(\"AnaalIJzerSettings\"");
	}
}
