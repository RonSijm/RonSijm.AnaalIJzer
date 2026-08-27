using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.TestSupport;
using RonSijm.AnaalIJzer.Core.SourceLocations;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.Parsing;

public sealed class SourceLocationParserTests
{
    [Fact]
    public void Parser_ReadsSourceLocationPolicies()
    {
        const string configText = """
                                  <ArchitecturalLevels>
                                    <Layer name="Ordering">
                                      <Class endsWith="Service" />
                                      <SourceLocations relativeTo="Configuration">
                                        <Source startsWith="Features/Ordering/" assemblyName="Shop.Application" />
                                        <Source startsWith="Contracts/Ordering/" />
                                      </SourceLocations>
                                    </Layer>
                                  </ArchitecturalLevels>
                                  """;

        var config = ParseConfig(configText, @"D:\repo\config\Architecture.anl");

        config.HasSourceLocationPolicies.Should().BeTrue();
        var layer = config.Layers.Should().ContainSingle().Subject;
        layer.SourceLocationPolicies.Should().ContainSingle();
        layer.SourceLocationPolicies[0].RelativeTo.Should().Be(SourceLocationBase.Configuration);
        layer.SourceLocationPolicies[0].Rules.Should().HaveCount(2);
        layer.SourceLocationPolicies[0].Rules[0].AssemblyName.Should().Be("Shop.Application");
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
