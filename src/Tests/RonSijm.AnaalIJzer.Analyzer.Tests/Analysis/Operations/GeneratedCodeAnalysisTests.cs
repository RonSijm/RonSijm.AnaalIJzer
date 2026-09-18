using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis.Operations;

public sealed class GeneratedCodeAnalysisTests
{
	[Fact]
	public async Task GeneratedSource_IsExcludedByDefault()
	{
		var diagnostics = await GetDiagnosticsAsync("Generated_Clock_Kitchen.g.cs", CreateConfig());

		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.OperationNotAllowed);
	}

	[Fact]
	public async Task GeneratedSource_IsAnalyzedWhenIncludeConfiguredPathMatches()
	{
		const string generatedCode = """
			<GeneratedCode mode="IncludeConfigured">
			  <Path endsWith="Generated_Clock_Kitchen.g.cs" />
			</GeneratedCode>
			""";
		var diagnostics = await GetDiagnosticsAsync("Generated_Clock_Kitchen.g.cs", CreateConfig(generatedCode));

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.OperationNotAllowed);
	}

	[Fact]
	public async Task GeneratedSource_IsExcludedWhenConfiguredPathDoesNotMatch()
	{
		const string generatedCode = """
			<GeneratedCode mode="IncludeConfigured">
			  <Path endsWith="Generated_Menu_Kitchen.g.cs" />
			</GeneratedCode>
			""";
		var diagnostics = await GetDiagnosticsAsync("Generated_Clock_Kitchen.g.cs", CreateConfig(generatedCode));

		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.OperationNotAllowed);
	}

	[Fact]
	public async Task OrdinarySource_RemainsAnalyzedWhenGeneratedSourceIsExcluded()
	{
		var diagnostics = await GetDiagnosticsAsync("PizzaKitchen.cs", CreateConfig());

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.OperationNotAllowed);
	}

	private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string sourcePath, string config)
	{
		const string source = """
			using System;

			public sealed class PizzaKitchen
			{
				public DateTime Prepare() => DateTime.UtcNow;
			}
			""";
		var result = await AnalyzerTestHelper.GetDiagnosticsAsync(
			[(sourcePath, source)],
			null,
			("Architecture.anl", config));

		return result;
	}

	private static string CreateConfig(string? generatedCode = null)
	{
		var result = $"""
			<ArchitecturalLevels>
			  {generatedCode}
			  <Layer name="Kitchen">
			    <Class endsWith="Kitchen" />
			    <ForbiddenOperations>
			      <ForbiddenOperation allowedSites="StaticMember">
			        <OperationMatcher kind="PropertyRead" staticAccess="true">
			          <ContainingType exactFullName="System.DateTime" />
			          <Member exactName="UtcNow" memberKind="Property" />
			        </OperationMatcher>
			      </ForbiddenOperation>
			    </ForbiddenOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

		return result;
	}
}
