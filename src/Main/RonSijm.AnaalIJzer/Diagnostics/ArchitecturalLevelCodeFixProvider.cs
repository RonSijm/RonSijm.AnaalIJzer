using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace RonSijm.AnaalIJzer.Diagnostics;

/// <summary>
///     Registers two kinds of code fix for architectural diagnostics:
///     <list type="bullet">
///         <item>
///             For ARCH003 with a configured <c>&lt;Fix Rename="…"/&gt;</c>, a solution-wide
///             rename that replaces the matched suffix with the configured fix suffix.
///         </item>
///         <item>
///             For ARCH001/003/004/005, an "Add '<c>TypeName</c>' to exceptions" action that
///             modifies <c>ArchitecturalLevels.xml</c> to whitelist the offending type under
///             the originating rule.
///         </item>
///     </list>
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ArchitecturalLevelCodeFixProvider))]
[Shared]
public sealed class ArchitecturalLevelCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
    [
        ArchitecturalDiagnosticIds.IllegalLevelDependency,
        ArchitecturalDiagnosticIds.ForbiddenDependency,
        ArchitecturalDiagnosticIds.WrongDirectionDependency,
        ArchitecturalDiagnosticIds.SameLayerDependency,
    ];

    // Rename is not batch-safe (each rename changes all references), so no FixAll.
    public override FixAllProvider? GetFixAllProvider()
    {
        return null;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
            {
                await TryRegisterRenameAsync(context, diagnostic).ConfigureAwait(false);
            }

            TryRegisterAddToExceptions(context, diagnostic);
        }
    }

    private static async Task TryRegisterRenameAsync(CodeFixContext context, Diagnostic diagnostic)
    {
        if (!diagnostic.Properties.TryGetValue(ArchitecturalLevelAnalyzer.PropertyMatchedSuffix, out var matchedSuffix)
            || !diagnostic.Properties.TryGetValue(ArchitecturalLevelAnalyzer.PropertyFixSuffix, out var fixSuffix))
        {
            return;
        }

        if (matchedSuffix is null || fixSuffix is null)
        {
            return;
        }

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return;
        }

        // The diagnostic sits on the ParameterSyntax; find the type node inside it.
        var node = root.FindNode(diagnostic.Location.SourceSpan);
        var paramSyntax = node as ParameterSyntax ?? node.Parent as ParameterSyntax;
        if (paramSyntax?.Type is null)
        {
            return;
        }

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var typeSymbol = semanticModel?.GetTypeInfo(paramSyntax.Type, context.CancellationToken).Type;
        if (typeSymbol is null)
        {
            return;
        }

        var oldName = typeSymbol.Name;
        if (!oldName.EndsWith(matchedSuffix, StringComparison.Ordinal))
        {
            return;
        }

        var newName = oldName.Substring(0, oldName.Length - matchedSuffix.Length) + fixSuffix;
        var title = $"Rename '{oldName}' to '{newName}'";

        context.RegisterCodeFix(CodeAction.Create(title, ct => RenameTypeAsync(context.Document, typeSymbol, newName, ct), title), diagnostic);
    }

    private static void TryRegisterAddToExceptions(CodeFixContext context, Diagnostic diagnostic)
    {
        if (!AddToExceptionsCodeFix.TryReadRuleLocation(diagnostic, out var line, out var column, out var depTypeName, out var configPath)
            || depTypeName is null)
        {
            return;
        }

        var configDoc = AddToExceptionsCodeFix.FindConfigDocument(context.Document.Project, configPath);
        if (configDoc is null)
        {
            return;
        }

        var configFileName = string.IsNullOrWhiteSpace(configPath) ? AddToExceptionsCodeFix.ConfigFileName : Path.GetFileName(configPath);
        var title = $"Add '{depTypeName}' to exceptions in {configFileName}";

        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                ct => AddExceptionAsync(context.Document.Project, configDoc.Id, line, column, depTypeName, ct),
                title),
            diagnostic);
    }

    private static async Task<Solution> AddExceptionAsync(Project project, DocumentId configDocId, int line, int column, string depTypeName, CancellationToken cancellationToken)
    {
        var doc = project.Solution.GetAdditionalDocument(configDocId);
        if (doc is null)
        {
            return project.Solution;
        }

        var text = await doc.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var newText = AddToExceptionsCodeFix.AddException(text, line, column, depTypeName);
        return newText is null
            ? project.Solution
            : project.Solution.WithAdditionalDocumentText(configDocId, newText);
    }

    private static async Task<Solution> RenameTypeAsync(Document document, ISymbol typeSymbol, string newName, CancellationToken cancellationToken)
    {
        return await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, new SymbolRenameOptions(), newName, cancellationToken).ConfigureAwait(false);
    }
}
