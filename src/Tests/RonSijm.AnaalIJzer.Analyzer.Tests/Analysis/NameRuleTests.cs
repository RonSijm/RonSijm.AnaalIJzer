using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis;

public sealed class NameRuleTests
{
	[Fact]
	public async Task RequireMatchingNames_ReportsSwappedMethodArguments()
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
		                          public void Initialize()
		                          {
		                              var fruitId = 1;
		                              var animalId = 2;

		                              Log(animalId, fruitId);
		                          }

		                          public void Log(int fruitId, int animalId)
		                          {
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var nameRuleDiagnostics = diagnostics.Where(d => d.Id == ArchitecturalDiagnosticIds.NameRuleViolation).ToArray();
		nameRuleDiagnostics.Should().HaveCount(2);
		nameRuleDiagnostics.Should().OnlyContain(d => d.Properties["Site"] == "Method");
		nameRuleDiagnostics.Select(d => d.Properties["SourceName"]).Should().BeEquivalentTo("animalId", "fruitId");
		nameRuleDiagnostics.Select(d => d.Properties["TargetName"]).Should().BeEquivalentTo("fruitId", "animalId");
	}

	[Fact]
	public async Task RequireMatchingNames_AllowsMatchingPropertyAssignment()
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
		                      public sealed class Customer
		                      {
		                          public int Id { get; set; }
		                      }

		                      public class OrderService
		                      {
		                          public void Assign(Customer customer, int customerId)
		                          {
		                              customer.Id = customerId;
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Where(d => d.Id == ArchitecturalDiagnosticIds.NameRuleViolation).Should().BeEmpty();
	}

	[Fact]
	public async Task RequireMatchingNames_ReportsMismatchedPropertyAssignment()
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
		                      public sealed class Customer
		                      {
		                          public int Id { get; set; }
		                      }

		                      public class OrderService
		                      {
		                          public void Assign(Customer customer, int animalId)
		                          {
		                              customer.Id = animalId;
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.NameRuleViolation).Which;
		diagnostic.Properties["Site"].Should().Be("Property");
		diagnostic.Properties["SourceName"].Should().Be("animalId");
		diagnostic.Properties["TargetName"].Should().Be("Customer.Id");
	}

	[Fact]
	public async Task RequireMatchingNames_AllowMapping_CanPermitIntentionalRename()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Service" />
		                              <NameRules>
		                                  <RequireMatchingNames>
		                                      <Name endsWith="Id" />
		                                      <Allow from="legacyCustomerId" to="customerId" />
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

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Where(d => d.Id == ArchitecturalDiagnosticIds.NameRuleViolation).Should().BeEmpty();
	}

	[Fact]
	public async Task RequireMatchingNames_AllowMapping_RespectsAllowedSites()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Service" />
		                              <NameRules>
		                                  <RequireMatchingNames>
		                                      <Name endsWith="Id" />
		                                      <Allow from="legacyCustomerId" to="customerId" allowedSites="Constructor" />
		                                  </RequireMatchingNames>
		                              </NameRules>
		                          </Layer>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public sealed class Customer
		                      {
		                          public Customer(int customerId)
		                          {
		                          }
		                      }

		                      public class OrderService
		                      {
		                          public void Run(int legacyCustomerId)
		                          {
		                              _ = new Customer(legacyCustomerId);
		                              Save(legacyCustomerId);
		                          }

		                          private void Save(int customerId)
		                          {
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.NameRuleViolation).Which;
		diagnostic.Properties["Site"].Should().Be("Method");
		diagnostic.GetMessage().Should().Contain("allowedSites does not include Method");
	}

	[Fact]
	public async Task RequireMatchingNames_CanBeScopedToOneSite()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Service" />
		                              <NameRules>
		                                  <RequireMatchingNames allowedSites="Local">
		                                      <Name endsWith="Id" />
		                                  </RequireMatchingNames>
		                              </NameRules>
		                          </Layer>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class OrderService
		                      {
		                          public void Run(int animalId)
		                          {
		                              Save(animalId);
		                              var fruitId = animalId;
		                          }

		                          private void Save(int fruitId)
		                          {
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.NameRuleViolation).Which;
		diagnostic.Properties["Site"].Should().Be("Local");
	}

	[Fact]
	public async Task RequireMatchingNames_IsLayerScoped()
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
		                          <Layer name="Persistence">
		                              <Class endsWith="Repository" />
		                          </Layer>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class OrderService
		                      {
		                          public void Run(int animalId)
		                          {
		                              var fruitId = animalId;
		                          }
		                      }

		                      public class OrderRepository
		                      {
		                          public void Run(int animalId)
		                          {
		                              var fruitId = animalId;
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.NameRuleViolation).Which;
		diagnostic.GetMessage().Should().Contain("OrderService");
	}

	[Fact]
	public async Task RequireMatchingNames_UsesCombinedMatcherConditions()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Service" />
		                              <NameRules>
		                                  <RequireMatchingNames>
		                                      <Name startsWith="customer" endsWith="Id" />
		                                  </RequireMatchingNames>
		                              </NameRules>
		                          </Layer>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class OrderService
		                      {
		                          public void Run(int animalId, int customerId)
		                          {
		                              var fruitId = animalId;
		                              var orderId = customerId;
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.NameRuleViolation).Which;
		diagnostic.Properties["SourceName"].Should().Be("customerId");
		diagnostic.Properties["TargetName"].Should().Be("orderId");
	}
}
