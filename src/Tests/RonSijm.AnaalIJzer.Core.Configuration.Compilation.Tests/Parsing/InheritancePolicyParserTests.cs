using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.TestSupport;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.Parsing;

public sealed class InheritancePolicyParserTests
{
	[Fact]
	public void Parser_ReadsInheritancePolicies()
	{
		const string configText = """
			<ArchitecturalLevels>
			  <Layer name="PersistenceEntities">
			    <Namespace startsWith="Shop.Persistence" />
			    <InheritancePolicy
			      typeKinds="Class"
			      requiredBaseTypes="Entity, AggregateRoot"
			      requiredInterfaces="IAuditedEntity"
			      description="Persistence entities use the shared entity contract." />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var config = ParseConfig(configText, @"D:\repo\config\Architecture.anl");

		config.HasInheritancePolicies.Should().BeTrue();
		var layer = config.Layers.Should().ContainSingle().Subject;
		var policy = layer.InheritancePolicies.Should().ContainSingle().Subject;
		policy.TypeKinds.Should().BeEquivalentTo(["Class"]);
		policy.RequiredBaseTypes.Should().BeEquivalentTo(["Entity", "AggregateRoot"]);
		policy.RequiredInterfaces.Should().BeEquivalentTo(["IAuditedEntity"]);
		policy.Description.Should().Be("Persistence entities use the shared entity contract.");
	}

	[Theory]
	[InlineData("""<InheritancePolicy requiredBaseTypes="Entity" />""")]
	[InlineData("""<InheritancePolicy typeKinds="Class" />""")]
	[InlineData("""<InheritancePolicy typeKinds="Unknown" requiredBaseTypes="Entity" />""")]
	public void Parser_RejectsInvalidInheritancePolicy(string policyXml)
	{
		var configText = $"""
			<ArchitecturalLevels>
			  <Layer name="PersistenceEntities">
			    <Namespace startsWith="Shop.Persistence" />
			    {policyXml}
			  </Layer>
			</ArchitecturalLevels>
			""";

		var config = ParseConfig(configText, @"D:\repo\config\Architecture.anl");

		config.ConfigurationIssues.Should().Contain(issue => issue.Kind == ConfigurationIssueKind.InvalidConfiguration);
		config.Layers.Should().ContainSingle().Which.InheritancePolicies.Should().BeEmpty();
	}

	private static AnalyzerConfiguration ParseConfig(string configText, string configPath)
	{
		var result = ArchitecturalConfigParser.Parse(
            [
                new TestAdditionalText(configPath, configText)
            ],
			CancellationToken.None);

		return result;
	}
}
