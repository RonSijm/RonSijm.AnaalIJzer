using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Config.Parsing;
using RonSijm.AnaalIJzer.ProjectArchitecture;
using RonSijm.AnaalIJzer.Testing;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Config;

public sealed class ProjectArchitectureTests
{
	[Fact]
	public async Task ProjectMatcher_WithSemanticAttribute_ReportsARCH006()
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

		diagnostics.Should().Contain(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration);
	}

}
