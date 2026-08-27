using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.Inheritance.Policies;
using RonSijm.AnaalIJzer.Diagnostics;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.Inheritance;

internal static class InheritancePolicyAnalyzer
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

		var evaluation = config.Engine.EvaluateInheritancePolicies(layerMatch.Value, typeSymbol);
		if (evaluation is null)
		{
			return;
		}

		var syntax = typeSymbol.DeclaringSyntaxReferences
			.Select(reference => reference.GetSyntax(context.CancellationToken))
			.OfType<BaseTypeDeclarationSyntax>()
			.OrderBy(node => node.SyntaxTree.FilePath, StringComparer.Ordinal)
			.ThenBy(node => node.SpanStart)
			.FirstOrDefault();
		if (syntax is null)
		{
			return;
		}

		Report(context, layerMatch.Value.Layer.Name, typeSymbol.Name, syntax.Identifier.GetLocation(), evaluation.Value);
	}

	private static void Report(SymbolAnalysisContext context, string layerName, string symbolName, Location location, InheritancePolicyEvaluation evaluation)
	{
		var policy = evaluation.Policy;
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitecturalDiagnostics.PropertyCallerTypeName, symbolName)
			.Add(ArchitecturalDiagnostics.PropertyCallerLayerName, layerName)
			.Add(ArchitecturalDiagnostics.PropertyDeclaredSymbolName, symbolName)
			.Add(ArchitecturalDiagnostics.PropertyInheritanceViolationKind, evaluation.ViolationKind.ToString())
			.Add(ArchitecturalDiagnostics.PropertyViolationReason, evaluation.Reason)
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlPath, policy.XmlPath)
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, policy.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture))
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, policy.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture));
		if (evaluation.MissingTypeNames.Length == 1)
		{
			properties = properties.Add(ArchitecturalDiagnostics.PropertyRequiredInheritanceTypeName, evaluation.MissingTypeNames[0]);
		}

		context.ReportDiagnostic(Diagnostic.Create(
			ArchitecturalDiagnostics.InheritancePolicyViolation,
			location,
			properties,
			symbolName,
			layerName,
			evaluation.ViolationKind.ToString(),
			evaluation.Reason));
	}
}
