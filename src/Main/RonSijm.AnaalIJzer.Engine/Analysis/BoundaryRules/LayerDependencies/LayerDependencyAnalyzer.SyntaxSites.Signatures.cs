using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.Core.Observations;
using RonSijm.AnaalIJzer.Core.Violations;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine.Analysis.BoundaryRules.LayerDependencies;

public static partial class LayerDependencyAnalyzer
{
	public static void AnalyzeConstructorDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ObservedDependencyCollector? observedDependencies)
	{
		var ctorDecl = (ConstructorDeclarationSyntax)context.Node;
		if (ctorDecl.Parent is not TypeDeclarationSyntax typeDeclaration)
		{
			return;
		}

		AnalyzeParameters(context, config, violations, observedDependencies, typeDeclaration.Identifier.ValueText, GetContainingNamespace(typeDeclaration), ctorDecl.ParameterList.Parameters, DependencySites.Constructor);
		NamingRules.LayerDependencyAnalyzer.AnalyzeParameterDeclarationNameRules(context, config, violations, ctorDecl.ParameterList.Parameters, DependencySites.Constructor);
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
			NamingRules.LayerDependencyAnalyzer.AnalyzeMethodReturnDeclarationNameRule(context, config, violations, methodDecl);
		}

		if (methodDecl.ParameterList.Parameters.Count > 0)
		{
			AnalyzeParameters(context, config, violations, observedDependencies, typeDeclaration.Identifier.ValueText, GetContainingNamespace(typeDeclaration), methodDecl.ParameterList.Parameters, DependencySites.Method);
			NamingRules.LayerDependencyAnalyzer.AnalyzeParameterDeclarationNameRules(context, config, violations, methodDecl.ParameterList.Parameters, DependencySites.Method);
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
		NamingRules.LayerDependencyAnalyzer.AnalyzeFieldInitializerNameRules(context, config, violations, fieldDecl);
		NamingRules.LayerDependencyAnalyzer.AnalyzeFieldDeclarationNameRules(context, config, violations, fieldDecl);
	}

	public static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ObservedDependencyCollector? observedDependencies)
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
		NamingRules.LayerDependencyAnalyzer.AnalyzePropertyInitializerNameRules(context, config, violations, propDecl);
		NamingRules.LayerDependencyAnalyzer.AnalyzePropertyDeclarationNameRule(context, config, violations, propDecl);
	}
}
