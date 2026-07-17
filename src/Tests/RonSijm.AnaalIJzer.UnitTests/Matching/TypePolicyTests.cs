using RonSijm.AnaalIJzer.UnitTests.TestSupport;

namespace RonSijm.AnaalIJzer.UnitTests.Matching;

public sealed class TypePolicyTests
{
	[Fact]
	public async Task GlobalAllowed_ReportsOnlyDependenciesOutsideTheWhitelist()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Application"><Class endsWith="Service" /></Layer>
		                        <Layer name="Command"><Class endsWith="Command" /></Layer>
		                        <Allowed>
		                          <Class startsWith="Create" />
		                          <Class startsWith="Cancel" />
		                        </Allowed>
		                        <AllowedDependency from="Application" to="Command" />
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      public class CreateOrderCommand { }
		                      public class CancelOrderCommand { }
		                      public class ProcessOrderCommand { }
		                      public class OrderService(CreateOrderCommand create, CancelOrderCommand cancel, ProcessOrderCommand process) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);
		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenDependency).Subject;
		diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Contain("global <Allowed>").And.Contain("ProcessOrderCommand");
	}

	[Fact]
	public async Task ScopedAllowed_AppliesOnlyToItsLayer()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Application"><Class endsWith="Service" /></Layer>
		                        <Layer name="Command">
		                          <Class endsWith="Command" />
		                          <Allowed><Class startsWith="Create" /></Allowed>
		                        </Layer>
		                        <Layer name="Query"><Class endsWith="Query" /></Layer>
		                        <AllowedDependency from="Application" to="Command" />
		                        <AllowedDependency from="Application" to="Query" />
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      public class CreateOrderCommand { }
		                      public class ProcessOrderCommand { }
		                      public class ProcessOrderQuery { }
		                      public class OrderService(CreateOrderCommand allowedCommand, ProcessOrderCommand deniedCommand, ProcessOrderQuery unaffectedQuery) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);
		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenDependency).Subject;
		diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Contain("layer 'Command'").And.Contain("ProcessOrderCommand");
	}

	[Fact]
	public async Task ScopedForbidden_AppliesOnlyToItsLayer()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Application"><Class endsWith="Service" /></Layer>
		                        <Layer name="Query">
		                          <Class endsWith="Query" />
		                          <Forbidden><Class startsWith="Delete" /></Forbidden>
		                        </Layer>
		                        <Layer name="Audit"><Class endsWith="AuditRecord" /></Layer>
		                        <AllowedDependency from="Application" to="Query" />
		                        <AllowedDependency from="Application" to="Audit" />
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      public class FindOrderQuery { }
		                      public class DeleteOrderQuery { }
		                      public class DeleteOrderAuditRecord { }
		                      public class OrderService(FindOrderQuery allowedQuery, DeleteOrderQuery deniedQuery, DeleteOrderAuditRecord unaffectedAuditRecord) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);
		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenDependency).Subject;
		diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Contain("<Forbidden>").And.Contain("layer 'Query'").And.Contain("DeleteOrderQuery");
	}

	[Fact]
	public async Task NestedAllowed_RequiresEveryAncestorWhitelistToPass()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Application"><Class endsWith="Service" /></Layer>
		                        <Layer name="Ordering">
		                          <Namespace startsWith="Company.Ordering" />
		                          <Allowed><Class startsWith="Create" /></Allowed>
		                          <Layer name="Command">
		                            <Class endsWith="Command" />
		                            <Allowed><Class contains="Order" /></Allowed>
		                          </Layer>
		                        </Layer>
		                        <AllowedDependency from="Application" to="Ordering" />
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      namespace Company.Ordering
		                      {
		                          public class CreateOrderCommand { }
		                          public class CreateCustomerCommand { }
		                      }

		                      public class CheckoutService(Company.Ordering.CreateOrderCommand allowed, Company.Ordering.CreateCustomerCommand denied) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);
		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenDependency).Subject;
		diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Contain("Ordering/Command").And.Contain("CreateCustomerCommand");
	}

	[Fact]
	public async Task NestedForbidden_IsInheritedByDescendantLayers()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Application"><Class endsWith="Service" /></Layer>
		                        <Layer name="Ordering">
		                          <Namespace startsWith="Company.Ordering" />
		                          <Forbidden><Class startsWith="Delete" /></Forbidden>
		                          <Layer name="Command"><Class endsWith="Command" /></Layer>
		                        </Layer>
		                        <AllowedDependency from="Application" to="Ordering" />
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      namespace Company.Ordering
		                      {
		                          public class CreateOrderCommand { }
		                          public class DeleteOrderCommand { }
		                      }

		                      public class CheckoutService(Company.Ordering.CreateOrderCommand allowed, Company.Ordering.DeleteOrderCommand denied) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);
		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenDependency).Subject;
		diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Contain("layer 'Ordering'").And.Contain("DeleteOrderCommand");
	}

	[Fact]
	public async Task Forbidden_WinsWhenAllowedAlsoMatches()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Application"><Class endsWith="Service" /></Layer>
		                        <Layer name="Command">
		                          <Class endsWith="Command" />
		                          <Allowed><Class startsWith="Create" /></Allowed>
		                          <Forbidden><Class typeName="CreateAdminCommand" /></Forbidden>
		                        </Layer>
		                        <AllowedDependency from="Application" to="Command" />
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      public class CreateOrderCommand { }
		                      public class CreateAdminCommand { }
		                      public class OrderService(CreateOrderCommand allowed, CreateAdminCommand denied) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);
		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenDependency).Subject;
		diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Contain("<Forbidden>").And.Contain("CreateAdminCommand");
	}

	[Fact]
	public async Task ScopedForbidden_RespectsExceptions()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Application"><Class endsWith="Service" /></Layer>
		                        <Layer name="Command">
		                          <Class endsWith="Command" />
		                          <Forbidden>
		                            <Class startsWith="Delete">
		                              <Exceptions><Class typeName="DeleteDraftCommand" /></Exceptions>
		                            </Class>
		                          </Forbidden>
		                        </Layer>
		                        <AllowedDependency from="Application" to="Command" />
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      public class DeleteDraftCommand { }
		                      public class DeleteOrderCommand { }
		                      public class OrderService(DeleteDraftCommand allowed, DeleteOrderCommand denied) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);
		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Which.GetMessage(CultureInfo.InvariantCulture).Should().Contain("DeleteOrderCommand");
	}

	[Fact]
	public async Task GlobalForbidden_AppliesWhenTheTypeAlsoMatchesALayer()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Application"><Class endsWith="Service" /></Layer>
		                        <Layer name="Command"><Class endsWith="Command" /></Layer>
		                        <Forbidden><Class startsWith="Delete" /></Forbidden>
		                        <AllowedDependency from="Application" to="Command" />
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      public class CreateOrderCommand { }
		                      public class DeleteOrderCommand { }
		                      public class OrderService(CreateOrderCommand allowed, DeleteOrderCommand denied) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);
		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenDependency).Subject;
		diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Contain("global <Forbidden>").And.Contain("DeleteOrderCommand");
	}
}
