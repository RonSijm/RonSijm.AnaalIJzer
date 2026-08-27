using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.Contracts.Contracts;
using RonSijm.AnaalIJzer.Diagnostics;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.Contracts;

internal static class ContractPurityAnalyzer
{
	internal static void AnalyzeSymbol(SymbolAnalysisContext context, AnalyzerConfig config, ConcurrentDictionary<ISymbol, byte> analyzedSymbols)
	{
		if (context.Symbol is not INamedTypeSymbol typeSymbol
		    || typeSymbol.IsImplicitlyDeclared
		    || !analyzedSymbols.TryAdd(typeSymbol, 0))
		{
			return;
		}

		var layerMatch = config.Engine.FindLayer(typeSymbol.Name, typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty, typeSymbol);
		if (layerMatch is null || layerMatch.Value.Layer.IsForbidden)
		{
			return;
		}

		var syntax = typeSymbol.DeclaringSyntaxReferences
			.Select(reference => reference.GetSyntax(context.CancellationToken))
			.OfType<TypeDeclarationSyntax>()
			.OrderBy(node => node.SyntaxTree.FilePath, StringComparer.Ordinal)
			.ThenBy(node => node.SpanStart)
			.FirstOrDefault();
		if (syntax is null)
		{
			return;
		}

		var typeShape = ContractDeclarationShapeFactory.CreateTypeShape(typeSymbol);
		if (config.Engine.EvaluateContractPolicies(layerMatch.Value, typeShape) is { } typeEvaluation)
		{
			Report(context, layerMatch.Value.Layer.Name, typeShape.DeclaredSymbolName, syntax.Identifier.GetLocation(), typeEvaluation);
			return;
		}

		foreach (var member in typeSymbol.GetMembers().Where(member => SymbolEqualityComparer.Default.Equals(member.ContainingType, typeSymbol)))
		{
			if (member.IsImplicitlyDeclared)
			{
				continue;
			}

			foreach (var candidate in ContractDeclarationShapeFactory.CreateMemberShapes(member, context.CancellationToken))
			{
				var evaluation = config.Engine.EvaluateContractPolicies(layerMatch.Value, candidate.Shape);
				if (evaluation is null)
				{
					continue;
				}

				Report(context, layerMatch.Value.Layer.Name, candidate.Shape.DeclaredSymbolName, candidate.Location, evaluation.Value);
				break;
			}
		}
	}

	private static void Report(SymbolAnalysisContext context, string layerName, string symbolName, Location location, ContractPolicyEvaluation evaluation)
	{
		var policy = evaluation.Policy;
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitecturalDiagnostics.PropertyCallerTypeName, symbolName)
			.Add(ArchitecturalDiagnostics.PropertyCallerLayerName, layerName)
			.Add(ArchitecturalDiagnostics.PropertyDeclaredSymbolName, symbolName)
			.Add(ArchitecturalDiagnostics.PropertyContractViolationKind, evaluation.ViolationKind.ToString())
			.Add(ArchitecturalDiagnostics.PropertyViolationReason, evaluation.Reason)
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlPath, policy.XmlPath)
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, policy.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture))
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, policy.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture));

		context.ReportDiagnostic(Diagnostic.Create(
			ArchitecturalDiagnostics.ContractPurityViolation,
			location,
			properties,
			symbolName,
			layerName,
			evaluation.ViolationKind.ToString(),
			evaluation.Reason));
	}
}
