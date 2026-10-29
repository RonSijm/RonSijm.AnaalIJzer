using RonSijm.IJzer.Analyzer.Tests.Testing;
using RonSijm.IJzer.Core.Findings;

namespace RonSijm.IJzer.Analyzer.Tests.Config;

public sealed class ProjectArchitectureTests
{
    [Fact]
    public async Task ProjectMatcher_WithSemanticAttribute_ReportsARCH_CONF_003()
    {
        const string config = """
		                      <ArchitecturalLevels>
		                        <ProjectArchitecture>
		                          <ProjectGroup name="Domain">
		                            <Project endsWith=".Domain" typeKind="Class" />
		                          </ProjectGroup>
		                        </ProjectArchitecture>
		                      </ArchitecturalLevels>
		                      """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public class Placeholder { }", config);

        diagnostics.Should().Contain(item => item.Id == ArchitecturalDiagnosticIds.ConfigurationInvalid);
    }

}