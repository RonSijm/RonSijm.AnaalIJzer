using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.TestSupport;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.OperationContracts.Model;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.Parsing;

public sealed class OperationContractParserTests
{
    [Fact]
    public void Parser_ReadsAnExplicitOperationContract()
    {
        const string configText = """
			<ArchitecturalLevels>
			  <Layer name="Controller"><Class endsWith="Controller" /></Layer>
			  <Layer name="Application"><Class endsWith="Kitchen" /></Layer>
			  <Operations description="The order path is explicit.">
			    <Operation name="PlacePizzaOrder" allowedOwnerLayers="Application" allowedEntryPointLayers="Controller" description="A waiter asks the kitchen to place the order.">
			      <Owner>
			        <DeclarationMatcher>
			          <ContainingType endsWith="Kitchen" />
			          <Member exactName="PlacePizzaOrder" memberKind="Method" />
			        </DeclarationMatcher>
			      </Owner>
			      <Request><Class exactName="PlacePizzaOrderRequest" /></Request>
			      <Response><Class exactName="PlacePizzaOrderResponse" /></Response>
			      <EntryPoint>
			        <DeclarationMatcher>
			          <ContainingType endsWith="Controller" />
			          <Member exactName="PlacePizzaOrder" memberKind="Method" />
			        </DeclarationMatcher>
			      </EntryPoint>
			    </Operation>
			  </Operations>
			</ArchitecturalLevels>
			""";

        var config = ParseConfig(configText);

        config.ConfigurationIssues.Should().BeEmpty();
        config.HasOperationContracts.Should().BeTrue();
        var operation = config.OperationContracts.Definitions.Should().ContainSingle().Subject;
        operation.Name.Should().Be("PlacePizzaOrder");
        operation.Description.Should().Be("A waiter asks the kitchen to place the order.");
        operation.AllowedOwnerLayers.Should().ContainSingle().Which.Should().Be("Application");
        operation.AllowedEntryPointLayers.Should().ContainSingle().Which.Should().Be("Controller");
        operation.HasRequestContract.Should().BeTrue();
        operation.HasResponseContract.Should().BeTrue();
        operation.EntryPoints.Should().ContainSingle();
    }

    [Theory]
    [InlineData("""<Operations />""")]
    [InlineData("""<Operation name="PlacePizzaOrder"><EntryPoint><DeclarationMatcher><Member exactName="PlacePizzaOrder" memberKind="Method" /></DeclarationMatcher></EntryPoint></Operation>""")]
    [InlineData("""<Operation name="PlacePizzaOrder"><Owner><DeclarationMatcher><Member exactName="PlacePizzaOrder" memberKind="Property" /></DeclarationMatcher></Owner></Operation>""")]
    [InlineData("""<Operation name="PlacePizzaOrder" allowedOwnerLayers="*"><Owner><DeclarationMatcher><Member exactName="PlacePizzaOrder" memberKind="Method" /></DeclarationMatcher></Owner></Operation>""")]
    [InlineData("""<Operation name="PlacePizzaOrder"><Owner><DeclarationMatcher><Member exactName="PlacePizzaOrder" memberKind="Method" /></DeclarationMatcher></Owner><Request><Class /></Request></Operation>""")]
    public void Parser_RejectsInvalidOperationContract(string operationXml)
    {
        var configText = "<ArchitecturalLevels>" + operationXml + "</ArchitecturalLevels>";

        var config = ParseConfig(configText);

        config.ConfigurationIssues.Should().Contain(issue => issue.Kind == ConfigurationIssueKind.InvalidConfiguration);
        config.OperationContracts.Definitions.Should().BeEmpty();
    }

    [Fact]
    public void Parser_RejectsDuplicateOperationNamesAcrossIncludedSettings()
    {
        const string rootConfig = """
			<ArchitecturalLevels>
			  <Include path="Shared.anl" />
			  <Operations>
			    <Operation name="PlacePizzaOrder">
			      <Owner><DeclarationMatcher><Member exactName="PlacePizzaOrder" memberKind="Method" /></DeclarationMatcher></Owner>
			    </Operation>
			  </Operations>
			</ArchitecturalLevels>
			""";
        const string sharedConfig = """
			<ArchitecturalLevels>
			  <Operations>
			    <Operation name="PlacePizzaOrder">
			      <Owner><DeclarationMatcher><Member exactName="PreparePizzaOrder" memberKind="Method" /></DeclarationMatcher></Owner>
			    </Operation>
			  </Operations>
			</ArchitecturalLevels>
			""";

        var config = ArchitecturalConfigParser.Parse(
            [
                new TestAdditionalText(@"D:\repo\Architecture.anl", rootConfig),
                new TestAdditionalText(@"D:\repo\Shared.anl", sharedConfig)
            ],
            CancellationToken.None);

        config.ConfigurationIssues.Should().Contain(issue => issue.Message.Contains("declared more than once", StringComparison.Ordinal));
        config.OperationContracts.Definitions.Should().ContainSingle();
    }

    private static AnalyzerConfiguration ParseConfig(string configText)
    {
        var result = ArchitecturalConfigParser.Parse(
            [
                new TestAdditionalText(@"D:\repo\Architecture.anl", configText)
            ],
            CancellationToken.None);

        return result;
    }
}