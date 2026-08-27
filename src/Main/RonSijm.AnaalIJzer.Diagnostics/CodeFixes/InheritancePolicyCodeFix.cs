using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static class InheritancePolicyCodeFix
{
	private const string MissingRequiredBaseType = "MissingRequiredBaseType";
	private const string MissingRequiredInterface = "MissingRequiredInterface";

	internal static async Task TryRegisterAsync(CodeFixContext context, Diagnostic diagnostic)
	{
		if (!diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyInheritanceViolationKind, out var violationKind)
		    || !diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyRequiredInheritanceTypeName, out var requiredTypeName)
		    || string.IsNullOrWhiteSpace(violationKind)
		    || string.IsNullOrWhiteSpace(requiredTypeName))
		{
			return;
		}

		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

		if (root is null)
		{
			return;
		}

		var declaration = root.FindToken(diagnostic.Location.SourceSpan.Start)
			.Parent?
			.AncestorsAndSelf()
			.OfType<TypeDeclarationSyntax>()
			.FirstOrDefault();
		if (declaration is null)
		{
			return;
		}

		var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
		var symbol = semanticModel?.GetDeclaredSymbol(declaration, context.CancellationToken);
		if (symbol is null)
		{
			return;
		}

		var parsedTypeName = SyntaxFactory.ParseTypeName(requiredTypeName!);
		if (parsedTypeName.ContainsDiagnostics)
		{
			return;
		}

		var updatedDeclaration = violationKind switch
		{
			MissingRequiredBaseType => TryAddRequiredBaseType(declaration, symbol, parsedTypeName),
			MissingRequiredInterface => AddBaseType(declaration, parsedTypeName),
			_ => null,
		};
		if (updatedDeclaration is null)
		{
			return;
		}

		var typeKindDescription = string.Equals(violationKind, MissingRequiredInterface, StringComparison.Ordinal)
			? "interface"
			: "base type";
		var title = $"Add required {typeKindDescription} '{requiredTypeName}'";
		context.RegisterCodeFix(CodeAction.Create(title, ct => ApplyDeclarationUpdateAsync(context.Document, declaration, updatedDeclaration, ct), title), diagnostic);
	}

	private static TypeDeclarationSyntax? TryAddRequiredBaseType(TypeDeclarationSyntax declaration, INamedTypeSymbol symbol, TypeSyntax parsedTypeName)
	{
		if (declaration is not (ClassDeclarationSyntax or RecordDeclarationSyntax))
		{
			return null;
		}

		var currentBaseType = symbol.BaseType;
		if (currentBaseType is not null
		    && currentBaseType.SpecialType != SpecialType.System_Object)
		{
			return null;
		}

		var result = declaration.BaseList is null
			? AddBaseType(declaration, parsedTypeName)
			: PrependBaseType(declaration, parsedTypeName);

		return result;
	}

	private static TypeDeclarationSyntax AddBaseType(TypeDeclarationSyntax declaration, TypeSyntax parsedTypeName)
	{
		var baseType = SyntaxFactory.SimpleBaseType(parsedTypeName);
		var baseList = declaration.BaseList is null
			? SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(baseType))
			: declaration.BaseList.WithTypes(declaration.BaseList.Types.Add(baseType));
		var result = declaration.WithBaseList(baseList);

		return result;
	}

	private static TypeDeclarationSyntax PrependBaseType(TypeDeclarationSyntax declaration, TypeSyntax parsedTypeName)
	{
		var baseType = SyntaxFactory.SimpleBaseType(parsedTypeName);
		var baseList = declaration.BaseList!;
		var updatedTypes = baseList.Types.Insert(0, baseType);
		var result = declaration.WithBaseList(baseList.WithTypes(updatedTypes));

		return result;
	}

	private static async Task<Solution> ApplyDeclarationUpdateAsync(Document document, TypeDeclarationSyntax declaration, TypeDeclarationSyntax updatedDeclaration, CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

		if (root is null)
		{
			return document.Project.Solution;
		}

		var updatedRoot = root.ReplaceNode(declaration, updatedDeclaration);
		var result = document.Project.Solution.WithDocumentSyntaxRoot(document.Id, updatedRoot);

		return result;
	}
}
