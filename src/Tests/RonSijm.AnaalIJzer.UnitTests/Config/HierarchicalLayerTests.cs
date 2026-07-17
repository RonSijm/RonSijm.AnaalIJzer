using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Parsing;
using RonSijm.AnaalIJzer.UnitTests.TestSupport;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.UnitTests.Config;

public sealed class HierarchicalLayerTests
{
	[Fact]
	public async Task CrossBoundaryDependency_PassesEveryConfiguredGate()
	{
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(CrossBoundarySource, CreateCrossBoundaryConfig());

		diagnostics.Should().BeEmpty();
	}

	[Theory]
	[InlineData("outer", "root boundary")]
	[InlineData("egress", "boundary 'Ordering'")]
	[InlineData("ingress", "boundary 'Billing'")]
	public async Task CrossBoundaryDependency_ReportsFirstMissingGate(string missingGate, string expectedBoundary)
	{
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(CrossBoundarySource, CreateCrossBoundaryConfig(missingGate));

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency).Subject;
		diagnostic.GetMessage().Should().Contain(expectedBoundary);
		diagnostic.GetMessage().Should().Contain("Ordering/Application");
		diagnostic.GetMessage().Should().Contain("Billing/Contracts");
	}

	[Fact]
	public async Task InternalDependency_IsCheckedAtSharedBoundary()
	{
		const string source = """
		                      namespace Shop.Ordering.Application
		                      {
		                          public class PlaceOrderService(Shop.Ordering.Repository.OrderRepository repository) { }
		                      }
		                      namespace Shop.Ordering.Repository
		                      {
		                          public class OrderRepository { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateCrossBoundaryConfig());

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task DifferentChildrenOfSameBoundary_DoNotReportSameLayer()
	{
		var config = CreateCrossBoundaryConfig().Replace(
			"""<AllowedDependency from="Application" to="Repository" />""",
			string.Empty);
		const string source = """
		                      namespace Shop.Ordering.Application
		                      {
		                          public class PlaceOrderService(Shop.Ordering.Repository.OrderRepository repository) { }
		                      }
		                      namespace Shop.Ordering.Repository
		                      {
		                          public class OrderRepository { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency);
		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.SameLayerDependency);
	}

	[Fact]
	public async Task SameDeepestChild_ReportsSameLayer()
	{
		const string source = """
		                      namespace Shop.Ordering.Application
		                      {
		                          public class PrepareOrderService { }
		                          public class PlaceOrderService(PrepareOrderService service) { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateCrossBoundaryConfig());

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.SameLayerDependency).Subject;
		diagnostic.GetMessage().Should().Contain("Ordering/Application");
	}

	[Fact]
	public async Task ParentMatcher_ClassifiesTypesThatMatchNoChild()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Ordering">
		                          <Namespace startsWith="Shop.Ordering" />
		                          <Layer name="Application"><Namespace startsWith="Shop.Ordering.Application" /></Layer>
		                          <AllowedDependency from="/Ordering" to="Application" />
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      namespace Shop.Ordering
		                      {
		                          public class OrderModule(Shop.Ordering.Application.PlaceOrderService service) { }
		                      }
		                      namespace Shop.Ordering.Application
		                      {
		                          public class PlaceOrderService { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task MatcherlessParent_DerivesMembershipFromChildren()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Commerce">
		                          <Layer name="Ordering"><Class typeName="PlaceOrderService" /></Layer>
		                          <Layer name="Billing"><Class typeName="InvoiceContract" /></Layer>
		                          <AllowedDependency from="Ordering" to="Billing" />
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      public class InvoiceContract { }
		                      public class PlaceOrderService(InvoiceContract contract) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task OuterBlockedRule_CannotBeOverriddenByInnerAllowances()
	{
		var config = CreateCrossBoundaryConfig().Replace(
			"""<AllowedDependency from="Ordering" to="Billing" />""",
			"""
			  <AllowedDependency from="Ordering" to="Billing" />
			  <BlockedDependency from="Ordering" to="Billing" />
			""");

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(CrossBoundarySource, config);

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency).Subject;
		diagnostic.GetMessage().Should().Contain("explicitly blocks");
	}

	[Fact]
	public async Task ReverseOuterGate_ReportsWrongDirection()
	{
		const string source = """
		                      namespace Shop.Ordering.Application
		                      {
		                          public class PlaceOrderService { }
		                      }
		                      namespace Shop.Billing.Contracts
		                      {
		                          public class InvoiceContract(Shop.Ordering.Application.PlaceOrderService service) { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateCrossBoundaryConfig());

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.WrongDirectionDependency);
	}

	[Fact]
	public async Task SiteFilters_MustPassAtEveryGate()
	{
		var config = CreateCrossBoundaryConfig().Replace(
			"""<AllowedDependency from="Application" to="/Billing/Contracts" />""",
			"""<AllowedDependency from="Application" to="/Billing/Contracts" allowedSites="Local" />""");

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(CrossBoundarySource, config);

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency).Subject;
		diagnostic.GetMessage().Should().Contain("allowedSites does not include Constructor");
		diagnostic.GetMessage().Should().Contain("boundary 'Ordering'");
	}

	[Fact]
	public async Task Wildcards_AreScopedToTheirBoundaryGate()
	{
		var config = CreateCrossBoundaryConfig()
			.Replace("""<AllowedDependency from="Application" to="/Billing/Contracts" />""", """<AllowedDependency from="Application" to="*" />""")
			.Replace("""<AllowedDependency from="/Ordering/Application" to="Contracts" />""", """<AllowedDependency from="*" to="Contracts" />""");

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(CrossBoundarySource, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public void Matching_UsesAncestorScopeAndFirstMatchingSibling()
	{
		const string configText = """
		                          <ArchitecturalLevels>
		                            <Layer name="Ordering">
		                              <Namespace startsWith="Shop.Ordering" />
		                              <Layer name="First"><Class endsWith="Service" /></Layer>
		                              <Layer name="Second"><Class typeName="OrderService" /></Layer>
		                            </Layer>
		                          </ArchitecturalLevels>
		                          """;
		var config = Parse(configText);

		config.FindLayer("OrderService", "Shop.Ordering.Application")!.Value.Layer.Name.Should().Be("Ordering/First");
		config.FindLayer("OrderService", "Shop.Billing.Application").Should().BeNull();
	}

	[Fact]
	public void Parser_AllowsRepeatedChildNamesUnderDifferentParents()
	{
		var config = Parse(CreateCrossBoundaryConfig());

		config.ConfigurationIssues.Should().BeEmpty();
		config.LayerNames.Should().ContainInOrder("Ordering", "Ordering/Application", "Ordering/Repository", "Billing", "Billing/Application", "Billing/Contracts");
	}

	[Fact]
	public void Parser_RejectsDuplicateSiblingNamesAndUnrootedPaths()
	{
		const string configText = """
		                          <ArchitecturalLevels>
		                            <Layer name="Ordering">
		                              <Namespace startsWith="Shop.Ordering" />
		                              <Layer name="Application"><Class endsWith="Service" /></Layer>
		                              <Layer name="Application"><Class endsWith="Manager" /></Layer>
		                              <AllowedDependency from="Application" to="Billing/Contracts" />
		                            </Layer>
		                          </ArchitecturalLevels>
		                          """;

		var config = Parse(configText);

		config.ConfigurationIssues.Should().Contain(item => item.Message.Contains("declared more than once"));
		config.ConfigurationIssues.Should().Contain(item => item.Message.Contains("must start with '/'"));
	}

	[Fact]
	public void CycleDetection_UsesCanonicalNestedPaths()
	{
		const string configText = """
		                          <ArchitecturalLevels enforceAcyclic="true">
		                            <Layer name="Ordering">
		                              <Namespace startsWith="Shop.Ordering" />
		                              <Layer name="Application"><Class endsWith="Service" /></Layer>
		                              <Layer name="Repository"><Class endsWith="Repository" /></Layer>
		                              <AllowedDependency from="Application" to="Repository" />
		                              <AllowedDependency from="Repository" to="Application" />
		                            </Layer>
		                          </ArchitecturalLevels>
		                          """;

		var config = Parse(configText);

		config.ConfigurationIssues.Should().ContainSingle(item => item.Kind == ConfigurationIssueKind.CyclicDependencyGraph);
		config.ConfigurationIssues[0].Message.Should().Contain("Ordering/Application -> Ordering/Repository");
	}

	[Fact]
	public async Task CascadingDependency_DefaultsToStrictNestedBoundary()
	{
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(FrameworkDependencySource, CreateFrameworkBoundaryConfig());

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency).Subject;
		diagnostic.GetMessage().Should().Contain("boundary 'Application'");
	}

	[Fact]
	public async Task CascadingDependency_RootWildcardAllowsDescendantBoundary()
	{
		var config = CreateFrameworkBoundaryConfig("""<AllowedDependency from="*" to="Framework" appliesToDescendants="true" />""");

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(FrameworkDependencySource, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task CascadingDependency_ScopedCascadeDoesNotApplyToUnrelatedParent()
	{
		const string source = """
		                      namespace Platform
		                      {
		                          public class FrameworkToken { }
		                      }
		                      namespace App.Contracts
		                      {
		                          public interface ISsoManager
		                          {
		                              Platform.FrameworkToken GetToken();
		                          }
		                      }
		                      namespace Data.Contracts
		                      {
		                          public interface IUserRepository
		                          {
		                              Platform.FrameworkToken GetToken();
		                          }
		                      }
		                      """;
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Application">
		                          <Namespace startsWith="App" />
		                          <Layer name="Contracts"><Namespace startsWith="App.Contracts" /></Layer>
		                          <AllowedDependency from="*" to="/Framework" appliesToDescendants="true" />
		                        </Layer>
		                        <Layer name="DataAbstraction">
		                          <Namespace startsWith="Data" />
		                          <Layer name="Contracts"><Namespace startsWith="Data.Contracts" /></Layer>
		                        </Layer>
		                        <Layer name="Framework"><Namespace startsWith="Platform" /></Layer>
		                        <AllowedDependency from="Application" to="Framework" />
		                        <AllowedDependency from="DataAbstraction" to="Framework" />
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency).Subject;
		diagnostic.GetMessage().Should().Contain("DataAbstraction/Contracts").And.Contain("boundary 'DataAbstraction'");
	}

	[Fact]
	public async Task CascadingDependency_SiteFiltersAreEnforced()
	{
		var config = CreateFrameworkBoundaryConfig("""<AllowedDependency from="*" to="Framework" allowedSites="MethodReturn" appliesToDescendants="true" />""");

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(FrameworkDependencySource, config);

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency).Subject;
		diagnostic.GetMessage().Should().Contain("allowedSites does not include Constructor");
	}

	[Fact]
	public async Task CascadingDependency_LocalBlockedRuleDeniesCascadedAllowedRule()
	{
		var config = CreateFrameworkBoundaryConfig(
			"""<AllowedDependency from="*" to="Framework" appliesToDescendants="true" />""",
			"""<BlockedDependency from="Contracts" to="/Framework" />""");

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(FrameworkDependencySource, config);

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency).Subject;
		diagnostic.GetMessage().Should().Contain("explicitly blocks").And.Contain("boundary 'Application'");
	}

	[Fact]
	public async Task CascadingDependency_CascadedBlockedRuleCannotBeOverriddenLocally()
	{
		var config = CreateFrameworkBoundaryConfig(
			"""
			<AllowedDependency from="*" to="Framework" appliesToDescendants="true" />
			<BlockedDependency from="Application" to="Framework" appliesToDescendants="true" />
			""",
			"""<AllowedDependency from="Contracts" to="/Framework" />""");

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(FrameworkDependencySource, config);

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency).Subject;
		diagnostic.GetMessage().Should().Contain("explicitly blocks");
	}

	[Fact]
	public async Task CascadingDependency_ReverseCascadedRuleReportsWrongDirection()
	{
		var config = CreateFrameworkBoundaryConfig(
			"""
			<AllowedDependency from="Application" to="Framework" />
			<AllowedDependency from="Framework" to="Application" appliesToDescendants="true" />
			""");

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(FrameworkDependencySource, config);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.WrongDirectionDependency);
	}

	private static string CreateCrossBoundaryConfig(string? missingGate = null)
	{
		var outer = missingGate == "outer" ? string.Empty : """<AllowedDependency from="Ordering" to="Billing" />""";
		var egress = missingGate == "egress" ? string.Empty : """<AllowedDependency from="Application" to="/Billing/Contracts" />""";
		var ingress = missingGate == "ingress" ? string.Empty : """<AllowedDependency from="/Ordering/Application" to="Contracts" />""";
		return $$"""
		          <ArchitecturalLevels>
		            <Layer name="Ordering">
		              <Namespace startsWith="Shop.Ordering" />
		              <Layer name="Application"><Namespace startsWith="Shop.Ordering.Application" /></Layer>
		              <Layer name="Repository"><Namespace startsWith="Shop.Ordering.Repository" /></Layer>
		              {{egress}}
		              <AllowedDependency from="Application" to="Repository" />
		            </Layer>
		            <Layer name="Billing">
		              <Namespace startsWith="Shop.Billing" />
		              <Layer name="Application"><Namespace startsWith="Shop.Billing.Application" /></Layer>
		              <Layer name="Contracts"><Namespace startsWith="Shop.Billing.Contracts" /></Layer>
		              {{ingress}}
		            </Layer>
		            {{outer}}
		          </ArchitecturalLevels>
		          """;
	}

	private static string CreateFrameworkBoundaryConfig(string rootRules = """<AllowedDependency from="*" to="Framework" />""", string applicationRules = "")
	{
		return $$"""
		         <ArchitecturalLevels>
		           <Layer name="Application">
		             <Namespace startsWith="App" />
		             <Layer name="Contracts"><Namespace startsWith="App.Contracts" /></Layer>
		             {{applicationRules}}
		           </Layer>
		           <Layer name="Framework">
		             <Namespace startsWith="Platform" />
		           </Layer>
		           {{rootRules}}
		         </ArchitecturalLevels>
		         """;
	}

	private static AnalyzerConfiguration Parse(string configText)
    {
        var result = ArchitecturalConfigParser.Parse([new TestAdditionalText("Architecture.anl", configText)],
            CancellationToken.None);

		return result;
    }

    private const string CrossBoundarySource = """
                                               namespace Shop.Billing.Contracts
                                               {
                                                   public class InvoiceContract { }
                                               }
                                               namespace Shop.Ordering.Application
                                               {
                                                   public class PlaceOrderService(Shop.Billing.Contracts.InvoiceContract contract) { }
                                               }
                                               """;

	private const string FrameworkDependencySource = """
	                                                 namespace Platform
	                                                 {
	                                                     public class FrameworkToken { }
	                                                 }
	                                                 namespace App.Contracts
	                                                 {
	                                                     public class SsoManager(Platform.FrameworkToken token) { }
	                                                 }
	                                                 """;

	private sealed class TestAdditionalText(string path, string content) : AdditionalText
    {
		private readonly SourceText _text = SourceText.From(content);

        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            var result = _text;

			return result;
        }
    }
}
