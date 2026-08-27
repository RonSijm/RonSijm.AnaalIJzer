using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Engine;

namespace RonSijm.AnaalIJzer.Outputs.Tests.TestSupport;

internal static class AnalyzerOutputTestHelper
{
	private static readonly MetadataReference[] BasicReferences =
	[
		..((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator)
			.Select(path => MetadataReference.CreateFromFile(path))
	];

	internal static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, string? levelConfig = null, string? configPath = null)
	{
		var result = await GetDiagnosticsAsync(
			[("Test.cs", source)],
			levelConfig is null ? [] : [(configPath ?? "Architecture.anl", levelConfig)]);

		return result;
	}

	private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync((string Path, string Source)[] sources, params (string Path, string Content)[] additionalFiles)
	{
		var syntaxTrees = sources
			.Select(source => CSharpSyntaxTree.ParseText(SourceText.From(source.Source), path: source.Path))
			.ToArray();

		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			syntaxTrees,
			BasicReferences,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var analyzerOptions = new AnalyzerOptions(
			[..additionalFiles.Select(file => new TestAdditionalText(file.Path, file.Content))]);

		var diagnostics = await compilation
			.WithAnalyzers([new ArchitecturalLevelAnalyzer()], analyzerOptions)
			.GetAnalyzerDiagnosticsAsync();

		return diagnostics;
	}

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
