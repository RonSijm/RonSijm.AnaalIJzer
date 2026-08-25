using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.ObservedDependencies;
using RonSijm.AnaalIJzer.Violations;
using AnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer;

internal static partial class LayerDependencyAnalyzer
{
	internal static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ObservedDependencyCollector? observedDependencies)
	{
		var typeDeclaration = (TypeDeclarationSyntax)context.Node;
		var parameterList = typeDeclaration switch
		{
			ClassDeclarationSyntax classDeclaration => classDeclaration.ParameterList,
			StructDeclarationSyntax structDeclaration => structDeclaration.ParameterList,
			RecordDeclarationSyntax recordDeclaration => recordDeclaration.ParameterList,
			_ => null
		};

		if (parameterList is not null && parameterList.Parameters.Count > 0)
		{
			AnalyzeParameters(context, config, violations, observedDependencies, typeDeclaration.Identifier.ValueText, GetContainingNamespace(typeDeclaration), parameterList.Parameters, DependencySites.Constructor);
			AnalyzeParameterDeclarationNameRules(context, config, violations, parameterList.Parameters, DependencySites.Constructor);
		}

		if (typeDeclaration.BaseList is null)
		{
			return;
		}

		var caller = TryGetCallerLayer(context, config, typeDeclaration);
		if (caller is null)
		{
			return;
		}

		foreach (var baseType in typeDeclaration.BaseList.Types)
		{
			var type = context.SemanticModel.GetTypeInfo(baseType.Type, context.CancellationToken).Type;
			if (type is null)
			{
				continue;
			}

			var site = GetBaseListDependencySite(typeDeclaration, type);
			AnalyzeTypeReference(context, config, violations, observedDependencies, caller.Value.TypeName, caller.Value.Match, baseType.Type.GetLocation(), type, site);
		}
	}

	internal static void AnalyzeAttribute(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ObservedDependencyCollector? observedDependencies)
	{
		var attribute = (AttributeSyntax)context.Node;
		var caller = TryGetCallerLayer(context, config, attribute);
		if (caller is null)
		{
			return;
		}

		if (context.SemanticModel.GetSymbolInfo(attribute, context.CancellationToken).Symbol is not IMethodSymbol constructor)
		{
			return;
		}

		AnalyzeTypeReference(context, config, violations, observedDependencies, caller.Value.TypeName, caller.Value.Match, attribute.Name.GetLocation(), constructor.ContainingType, DependencySites.Attribute);
	}

	private static string GetBaseListDependencySite(TypeDeclarationSyntax typeDeclaration, ITypeSymbol type)
	{
		var result = type.TypeKind == TypeKind.Interface && typeDeclaration is not InterfaceDeclarationSyntax
			? DependencySites.InterfaceImplementation
			: DependencySites.Inheritance;

		return result;
	}
}
