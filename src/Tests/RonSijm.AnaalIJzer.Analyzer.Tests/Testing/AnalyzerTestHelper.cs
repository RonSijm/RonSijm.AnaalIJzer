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
		var result = await ApplySelectedCodeFixAsync(
			source,
			levelConfig,
			ArchitecturalDiagnosticIds.ForbiddenDependency,
			action => action.Title.StartsWith("Rename '", StringComparison.Ordinal));

		return result;
	}

	public static async Task<string> ApplyCodeFixAsync(string source, string levelConfig, string targetDiagnosticId, string titlePrefix)
	{
		var result = await ApplySelectedCodeFixAsync(
			source,
			levelConfig,
			targetDiagnosticId,
			action => action.Title.StartsWith(titlePrefix, StringComparison.Ordinal));

		return result;
	}

	public static async Task<string> ApplyCodeFixAsync(string source, string targetDiagnosticId, string titlePrefix)
	{
		var result = await ApplySelectedCodeFixAsync(
			source,
			[],
			targetDiagnosticId,
			action => action.Title.StartsWith(titlePrefix, StringComparison.Ordinal));

		return result;
	}

	public static async Task<string> ApplyCodeFixAsync(string source, (string Path, string Content)[] additionalFiles, string targetDiagnosticId, string titlePrefix)
	{
		var result = await ApplySelectedCodeFixAsync(
			source,
			additionalFiles,
			targetDiagnosticId,
			action => action.Title.StartsWith(titlePrefix, StringComparison.Ordinal));

		return result;
	}

	public static async Task<IReadOnlyList<string>> GetCodeFixTitlesAsync(string source, string levelConfig, string targetDiagnosticId)
	{
		var actions = await GetCodeFixActionsAsync(source, [("Architecture.anl", levelConfig)], targetDiagnosticId);
		var result = actions.Select(action => action.Title).ToArray();

		return result;
	}

	public static async Task<IReadOnlyList<string>> GetCodeFixTitlesAsync(string source, string targetDiagnosticId)
	{
		var actions = await GetCodeFixActionsAsync(source, [], targetDiagnosticId);
		var result = actions.Select(action => action.Title).ToArray();

		return result;
	}

	public static async Task<IReadOnlyList<string>> GetCodeFixTitlesAsync(string source, (string Path, string Content)[] additionalFiles, string targetDiagnosticId)
	{
		var actions = await GetCodeFixActionsAsync(source, additionalFiles, targetDiagnosticId);
		var result = actions.Select(action => action.Title).ToArray();

		return result;
	}

	public static async Task<string> ApplyAddToExceptionsCodeFixAsync(string source, string levelConfig, string targetDiagnosticId)
	{
		var result = await ApplyAddToExceptionsCodeFixAsync(source, [("Architecture.anl", levelConfig)], targetDiagnosticId, "Architecture.anl");

		return result;
	}

	public static async Task<string> ApplyConfigurationCodeFixAsync(string source, string levelConfig, string targetDiagnosticId, string titlePrefix, string updatedConfigPath = "Architecture.anl")
	{
		var result = await ApplyConfigurationCodeFixAsync(source, [("Architecture.anl", levelConfig)], targetDiagnosticId, titlePrefix, updatedConfigPath);

		return result;
	}

	public static async Task<string> ApplyConfigurationCodeFixAsync(string source, (string Path, string Content)[] configs, string targetDiagnosticId, string titlePrefix, string updatedConfigPath)
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
		var actions = await GetCodeFixActionsAsync(document, targetDiagnosticId);
		var action = actions.FirstOrDefault(candidate => candidate.Title.StartsWith(titlePrefix, StringComparison.Ordinal))
		             ?? throw new InvalidOperationException("No matching configuration code fix registered. Got: " + string.Join(", ", actions.Select(candidate => candidate.Title)));
		var operations = await action.GetOperationsAsync(CancellationToken.None);
		var applyOperation = operations.OfType<ApplyChangesOperation>().FirstOrDefault()
		                     ?? throw new InvalidOperationException("Selected configuration code fix did not produce an ApplyChangesOperation.");
		var changedDocument = applyOperation.ChangedSolution.GetAdditionalDocument(configDocIds[updatedConfigPath])
		                     ?? throw new InvalidOperationException("Updated configuration document was not found after applying the code fix.");
		var changedText = await changedDocument.GetTextAsync();
		var result = changedText.ToString();

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

	private static async Task<string> ApplySelectedCodeFixAsync(string source, string levelConfig, string targetDiagnosticId, Func<CodeAction, bool> selector)
	{
		var result = await ApplySelectedCodeFixAsync(source, [("Architecture.anl", levelConfig)], targetDiagnosticId, selector);

		return result;
	}

	private static async Task<string> ApplySelectedCodeFixAsync(string source, (string Path, string Content)[] configs, string targetDiagnosticId, Func<CodeAction, bool> selector)
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
		var actions = await GetCodeFixActionsAsync(document, targetDiagnosticId);
		var action = actions.FirstOrDefault(selector);

		if (action is null)
		{
			return source;
		}

		var operations = await action.GetOperationsAsync(CancellationToken.None);
		var applyOperation = operations.OfType<ApplyChangesOperation>().FirstOrDefault();

		if (applyOperation is null)
		{
			return source;
		}

		var changedDocument = applyOperation.ChangedSolution.GetDocument(documentId)!;
		var changedText = await changedDocument.GetTextAsync();
		var result = changedText.ToString();

		return result;
	}

	private static async Task<List<CodeAction>> GetCodeFixActionsAsync(string source, (string Path, string Content)[] configs, string targetDiagnosticId)
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
		var result = await GetCodeFixActionsAsync(document, targetDiagnosticId);

		return result;
	}

	private static async Task<List<CodeAction>> GetCodeFixActionsAsync(Document document, string targetDiagnosticId)
	{
		var project = document.Project;
		var compilation = await project.GetCompilationAsync();
		var compilationWithAnalyzers = compilation!.WithAnalyzers([new ArchitecturalLevelAnalyzer()], project.AnalyzerOptions);
		var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
		var target = diagnostics.FirstOrDefault(diagnostic => diagnostic.Id == targetDiagnosticId)
		             ?? throw new InvalidOperationException($"No {targetDiagnosticId} diagnostic was produced.");
		var actions = new List<CodeAction>();
		var fixContext = new CodeFixContext(document, target, (action, _) => actions.Add(action), CancellationToken.None);

		await new ArchitecturalLevelCodeFixProvider().RegisterCodeFixesAsync(fixContext);

		return actions;
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
