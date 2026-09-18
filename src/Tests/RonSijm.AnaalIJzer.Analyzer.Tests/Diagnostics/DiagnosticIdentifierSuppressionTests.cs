using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

public sealed class DiagnosticIdentifierSuppressionTests
{
	private const string Source = """
	                              public class CandyRepository { }
	                              public class CandyController(CandyRepository repository) { }
	                              """;

	[Fact]
	public async Task PragmaWarning_CanSuppressTaxonomyDiagnosticId()
	{
		const string source = """
		                      #pragma warning disable ARCH_DEP_001
		                      public class CandyRepository { }
		                      public class CandyController(CandyRepository repository) { }
		                      #pragma warning restore ARCH_DEP_001
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics.Should().NotContain(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.DependencyNotAllowed);
	}

	[Fact]
	public async Task NoWarn_CanSuppressTaxonomyDiagnosticId()
	{
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsWithSuppressedIdsAsync(Source, TestConfigs.DefaultConfig, ArchitecturalDiagnosticIds.DependencyNotAllowed);

		diagnostics.Should().NotContain(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.DependencyNotAllowed);
	}

	[Fact]
	public async Task EditorConfigSeverity_CanSuppressTaxonomyDiagnosticId()
	{
		var severityKey = $"dotnet_diagnostic.{ArchitecturalDiagnosticIds.DependencyNotAllowed}.severity";
		var editorConfigOptions = ImmutableDictionary<string, string>.Empty.Add(severityKey, "none");

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsWithEditorConfigOptionsAsync(Source, TestConfigs.DefaultConfig, editorConfigOptions);

		diagnostics.Should().NotContain(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.DependencyNotAllowed);
		SyntaxFacts.IsValidIdentifier(ArchitecturalDiagnosticIds.DependencyNotAllowed).Should().BeTrue();
	}
}
