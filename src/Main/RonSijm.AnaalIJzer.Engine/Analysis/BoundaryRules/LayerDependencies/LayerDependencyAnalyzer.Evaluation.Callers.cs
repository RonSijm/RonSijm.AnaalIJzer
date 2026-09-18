using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Core.Observations;
using RonSijm.AnaalIJzer.Core.Violations;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine.Analysis.BoundaryRules.LayerDependencies;

public static partial class LayerDependencyAnalyzer
{
	private static void AnalyzeParameters(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ObservedDependencyCollector? observedDependencies, TypeDeclarationSyntax typeDeclaration, SeparatedSyntaxList<ParameterSyntax> parameters, string site)
	{
		var caller = TryGetCallerContext(context, config, typeDeclaration);
		if (caller is null)
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

			AnalyzeTypeReference(context, config, violations, observedDependencies, caller.Value, param.GetLocation(), paramSymbol.Type, site);
		}
	}

	private static CallerDependencyContext? TryGetCallerContext(SyntaxNodeAnalysisContext context, AnalyzerConfig config, SyntaxNode node)
	{
		var typeDeclaration = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
		if (typeDeclaration is null)
		{
			return null;
		}

		if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken) is not ITypeSymbol callerSymbol)
		{
			return null;
		}

		var callerName = callerSymbol.Name;
		var callerNamespace = callerSymbol.ContainingNamespace?.ToDisplayString() ?? GetContainingNamespace(typeDeclaration);
		var layerMatch = config.Engine.FindLayer(callerName, callerNamespace, callerSymbol);
		if (layerMatch is { } match && match.Layer.IsForbidden)
		{
			return null;
		}

		if (layerMatch is null && !config.HasNamespaceHierarchyPolicies)
		{
			return null;
		}

		var result = new CallerDependencyContext(callerName, callerNamespace, callerSymbol, layerMatch);

		return result;
	}

	public static (string TypeName, LayerMatch Match)? TryGetCallerLayer(SyntaxNodeAnalysisContext context, AnalyzerConfig config, SyntaxNode node)
	{
		var caller = TryGetCallerContext(context, config, node);
		if (caller?.LayerMatch is not { } match)
		{
			return null;
		}

		var result = (caller.Value.TypeName, match);

		return result;
	}
}
