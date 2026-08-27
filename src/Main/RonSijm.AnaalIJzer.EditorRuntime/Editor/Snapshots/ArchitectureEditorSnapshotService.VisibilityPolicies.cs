using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.Core.Visibility;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.EditorRuntime.Editor.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static void AddVisibilityPolicyIndicators(SyntaxNode node, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureVisibilityPolicyIndicator>.Builder indicators, HashSet<ISymbol> analyzedSymbols, CancellationToken cancellationToken)
	{
		foreach (var symbol in GetDeclaredSymbols(node, semanticModel, cancellationToken))
		{
			var normalizedSymbol = symbol is IMethodSymbol { PartialDefinitionPart: not null } method
				? method.PartialDefinitionPart
				: symbol;
			if (!analyzedSymbols.Add(normalizedSymbol)
			    || normalizedSymbol.IsImplicitlyDeclared
			    || !normalizedSymbol.TryGetArchitectureDeclarationTarget(out var target)
			    || !normalizedSymbol.TryGetArchitectureAccessibility(out var accessibility))
			{
				continue;
			}

			var ownerType = normalizedSymbol switch
			{
				INamedTypeSymbol namedType => namedType.ContainingType ?? namedType,
				_ => normalizedSymbol.ContainingType
			};
			if (ownerType is null)
			{
				continue;
			}

			var layerMatch = config.Engine.FindLayer(ownerType.Name, ownerType.ContainingNamespace?.ToDisplayString() ?? string.Empty, ownerType);
			if (layerMatch is null || layerMatch.Value.Layer.IsForbidden)
			{
				continue;
			}

			var evaluation = config.Engine.EvaluateVisibilityPolicies(layerMatch.Value, target, accessibility);
			if (evaluation is null)
			{
				continue;
			}

			var span = GetDeclarationIdentifierSpan(node, normalizedSymbol);
			var declarationName = normalizedSymbol is INamedTypeSymbol
				? normalizedSymbol.Name
				: $"{normalizedSymbol.ContainingType?.Name}.{normalizedSymbol.Name}";
			var policy = evaluation.Value.Policy;
			indicators.Add(new ArchitectureVisibilityPolicyIndicator(
				span,
				declarationName,
				target.ToString(),
				accessibility.ToDisplayText(),
				normalizedSymbol.IsEffectivelyExternallyVisible(),
				layerMatch.Value.Layer.Name,
				evaluation.Value.Reason,
				policy.Description,
				policy.XmlPath,
				policy.XmlLineNumber));
		}
	}

	private static IEnumerable<ISymbol> GetDeclaredSymbols(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
	{
		switch (node)
		{
			case BaseTypeDeclarationSyntax:
			case DelegateDeclarationSyntax:
			case BaseMethodDeclarationSyntax:
			case BasePropertyDeclarationSyntax:
				if (semanticModel.GetDeclaredSymbol(node, cancellationToken) is { } symbol)
				{
					yield return symbol;
				}
				break;
			case BaseFieldDeclarationSyntax field:
				foreach (var variable in field.Declaration.Variables)
				{
					if (semanticModel.GetDeclaredSymbol(variable, cancellationToken) is { } fieldSymbol)
					{
						yield return fieldSymbol;
					}
				}
				break;
		}
	}

	private static Microsoft.CodeAnalysis.Text.TextSpan GetDeclarationIdentifierSpan(SyntaxNode node, ISymbol symbol)
	{
		var result = node switch
		{
			BaseTypeDeclarationSyntax declaration => declaration.Identifier.Span,
			DelegateDeclarationSyntax declaration => declaration.Identifier.Span,
			MethodDeclarationSyntax declaration => declaration.Identifier.Span,
			ConstructorDeclarationSyntax declaration => declaration.Identifier.Span,
			DestructorDeclarationSyntax declaration => declaration.Identifier.Span,
			OperatorDeclarationSyntax declaration => declaration.OperatorToken.Span,
			ConversionOperatorDeclarationSyntax declaration => declaration.Type.Span,
			PropertyDeclarationSyntax declaration => declaration.Identifier.Span,
			IndexerDeclarationSyntax declaration => declaration.ThisKeyword.Span,
			EventDeclarationSyntax declaration => declaration.Identifier.Span,
			BaseFieldDeclarationSyntax declaration => declaration.Declaration.Variables.First(variable => string.Equals(variable.Identifier.ValueText, symbol.Name, StringComparison.Ordinal)).Identifier.Span,
			_ => node.Span
		};

		return result;
	}
}
