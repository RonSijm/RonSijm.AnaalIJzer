using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

public sealed class NameRuleAllowMappingCodeFixTests
{
	[Fact]
	public async Task RequireMatchingNames_AddsAllowMappingToConfiguration()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <NameRules>
			      <RequireMatchingNames>
			        <Name endsWith="Id" />
			      </RequireMatchingNames>
			    </NameRules>
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public class OrderService
			{
			    public void Run(int legacyCustomerId)
			    {
			        Save(legacyCustomerId);
			    }

			    private void Save(int customerId)
			    {
			    }
			}
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.NameRuleViolation,
			"Add <Allow from=\"legacy.customer.id\" to=\"customer.id\" /> to name rule");

		updatedConfig.Should().Contain("<Allow from=\"legacy.customer.id\" to=\"customer.id\" />");
	}

	[Fact]
	public async Task RequireMatchingNames_OffersSiteScopedAllowMapping()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <NameRules>
			      <RequireMatchingNames>
			        <Name endsWith="Id" />
			      </RequireMatchingNames>
			    </NameRules>
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public class OrderService
			{
			    public void Run(int legacyCustomerId)
			    {
			        Save(legacyCustomerId);
			    }

			    private void Save(int customerId)
			    {
			    }
			}
			""";

		var titles = await AnalyzerTestHelper.GetCodeFixTitlesAsync(source, config, ArchitecturalDiagnosticIds.NameRuleViolation);

		titles.Should().Contain("Add <Allow from=\"legacy.customer.id\" to=\"customer.id\" /> to name rule");
		titles.Should().Contain("Add site-scoped <Allow from=\"legacy.customer.id\" to=\"customer.id\" /> for Method");
	}

	[Fact]
	public async Task RequireMatchingNames_InlineSettings_AddsAllowMappingToAssemblyMetadata()
	{
		const string source = """"
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <NameRules>
			      <RequireMatchingNames>
			        <Name endsWith="Id" />
			      </RequireMatchingNames>
			    </NameRules>
			  </Layer>
			</ArchitecturalLevels>
			""")]

			public class OrderService
			{
			    public void Run(int legacyCustomerId)
			    {
			        Save(legacyCustomerId);
			    }

			    private void Save(int customerId)
			    {
			    }
			}
			"""";

		var updatedSource = await AnalyzerTestHelper.ApplyCodeFixAsync(
			source,
			ArchitecturalDiagnosticIds.NameRuleViolation,
			"Add <Allow from=\"legacy.customer.id\" to=\"customer.id\" /> to name rule");

		updatedSource.Should().Contain("<Allow from=\"legacy.customer.id\" to=\"customer.id\" />");
	}
}
