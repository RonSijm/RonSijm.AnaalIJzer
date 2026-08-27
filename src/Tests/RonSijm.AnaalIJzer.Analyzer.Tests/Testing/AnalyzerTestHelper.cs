using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Engine;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Testing;

public static class AnalyzerTestHelper
{
	private static readonly MetadataReference[] BasicReferences =
	[
		..((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator)
			.Select(path => MetadataReference.CreateFromFile(path))
	];

	public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, string? levelConfig = null)
	{
		var result = await GetDiagnosticsAsync(source, levelConfig, null);

		return result;
	}

	private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, string? levelConfig, string? configPath)
	{
		var result = await GetDiagnosticsAsync(source, levelConfig is null ? [] : [(configPath ?? "Architecture.anl", levelConfig)]);

		return result;
	}

	public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, params (string Path, string Content)[] additionalFiles)
	{
		var result = await GetDiagnosticsAsync([("Test.cs", source)], null, additionalFiles);

		return result;
	}

	public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync((string Path, string Source)[] sources, ImmutableDictionary<string, string>? globalOptions, params (string Path, string Content)[] additionalFiles)
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
			[..additionalFiles.Select(file => new TestAdditionalText(file.Path, file.Content))],
			new TestAnalyzerConfigOptionsProvider(globalOptions));

		var compilationWithAnalyzers = compilation.WithAnalyzers(
			[new ArchitecturalLevelAnalyzer()],
			analyzerOptions);

		var result = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

		return result;
	}

	public static async Task<string> ApplyCodeFixAsync(string source, string levelConfig)
	{
		using var workspace = new AdhocWorkspace();

		var projectId = ProjectId.CreateNewId();
		var documentId = DocumentId.CreateNewId(projectId);

		var solution = workspace.CurrentSolution
			.AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
			.AddMetadataReferences(projectId, BasicReferences)
			.AddDocument(documentId, "Test.cs", source);

		workspace.TryApplyChanges(solution);

		var document = workspace.CurrentSolution.GetDocument(documentId)!;
		var compilation = await document.Project.GetCompilationAsync();

		var additionalTexts = ImmutableArray.Create<AdditionalText>(new TestAdditionalText("Architecture.anl", levelConfig));

		var analyzerOptions = new AnalyzerOptions(additionalTexts);
		var compilationWithAnalyzers = compilation!.WithAnalyzers([new ArchitecturalLevelAnalyzer()], analyzerOptions);

		var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

		var target = diagnostics.FirstOrDefault(d =>
			d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency &&
			d.Properties.ContainsKey(ArchitectureDiagnosticProperties.PropertyMatchedSuffix));

		if (target is null)
		{
			return source;
		}

		var actions = new List<CodeAction>();
		var fixContext = new CodeFixContext(document, target, (action, _) => actions.Add(action), CancellationToken.None);

		var fixer = new ArchitecturalLevelCodeFixProvider();
		await fixer.RegisterCodeFixesAsync(fixContext);

		if (actions.Count == 0)
		{
			return source;
		}

		var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
		var applyOperation = operations.OfType<ApplyChangesOperation>().FirstOrDefault();

		if (applyOperation is null)
		{
			return source;
		}

		var changedSolution = applyOperation.ChangedSolution;
		var changedDocument = changedSolution.GetDocument(documentId)!;
		var changedText = await changedDocument.GetTextAsync();
		var result = changedText.ToString();

		return result;
	}

	public static async Task<string> ApplyAddToExceptionsCodeFixAsync(string source, string levelConfig, string targetDiagnosticId)
	{
		var result = await ApplyAddToExceptionsCodeFixAsync(source, [("Architecture.anl", levelConfig)], targetDiagnosticId, "Architecture.anl");

		return result;
	}

	public static async Task<string> ApplyAddToExceptionsCodeFixAsync(string source, (string Path, string Content)[] configs, string targetDiagnosticId, string updatedConfigPath)
	{
		using var workspace = new AdhocWorkspace();

		var projectId = ProjectId.CreateNewId();
		var documentId = DocumentId.CreateNewId(projectId);
		var configDocIds = configs.ToDictionary(config => config.Path, _ => DocumentId.CreateNewId(projectId), StringComparer.OrdinalIgnoreCase);

		var solution = workspace.CurrentSolution
			.AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
			.AddMetadataReferences(projectId, BasicReferences)
			.AddDocument(documentId, "Test.cs", source);

		foreach (var config in configs)
		{
			solution = solution.AddAdditionalDocument(DocumentInfo.Create(configDocIds[config.Path], name: Path.GetFileName(config.Path), filePath: config.Path, loader: TextLoader.From(TextAndVersion.Create(SourceText.From(config.Content), VersionStamp.Create()))));
		}

		workspace.TryApplyChanges(solution);

		var document = workspace.CurrentSolution.GetDocument(documentId)!;
		var project = document.Project;

		var compilation = await project.GetCompilationAsync();
		var compilationWithAnalyzers = compilation!.WithAnalyzers(
			[new ArchitecturalLevelAnalyzer()], project.AnalyzerOptions);

		var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

		var target = diagnostics.FirstOrDefault(d =>
			d.Id == targetDiagnosticId
			&& d.Properties.ContainsKey(ArchitectureDiagnosticProperties.PropertyRuleXmlLine));

		if (target is null)
		{
			throw new InvalidOperationException($"No {targetDiagnosticId} diagnostic with rule location was produced.");
		}

		var actions = new List<CodeAction>();
		var fixContext = new CodeFixContext(document, target, (action, _) => actions.Add(action), CancellationToken.None);
		await new ArchitecturalLevelCodeFixProvider().RegisterCodeFixesAsync(fixContext);

		var addAction = actions.FirstOrDefault(a => a.Title.StartsWith("Add '", StringComparison.Ordinal) || a.Title.StartsWith("Add temporary exception requiring review", StringComparison.Ordinal))
			?? throw new InvalidOperationException("No 'Add to exceptions' code action registered. Got: " + string.Join(", ", actions.Select(a => a.Title)));

		var operations = await addAction.GetOperationsAsync(CancellationToken.None);
		var apply = operations.OfType<ApplyChangesOperation>().Single();

		var changedDoc = apply.ChangedSolution.GetAdditionalDocument(configDocIds[updatedConfigPath])
			?? throw new InvalidOperationException("AdditionalDocument missing after applying fix.");

		var changedText = await changedDoc.GetTextAsync();
		var result = changedText.ToString();

		return result;
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

	private sealed class TestAnalyzerConfigOptionsProvider(ImmutableDictionary<string, string>? globalOptions) : AnalyzerConfigOptionsProvider
	{
		private static readonly AnalyzerConfigOptions EmptyOptions = new TestAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty);
		private readonly AnalyzerConfigOptions _globalOptions = new TestAnalyzerConfigOptions(globalOptions ?? ImmutableDictionary<string, string>.Empty);

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
