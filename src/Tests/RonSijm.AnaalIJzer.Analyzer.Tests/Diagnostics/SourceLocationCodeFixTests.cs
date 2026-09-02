using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Engine;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

public sealed class SourceLocationCodeFixTests
{
	private static readonly MetadataReference[] BasicReferences =
	[
		..((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator)
			.Select(path => MetadataReference.CreateFromFile(path))
	];

	[Fact]
	public async Task ProjectRelativeSourceLocation_AddsExactSourceRuleToConfiguration()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Ordering">
			    <Class endsWith="Service" />
			    <SourceLocations>
			      <Source startsWith="Ordering/" />
			    </SourceLocations>
			  </Layer>
			</ArchitecturalLevels>
			""";
		var globalOptions = ImmutableDictionary<string, string>.Empty.Add("build_property.MSBuildProjectDirectory", @"D:\repo\Shop");

		var result = await ApplyConfigurationCodeFixAsync(
			(@"D:\repo\Shop\Infrastructure\CandyService.cs", "public class CandyService { }"),
			("Architecture.anl", config),
			globalOptions,
			"Add source location 'Infrastructure/CandyService.cs' to layer 'Ordering'");

		result.Should().Contain("<Source exactName=\"Infrastructure/CandyService.cs\" />");
	}

	[Fact]
	public async Task ProjectRelativeSourceLocation_InlineSettings_UpdatesAssemblyMetadata()
	{
		const string source = """"
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", """
			<ArchitecturalLevels>
			  <Layer name="Ordering">
			    <Class endsWith="Service" />
			    <SourceLocations>
			      <Source startsWith="Ordering/" />
			    </SourceLocations>
			  </Layer>
			</ArchitecturalLevels>
			""")]

			public class CandyService { }
			"""";
		var globalOptions = ImmutableDictionary<string, string>.Empty.Add("build_property.MSBuildProjectDirectory", @"D:\repo\Shop");

		var result = await ApplySourceCodeFixAsync(
			(@"D:\repo\Shop\Infrastructure\CandyService.cs", source),
			globalOptions,
			"Add source location 'Infrastructure/CandyService.cs' to layer 'Ordering'");

		result.Should().Contain("<Source exactName=\"Infrastructure/CandyService.cs\" />");
	}

	private static async Task<string> ApplyConfigurationCodeFixAsync((string Path, string Source) source, (string Path, string Content) config, ImmutableDictionary<string, string> globalOptions, string titlePrefix)
	{
		using var workspace = new AdhocWorkspace();

		var projectId = ProjectId.CreateNewId();
		var documentId = DocumentId.CreateNewId(projectId);
		var configDocumentId = DocumentId.CreateNewId(projectId);

		var solution = workspace.CurrentSolution
			.AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
			.AddMetadataReferences(projectId, BasicReferences)
			.AddDocument(DocumentInfo.Create(documentId, name: Path.GetFileName(source.Path), filePath: source.Path, loader: TextLoader.From(TextAndVersion.Create(SourceText.From(source.Source), VersionStamp.Create()))))
			.AddAdditionalDocument(DocumentInfo.Create(configDocumentId, name: Path.GetFileName(config.Path), filePath: config.Path, loader: TextLoader.From(TextAndVersion.Create(SourceText.From(config.Content), VersionStamp.Create()))));

		workspace.TryApplyChanges(solution);

		var document = workspace.CurrentSolution.GetDocument(documentId)!;
		var actions = await GetCodeFixActionsAsync(document, globalOptions, ArchitecturalDiagnosticIds.SourceLocationViolation);
		var action = actions.First(candidate => candidate.Title.StartsWith(titlePrefix, StringComparison.Ordinal));
		var updatedSolution = await ApplyCodeFixAsync(action);
		var updatedDocument = updatedSolution.GetAdditionalDocument(configDocumentId)!;
		var updatedText = await updatedDocument.GetTextAsync();
		var result = updatedText.ToString();

		return result;
	}

	private static async Task<string> ApplySourceCodeFixAsync((string Path, string Source) source, ImmutableDictionary<string, string> globalOptions, string titlePrefix)
	{
		using var workspace = new AdhocWorkspace();

		var projectId = ProjectId.CreateNewId();
		var documentId = DocumentId.CreateNewId(projectId);

		var solution = workspace.CurrentSolution
			.AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
			.AddMetadataReferences(projectId, BasicReferences)
			.AddDocument(DocumentInfo.Create(documentId, name: Path.GetFileName(source.Path), filePath: source.Path, loader: TextLoader.From(TextAndVersion.Create(SourceText.From(source.Source), VersionStamp.Create()))));

		workspace.TryApplyChanges(solution);

		var document = workspace.CurrentSolution.GetDocument(documentId)!;
		var actions = await GetCodeFixActionsAsync(document, globalOptions, ArchitecturalDiagnosticIds.SourceLocationViolation);
		var action = actions.First(candidate => candidate.Title.StartsWith(titlePrefix, StringComparison.Ordinal));
		var updatedSolution = await ApplyCodeFixAsync(action);
		var updatedDocument = updatedSolution.GetDocument(documentId)!;
		var updatedText = await updatedDocument.GetTextAsync();
		var result = updatedText.ToString();

		return result;
	}

	private static async Task<List<CodeAction>> GetCodeFixActionsAsync(Document document, ImmutableDictionary<string, string> globalOptions, string targetDiagnosticId)
	{
		var compilation = await document.Project.GetCompilationAsync();
		var analyzerOptions = new AnalyzerOptions(
			[..document.Project.AnalyzerOptions.AdditionalFiles],
			new TestAnalyzerConfigOptionsProvider(globalOptions));
		var diagnostics = await compilation!.WithAnalyzers([new ArchitecturalLevelAnalyzer()], analyzerOptions).GetAnalyzerDiagnosticsAsync();
		var target = diagnostics.First(diagnostic => diagnostic.Id == targetDiagnosticId);
		var actions = new List<CodeAction>();
		var fixContext = new CodeFixContext(document, target, (action, _) => actions.Add(action), CancellationToken.None);

		await new ArchitecturalLevelCodeFixProvider().RegisterCodeFixesAsync(fixContext);

		return actions;
	}

	private static async Task<Solution> ApplyCodeFixAsync(CodeAction action)
	{
		var operations = await action.GetOperationsAsync(CancellationToken.None);
		var applyOperation = operations.OfType<ApplyChangesOperation>().First();
		var result = applyOperation.ChangedSolution;

		return result;
	}

	private sealed class TestAnalyzerConfigOptionsProvider(ImmutableDictionary<string, string> globalOptions) : AnalyzerConfigOptionsProvider
	{
		private static readonly AnalyzerConfigOptions EmptyOptions = new TestAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty);
		private readonly AnalyzerConfigOptions _globalOptions = new TestAnalyzerConfigOptions(globalOptions);

		public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
		{
			return EmptyOptions;
		}

		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
		{
			return EmptyOptions;
		}
	}

	private sealed class TestAnalyzerConfigOptions(ImmutableDictionary<string, string> values) : AnalyzerConfigOptions
	{
		public override bool TryGetValue(string key, out string value)
		{
			var result = values.TryGetValue(key, out value!);

			return result;
		}
	}
}
