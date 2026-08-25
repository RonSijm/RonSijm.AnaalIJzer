using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.SymbolFacts;
using RonSijm.AnaalIJzer.Engine.Visibility;
using AnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;
using RonSijm.AnaalIJzer.Engine.LayerModel;

namespace RonSijm.AnaalIJzer.Analysis.Visibility;

internal static class VisibilityPolicyAnalyzer
{
	internal static void AnalyzeSymbol(SymbolAnalysisContext context, AnalyzerConfig config, ConcurrentDictionary<ISymbol, byte> analyzedSymbols)
	{
		var symbol = NormalizePartialSymbol(context.Symbol);
		if (!analyzedSymbols.TryAdd(symbol, 0)
		    || symbol.IsImplicitlyDeclared
		    || !symbol.TryGetArchitectureDeclarationTarget(out var target)
		    || !symbol.TryGetArchitectureAccessibility(out var accessibility))
		{
			return;
		}

		var ownerType = GetPolicyOwnerType(symbol);
		if (ownerType is null)
		{
			return;
		}

		var layerMatch = config.Engine.FindLayer(ownerType.Name, ownerType.ContainingNamespace?.ToDisplayString() ?? string.Empty, ownerType);
		if (layerMatch is null || layerMatch.Value.Layer.IsForbidden)
		{
			return;
		}

		var evaluation = config.Engine.EvaluateVisibilityPolicies(layerMatch.Value, target, accessibility);
		if (evaluation is null)
		{
			return;
		}

		var location = GetDeclarationLocation(symbol, context.CancellationToken);
		if (location is null)
		{
			return;
		}

		var policy = evaluation.Value.Policy;
		var symbolName = GetDisplayName(symbol);
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitecturalDiagnostics.PropertyCallerTypeName, ownerType.Name)
			.Add(ArchitecturalDiagnostics.PropertyCallerLayerName, layerMatch.Value.Layer.Name)
			.Add(ArchitecturalDiagnostics.PropertyDeclaredSymbolName, symbolName)
			.Add(ArchitecturalDiagnostics.PropertyDeclarationTarget, target.ToString())
			.Add(ArchitecturalDiagnostics.PropertyDeclaredAccessibility, accessibility.ToDisplayText())
			.Add(ArchitecturalDiagnostics.PropertyViolationReason, evaluation.Value.Reason)
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlPath, policy.XmlPath)
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, policy.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture))
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, policy.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture));

		context.ReportDiagnostic(Diagnostic.Create(
			ArchitecturalDiagnostics.VisibilityPolicyViolation,
			location,
			properties,
			symbolName,
			layerMatch.Value.Layer.Name,
			accessibility.ToDisplayText(),
			evaluation.Value.Reason));
	}

	private static ISymbol NormalizePartialSymbol(ISymbol symbol)
	{
		if (symbol is IMethodSymbol method && method.PartialDefinitionPart is not null)
		{
			return method.PartialDefinitionPart;
		}

		return symbol;
	}

	private static INamedTypeSymbol? GetPolicyOwnerType(ISymbol symbol)
	{
		var result = symbol switch
		{
			INamedTypeSymbol namedType => namedType.ContainingType ?? namedType,
			_ => symbol.ContainingType
		};

		return result;
	}

	private static string GetDisplayName(ISymbol symbol)
	{
		var result = symbol is INamedTypeSymbol
			? symbol.Name
			: $"{symbol.ContainingType?.Name}.{symbol.Name}";

		return result;
	}

	private static Location? GetDeclarationLocation(ISymbol symbol, CancellationToken cancellationToken)
	{
		var syntax = symbol.DeclaringSyntaxReferences
			.Select(reference => reference.GetSyntax(cancellationToken))
			.OrderBy(node => node.SyntaxTree.FilePath, StringComparer.Ordinal)
			.ThenBy(node => node.SpanStart)
			.FirstOrDefault();
		if (syntax is null)
		{
			return null;
		}

		var modifierLocation = GetAccessibilityModifierLocation(syntax);
		var result = modifierLocation ?? GetIdentifierLocation(syntax);

		return result;
	}

	private static Location? GetAccessibilityModifierLocation(SyntaxNode syntax)
	{
		var syntaxDeclaration = syntax is VariableDeclaratorSyntax
			? syntax.FirstAncestorOrSelf<BaseFieldDeclarationSyntax>() ?? syntax
			: syntax;
		var modifiers = syntaxDeclaration switch
		{
			BaseTypeDeclarationSyntax declaration => declaration.Modifiers,
			DelegateDeclarationSyntax declaration => declaration.Modifiers,
			BaseMethodDeclarationSyntax declaration => declaration.Modifiers,
			BasePropertyDeclarationSyntax declaration => declaration.Modifiers,
			BaseFieldDeclarationSyntax declaration => declaration.Modifiers,
			_ => default
		};

		foreach (var modifier in modifiers)
		{
			if (modifier.IsKind(SyntaxKind.PublicKeyword)
			    || modifier.IsKind(SyntaxKind.InternalKeyword)
			    || modifier.IsKind(SyntaxKind.ProtectedKeyword)
			    || modifier.IsKind(SyntaxKind.PrivateKeyword)
			    || modifier.IsKind(SyntaxKind.FileKeyword))
			{
				return modifier.GetLocation();
			}
		}

		return null;
	}

	private static Location GetIdentifierLocation(SyntaxNode syntax)
	{
		var result = syntax switch
		{
			BaseTypeDeclarationSyntax declaration => declaration.Identifier.GetLocation(),
			DelegateDeclarationSyntax declaration => declaration.Identifier.GetLocation(),
			MethodDeclarationSyntax declaration => declaration.Identifier.GetLocation(),
			ConstructorDeclarationSyntax declaration => declaration.Identifier.GetLocation(),
			DestructorDeclarationSyntax declaration => declaration.Identifier.GetLocation(),
			OperatorDeclarationSyntax declaration => declaration.OperatorToken.GetLocation(),
			ConversionOperatorDeclarationSyntax declaration => declaration.Type.GetLocation(),
			PropertyDeclarationSyntax declaration => declaration.Identifier.GetLocation(),
			IndexerDeclarationSyntax declaration => declaration.ThisKeyword.GetLocation(),
			EventDeclarationSyntax declaration => declaration.Identifier.GetLocation(),
			VariableDeclaratorSyntax declaration => declaration.Identifier.GetLocation(),
			_ => syntax.GetLocation()
		};

		return result;
	}
}
