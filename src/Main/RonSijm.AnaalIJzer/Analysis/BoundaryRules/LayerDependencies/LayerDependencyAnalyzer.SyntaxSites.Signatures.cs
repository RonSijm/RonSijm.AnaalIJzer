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
	internal static void AnalyzeConstructorDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ObservedDependencyCollector? observedDependencies)
	{
		var ctorDecl = (ConstructorDeclarationSyntax)context.Node;
		if (ctorDecl.Parent is not TypeDeclarationSyntax typeDeclaration)
		{
			return;
		}

		AnalyzeParameters(context, config, violations, observedDependencies, typeDeclaration.Identifier.ValueText, GetContainingNamespace(typeDeclaration), ctorDecl.ParameterList.Parameters, DependencySites.Constructor);
		AnalyzeParameterDeclarationNameRules(context, config, violations, ctorDecl.ParameterList.Parameters, DependencySites.Constructor);
	}

	internal static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ObservedDependencyCollector? observedDependencies)
	{
		var methodDecl = (MethodDeclarationSyntax)context.Node;
		if (methodDecl.Parent is not TypeDeclarationSyntax typeDeclaration)
		{
			return;
		}

		var caller = TryGetCallerLayer(context, config, methodDecl);
		if (caller is null)
		{
			return;
		}

		var returnTypeInfo = context.SemanticModel.GetTypeInfo(methodDecl.ReturnType, context.CancellationToken);
		if (returnTypeInfo.Type is not null)
		{
			AnalyzeTypeReference(context, config, violations, observedDependencies, caller.Value.TypeName, caller.Value.Match, methodDecl.ReturnType.GetLocation(), returnTypeInfo.Type, DependencySites.MethodReturn);
			AnalyzeMethodReturnDeclarationNameRule(context, config, violations, methodDecl);
		}

		if (methodDecl.ParameterList.Parameters.Count > 0)
		{
			AnalyzeParameters(context, config, violations, observedDependencies, typeDeclaration.Identifier.ValueText, GetContainingNamespace(typeDeclaration), methodDecl.ParameterList.Parameters, DependencySites.Method);
			AnalyzeParameterDeclarationNameRules(context, config, violations, methodDecl.ParameterList.Parameters, DependencySites.Method);
		}
	}

	internal static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ObservedDependencyCollector? observedDependencies)
	{
		var fieldDecl = (FieldDeclarationSyntax)context.Node;
		var caller = TryGetCallerLayer(context, config, fieldDecl);
		if (caller is null)
		{
			return;
		}

		var typeSyntax = fieldDecl.Declaration.Type;
		var typeInfo = context.SemanticModel.GetTypeInfo(typeSyntax, context.CancellationToken);
		if (typeInfo.Type is null)
		{
			return;
		}

		AnalyzeTypeReference(context, config, violations, observedDependencies, caller.Value.TypeName, caller.Value.Match, typeSyntax.GetLocation(), typeInfo.Type, DependencySites.Field);
		AnalyzeFieldInitializerNameRules(context, config, violations, fieldDecl);
		AnalyzeFieldDeclarationNameRules(context, config, violations, fieldDecl);
	}

	internal static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ObservedDependencyCollector? observedDependencies)
	{
		var propDecl = (PropertyDeclarationSyntax)context.Node;
		var caller = TryGetCallerLayer(context, config, propDecl);
		if (caller is null)
		{
			return;
		}

		var typeInfo = context.SemanticModel.GetTypeInfo(propDecl.Type, context.CancellationToken);
		if (typeInfo.Type is null)
		{
			return;
		}

		AnalyzeTypeReference(context, config, violations, observedDependencies, caller.Value.TypeName, caller.Value.Match, propDecl.Type.GetLocation(), typeInfo.Type, DependencySites.Property);
		AnalyzePropertyInitializerNameRules(context, config, violations, propDecl);
		AnalyzePropertyDeclarationNameRule(context, config, violations, propDecl);
	}
}
