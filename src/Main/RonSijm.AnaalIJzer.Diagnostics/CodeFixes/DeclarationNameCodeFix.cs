using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static class DeclarationNameCodeFix
{
	private const string DeclarationNameRuleKind = "RequireDeclarationNameMatchesType";

	internal static async Task TryRegisterAsync(CodeFixContext context, Diagnostic diagnostic)
	{
		if (!diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyNameRuleKind, out var ruleKind)
		    || !string.Equals(ruleKind, DeclarationNameRuleKind, StringComparison.Ordinal)
		    || !diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyTypeName, out var typeName)
		    || !diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyDeclaredName, out var declaredName)
		    || string.IsNullOrWhiteSpace(typeName)
		    || string.IsNullOrWhiteSpace(declaredName)
		    || string.Equals(typeName, declaredName, StringComparison.Ordinal)
		    || !SyntaxFacts.IsValidIdentifier(typeName))
		{
			return;
		}

		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

		if (root is null)
		{
			return;
		}

		var symbol = await FindDeclaredSymbolAsync(context.Document, root, diagnostic.Location.SourceSpan.Start, context.CancellationToken).ConfigureAwait(false);
		if (symbol is null || !string.Equals(symbol.Name, declaredName, StringComparison.Ordinal))
		{
			return;
		}

		var title = $"Rename '{declaredName}' to '{typeName}' to match its type";
		context.RegisterCodeFix(CodeAction.Create(title, ct => RenameSymbolAsync(context.Document, symbol, typeName, ct), title), diagnostic);
	}

	private static async Task<ISymbol?> FindDeclaredSymbolAsync(Document document, SyntaxNode root, int position, CancellationToken cancellationToken)
	{
		var node = root.FindToken(position).Parent;
		var declaration = node?.AncestorsAndSelf().FirstOrDefault(candidate =>
			candidate is ParameterSyntax
			or VariableDeclaratorSyntax
			or MethodDeclarationSyntax
			or PropertyDeclarationSyntax);

		if (declaration is null)
		{
			return null;
		}

		var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		var result = semanticModel?.GetDeclaredSymbol(declaration, cancellationToken);

		return result;
	}

	private static async Task<Solution> RenameSymbolAsync(Document document, ISymbol symbol, string newName, CancellationToken cancellationToken)
	{
		var result = await Renamer.RenameSymbolAsync(document.Project.Solution, symbol, new SymbolRenameOptions(), newName, cancellationToken).ConfigureAwait(false);

		return result;
	}
}
