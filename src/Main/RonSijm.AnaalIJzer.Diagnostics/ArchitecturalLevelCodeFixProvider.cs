using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using RonSijm.AnaalIJzer.Core.Findings;

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
///             modifies <c>Architecture.anl</c> to whitelist the offending type under
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
		ArchitecturalDiagnosticIds.NameRuleViolation,
		ArchitecturalDiagnosticIds.ForbiddenDependency,
		ArchitecturalDiagnosticIds.WrongDirectionDependency,
		ArchitecturalDiagnosticIds.SameLayerDependency,
		ArchitecturalDiagnosticIds.ContractPurityViolation,
		ArchitecturalDiagnosticIds.InheritancePolicyViolation,
	];

    // Rename is not batch-safe (each rename changes all references), so no FixAll.
	public override FixAllProvider? GetFixAllProvider()
	{
		FixAllProvider? result = null;

		return result;
	}

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
			if (diagnostic.Id == ArchitecturalDiagnosticIds.NameRuleViolation)
			{
				await DeclarationNameCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

            if (diagnostic.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
            {
                await TryRegisterRenameAsync(context, diagnostic).ConfigureAwait(false);
            }

			if (diagnostic.Id == ArchitecturalDiagnosticIds.ContractPurityViolation)
			{
				await ContractPurityCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

			if (diagnostic.Id == ArchitecturalDiagnosticIds.InheritancePolicyViolation)
			{
				await InheritancePolicyCodeFix.TryRegisterAsync(context, diagnostic).ConfigureAwait(false);
			}

            await TryRegisterAddToExceptionsAsync(context, diagnostic).ConfigureAwait(false);
        }
    }

    private static async Task TryRegisterAddToExceptionsAsync(CodeFixContext context, Diagnostic diagnostic)
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
        var configText = await configDoc.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
        var requiresReview = AddToExceptionsCodeFix.RequiresExceptionReview(configText);
        var title = requiresReview
            ? $"Add temporary exception requiring review in {configFileName}"
            : $"Add '{depTypeName}' to exceptions in {configFileName}";

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

	private static Task TryRegisterRenameAsync(CodeFixContext context, Diagnostic diagnostic)
	{
		var result = RenameCodeFix.TryRegisterAsync(context, diagnostic);

		return result;
	}
}
