using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static class RenameCodeFix
{
	internal static async Task TryRegisterAsync(CodeFixContext context, Diagnostic diagnostic)
	{
		if (!diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyMatchedSuffix, out var matchedSuffix)
		    || !diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyFixSuffix, out var fixSuffix)
		    || matchedSuffix is null
		    || fixSuffix is null)
		{
			return;
		}

		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

		if (root is null)
		{
			return;
		}

		var node = root.FindNode(diagnostic.Location.SourceSpan);
		var paramSyntax = node as ParameterSyntax ?? node.Parent as ParameterSyntax;
		if (paramSyntax?.Type is null)
		{
			return;
		}

		var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
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

	private static async Task<Solution> RenameTypeAsync(Document document, ISymbol typeSymbol, string newName, CancellationToken cancellationToken)
	{
		var result = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, new SymbolRenameOptions(), newName, cancellationToken).ConfigureAwait(false);

		return result;
	}
}
