using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace RonSijm.AnaalIJzer.UnitTests;

internal static class AnalyzerTestHelper
{
    private static readonly MetadataReference[] BasicReferences =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(path => MetadataReference.CreateFromFile(path))
        .ToArray<MetadataReference>();

    internal static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, string? levelConfig = null)
    {
        return await GetDiagnosticsAsync(source, levelConfig, null);
    }

    internal static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, string? levelConfig, string? configPath)
    {
        return await GetDiagnosticsAsync(source, levelConfig is null ? [] : [(configPath ?? "ArchitecturalLevels.xml", levelConfig)]);
    }

    internal static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, params (string Path, string Content)[] additionalFiles)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            BasicReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzerOptions = new AnalyzerOptions([..additionalFiles.Select(file => new TestAdditionalText(file.Path, file.Content))]);

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            [new ArchitecturalLevelAnalyzer()],
            analyzerOptions);

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    /// <summary>
    ///     Runs the analyzer against <paramref name="source" />, finds the first ARCH003 diagnostic
    ///     that has rename properties, applies the <see cref="ArchitecturalLevelCodeFixProvider" />,
    ///     and returns the resulting source text.
    /// </summary>
    internal static async Task<string> ApplyCodeFixAsync(string source, string levelConfig)
    {
        // Build a real workspace so Renamer can update all references.
        using var workspace = new AdhocWorkspace();

        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var solution = workspace.CurrentSolution
            .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
            .AddMetadataReferences(projectId, BasicReferences)
            .AddDocument(documentId, "Test.cs", source);

        workspace.TryApplyChanges(solution);

        var document = workspace.CurrentSolution.GetDocument(documentId)!;

        // Compile and analyse.
        var compilation = await document.Project.GetCompilationAsync();
        
        var additionalTexts = ImmutableArray.Create<AdditionalText>(new TestAdditionalText("ArchitecturalLevels.xml", levelConfig));

        var analyzerOptions = new AnalyzerOptions(additionalTexts);
        var compilationWithAnalyzers = compilation!.WithAnalyzers([new ArchitecturalLevelAnalyzer()], analyzerOptions);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        var target = diagnostics.FirstOrDefault(d =>
            d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency &&
            d.Properties.ContainsKey(ArchitecturalLevelAnalyzer.PropertyMatchedSuffix));

        if (target is null)
        {
            return source;
        }

        // Let the code fix provider register actions.
        var actions = new List<CodeAction>();
        var fixContext = new CodeFixContext(document, target, (action, _) => actions.Add(action), CancellationToken.None);

        var fixer = new ArchitecturalLevelCodeFixProvider();
        await fixer.RegisterCodeFixesAsync(fixContext);

        if (actions.Count == 0)
        {
            return source;
        }

        // Apply the first action.
        var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        var applyOperation = operations.OfType<ApplyChangesOperation>().FirstOrDefault();

        if (applyOperation is null)
        {
            return source;
        }

        var changedSolution = applyOperation.ChangedSolution;
        var changedDocument = changedSolution.GetDocument(documentId)!;
        var changedText = await changedDocument.GetTextAsync();

        return changedText.ToString();
    }

    /// <summary>
    ///     Runs the analyzer against <paramref name="source" /> with <paramref name="levelConfig" />
    ///     wired as a real <c>AdditionalDocument</c>, picks the first <paramref name="targetDiagnosticId" />
    ///     diagnostic that carries a rule location, applies the "Add to exceptions" code action,
    ///     and returns the updated XML text of <c>ArchitecturalLevels.xml</c>.
    /// </summary>
    internal static async Task<string> ApplyAddToExceptionsCodeFixAsync(string source, string levelConfig, string targetDiagnosticId) =>
        await ApplyAddToExceptionsCodeFixAsync(source, [("ArchitecturalLevels.xml", levelConfig)], targetDiagnosticId, "ArchitecturalLevels.xml");

    internal static async Task<string> ApplyAddToExceptionsCodeFixAsync(string source, (string Path, string Content)[] configs, string targetDiagnosticId, string updatedConfigPath)
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
            && d.Properties.ContainsKey(ArchitecturalDiagnostics.PropertyRuleXmlLine));

        if (target is null)
        {
            throw new InvalidOperationException($"No {targetDiagnosticId} diagnostic with rule location was produced.");
        }

        var actions = new List<CodeAction>();
        var fixContext = new CodeFixContext(document, target, (action, _) => actions.Add(action), CancellationToken.None);
        await new ArchitecturalLevelCodeFixProvider().RegisterCodeFixesAsync(fixContext);

        var addAction = actions.FirstOrDefault(a => a.Title.StartsWith("Add '", StringComparison.Ordinal))
            ?? throw new InvalidOperationException("No 'Add to exceptions' code action registered. Got: " + string.Join(", ", actions.Select(a => a.Title)));

        var operations = await addAction.GetOperationsAsync(CancellationToken.None);
        var apply = operations.OfType<ApplyChangesOperation>().Single();

        var changedDoc = apply.ChangedSolution.GetAdditionalDocument(configDocIds[updatedConfigPath])
            ?? throw new InvalidOperationException("AdditionalDocument missing after applying fix.");

        var changedText = await changedDoc.GetTextAsync();
        return changedText.ToString();
    }

    private sealed class TestAdditionalText(string path, string content) : AdditionalText
    {
        private readonly SourceText _text = SourceText.From(content);

        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return _text;
        }
    }
}
