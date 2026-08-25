using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.ObservedDependencies;
using RonSijm.AnaalIJzer.Violations;
using AnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;
using RonSijm.AnaalIJzer.Engine.LayerModel;

namespace RonSijm.AnaalIJzer;

internal static partial class LayerDependencyAnalyzer
{
	private static void AnalyzeParameters(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ObservedDependencyCollector? observedDependencies, string callerTypeName, string callerNamespace, SeparatedSyntaxList<ParameterSyntax> parameters, string site)
	{
		var typeDeclaration = parameters.Count > 0 ? parameters[0].FirstAncestorOrSelf<TypeDeclarationSyntax>() : null;
		var callerSymbol = typeDeclaration is null ? null : context.SemanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken) as ITypeSymbol;

		var callerMatch = config.Engine.FindLayer(callerTypeName, callerNamespace, callerSymbol);
		if (callerMatch is null)
		{
			return;
		}

		if (callerMatch.Value.Layer.IsForbidden)
		{
			return;
		}

		foreach (var param in parameters)
		{
			var paramSymbol = context.SemanticModel.GetDeclaredSymbol(param, context.CancellationToken);
			if (paramSymbol is null)
			{
				continue;
			}

			AnalyzeTypeReference(context, config, violations, observedDependencies, callerTypeName, callerMatch.Value, param.GetLocation(), paramSymbol.Type, site);
		}
	}

	private static (string TypeName, LayerMatch Match)? TryGetCallerLayer(SyntaxNodeAnalysisContext context, AnalyzerConfig config, SyntaxNode node)
	{
		var typeDeclaration = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
		if (typeDeclaration is null)
		{
			return null;
		}

		var callerName = typeDeclaration.Identifier.ValueText;
		var callerNs = GetContainingNamespace(typeDeclaration);
		var callerSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken) as ITypeSymbol;
		var match = config.Engine.FindLayer(callerName, callerNs, callerSymbol);
		if (match is null || match.Value.Layer.IsForbidden)
		{
			return null;
		}

		var result = (callerName, match.Value);

		return result;
	}
}
