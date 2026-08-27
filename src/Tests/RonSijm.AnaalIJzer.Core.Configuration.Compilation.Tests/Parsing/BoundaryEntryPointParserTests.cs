using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.TestSupport;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.Parsing;

public sealed class BoundaryEntryPointParserTests
{
    [Fact]
    public void Parser_ReadsBoundaryEntryPointPolicies()
    {
        const string configText = """
                                  <ArchitecturalLevels>
                                    <Layer name="Ordering">
                                      <Namespace startsWith="Shop.Ordering" />
                                      <EntryPoints>
                                        <EntryPoint layer="Contracts" />
                                        <EntryPoint allowedSites="Method">
                                          <Class endsWith="Facade" />
                                        </EntryPoint>
                                      </EntryPoints>
                                      <Layer name="Contracts">
                                        <Class endsWith="Contract" />
                                      </Layer>
                                      <Layer name="Implementation">
                                        <Class endsWith="Service" />
                                      </Layer>
                                    </Layer>
                                  </ArchitecturalLevels>
                                  """;

        var config = ParseConfig(configText);

        config.HasEntryPointPolicies.Should().BeTrue();
        var ordering = config.Layers.Should().ContainSingle().Subject;
        ordering.EntryPointPolicies.Should().ContainSingle();
        ordering.EntryPointPolicies[0].Rules.Should().HaveCount(2);
        ordering.EntryPointPolicies[0].Rules[0].Selector.LayerPath.Should().Be("Ordering/Contracts");
        ordering.EntryPointPolicies[0].Rules[1].SiteFilter.AllowedSites.Should().ContainSingle().Which.Should().Be("Method");
    }

    [Fact]
    public void Parser_RejectsLayerSelectorOutsideOwningBoundary()
    {
        const string configText = """
                                  <ArchitecturalLevels>
                                    <Layer name="Presentation">
                                      <Class endsWith="Controller" />
                                    </Layer>
                                    <Layer name="Ordering">
                                      <Namespace startsWith="Shop.Ordering" />
                                      <EntryPoints>
                                        <EntryPoint layer="/Presentation" />
                                      </EntryPoints>
                                      <Layer name="Contracts">
                                        <Class endsWith="Contract" />
                                      </Layer>
                                    </Layer>
                                  </ArchitecturalLevels>
                                  """;

        var config = ParseConfig(configText);

        config.ConfigurationIssues.Should().Contain(issue => issue.Message.Contains("must resolve to layer 'Ordering' or one of its descendants"));
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
