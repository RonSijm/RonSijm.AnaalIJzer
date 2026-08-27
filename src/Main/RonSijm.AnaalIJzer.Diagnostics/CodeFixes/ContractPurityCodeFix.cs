using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static class ContractPurityCodeFix
{
	private const string DisallowedPropertyAccessor = "DisallowedPropertyAccessor";

	internal static async Task TryRegisterAsync(CodeFixContext context, Diagnostic diagnostic)
	{
		if (!diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyContractViolationKind, out var violationKind)
		    || !string.Equals(violationKind, DisallowedPropertyAccessor, StringComparison.Ordinal)
		    || !diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyContractPropertyAccessor, out var accessorName)
		    || string.IsNullOrWhiteSpace(accessorName))
		{
			return;
		}

		var accessorKind = GetAccessorKind(accessorName!);
		if (accessorKind is null)
		{
			return;
		}

		var normalizedAccessorName = accessorName!;

		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

		if (root is null)
		{
			return;
		}

		var propertyDeclaration = root.FindToken(diagnostic.Location.SourceSpan.Start)
			.Parent?
			.AncestorsAndSelf()
			.OfType<PropertyDeclarationSyntax>()
			.FirstOrDefault();
		if (propertyDeclaration?.AccessorList is null)
		{
			return;
		}

		var accessors = propertyDeclaration.AccessorList.Accessors;
		var accessorDeclaration = accessors.FirstOrDefault(accessor => accessor.Kind() == accessorKind.Value);
		if (accessorDeclaration is null || accessors.Count <= 1 || !accessors.Any(accessor => accessor.Kind() == SyntaxKind.GetAccessorDeclaration))
		{
			return;
		}

		var symbolName = diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyDeclaredSymbolName, out var declaredSymbolName)
			? declaredSymbolName ?? "property"
			: "property";
		var displayAccessorName = char.ToLowerInvariant(normalizedAccessorName[0]) + normalizedAccessorName.Substring(1);
		var title = $"Remove disallowed {displayAccessorName} accessor from '{symbolName}'";
		context.RegisterCodeFix(CodeAction.Create(title, ct => RemoveAccessorAsync(context.Document, propertyDeclaration, accessorDeclaration, ct), title), diagnostic);
	}

	private static SyntaxKind? GetAccessorKind(string accessorName)
	{
		SyntaxKind? result = accessorName switch
		{
			"Set" => SyntaxKind.SetAccessorDeclaration,
			"Init" => SyntaxKind.InitAccessorDeclaration,
			_ => null,
		};

		return result;
	}

	private static async Task<Solution> RemoveAccessorAsync(Document document, PropertyDeclarationSyntax propertyDeclaration, AccessorDeclarationSyntax accessorDeclaration, CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

		if (root is null || propertyDeclaration.AccessorList is null)
		{
			return document.Project.Solution;
		}

		var updatedAccessorList = propertyDeclaration.AccessorList.WithAccessors(propertyDeclaration.AccessorList.Accessors.Remove(accessorDeclaration));
		var updatedPropertyDeclaration = propertyDeclaration.WithAccessorList(updatedAccessorList);
		var updatedRoot = root.ReplaceNode(propertyDeclaration, updatedPropertyDeclaration);
		var result = document.Project.Solution.WithDocumentSyntaxRoot(document.Id, updatedRoot);

		return result;
	}
}
