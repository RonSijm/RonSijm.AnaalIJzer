using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Diagnostics;
using RonSijm.AnaalIJzer.Violations;
using AnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer;

internal static partial class LayerDependencyAnalyzer
{
	internal static void AnalyzeConstructorDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var ctorDecl = (ConstructorDeclarationSyntax)context.Node;

		if (ctorDecl.Parent is not TypeDeclarationSyntax typeDeclaration)
		{
			return;
		}

		AnalyzeParameters(context, config, violations, typeDeclaration.Identifier.ValueText, GetContainingNamespace(typeDeclaration), ctorDecl.ParameterList.Parameters, DependencySites.Constructor);
	}

	internal static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
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
			AnalyzeParameters(context, config, violations, typeDeclaration.Identifier.ValueText, GetContainingNamespace(typeDeclaration), parameterList.Parameters, DependencySites.Constructor);
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
			if (type is not null)
			{
				var site = GetBaseListDependencySite(typeDeclaration, type);
				AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, baseType.Type.GetLocation(), type, site);
			}
		}
	}

	private static string GetBaseListDependencySite(TypeDeclarationSyntax typeDeclaration, ITypeSymbol type)
	{
		var result = type.TypeKind == TypeKind.Interface && typeDeclaration is not InterfaceDeclarationSyntax
			? DependencySites.InterfaceImplementation
			: DependencySites.Inheritance;

		return result;
	}

	internal static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
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
			AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, methodDecl.ReturnType.GetLocation(), returnTypeInfo.Type, DependencySites.MethodReturn);
		}

		if (methodDecl.ParameterList.Parameters.Count > 0)
		{
			AnalyzeParameters(context, config, violations, typeDeclaration.Identifier.ValueText, GetContainingNamespace(typeDeclaration), methodDecl.ParameterList.Parameters, DependencySites.Method);
		}
	}

	internal static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
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

		AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, typeSyntax.GetLocation(), typeInfo.Type, DependencySites.Field);
	}

	internal static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
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

		AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, propDecl.Type.GetLocation(), typeInfo.Type, DependencySites.Property);
	}

	internal static void AnalyzeLocalDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var localDecl = (LocalDeclarationStatementSyntax)context.Node;
		var caller = TryGetCallerLayer(context, config, localDecl);
		if (caller is null)
		{
			return;
		}

		var typeSyntax = localDecl.Declaration.Type;
		var typeInfo = context.SemanticModel.GetTypeInfo(typeSyntax, context.CancellationToken);
		if (typeInfo.Type is not null && typeInfo.Type.TypeKind != TypeKind.Error)
		{
			AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, typeSyntax.GetLocation(), typeInfo.Type, DependencySites.Local);
			return;
		}

		foreach (var variable in localDecl.Declaration.Variables)
		{
			if (context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken) is not ILocalSymbol localSymbol || localSymbol.Type.TypeKind == TypeKind.Error)
			{
				continue;
			}

			AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, variable.Identifier.GetLocation(), localSymbol.Type, DependencySites.Local);
		}
	}

	internal static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var node = (ExpressionSyntax)context.Node;
		var caller = TryGetCallerLayer(context, config, node);
		if (caller is null)
		{
			return;
		}

		var typeInfo = context.SemanticModel.GetTypeInfo(node, context.CancellationToken);
		if (typeInfo.Type is null)
		{
			return;
		}

		// Squiggle the type name when it's explicit; otherwise the whole 'new(...)' expression.
		var location = node is ObjectCreationExpressionSyntax oce
			? oce.Type.GetLocation()
			: node.GetLocation();

		AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, location, typeInfo.Type, DependencySites.New);
	}

	internal static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var invocation = (InvocationExpressionSyntax)context.Node;
		var caller = TryGetCallerLayer(context, config, invocation);
		if (caller is null)
		{
			return;
		}

		if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is IMethodSymbol method)
		{
			var staticContainer = method.IsStatic ? method.ContainingType : method.ReducedFrom?.ContainingType;
			if (staticContainer is not null)
			{
				var staticLocation = invocation.Expression is MemberAccessExpressionSyntax memberAccess
					? memberAccess.Expression.GetLocation()
					: invocation.Expression.GetLocation();
				AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, staticLocation, staticContainer, DependencySites.StaticMember);
			}
		}

		// Service-locator style: only generic method invocations carry a type-argument
		// dependency that the caller has not declared elsewhere.
		var generic = invocation.Expression switch
		{
			MemberAccessExpressionSyntax m => m.Name as GenericNameSyntax,
			GenericNameSyntax g => g,
			_ => null,
		};

		if (generic is null || generic.TypeArgumentList.Arguments.Count == 0)
		{
			return;
		}

		foreach (var typeArg in generic.TypeArgumentList.Arguments)
		{
			var typeInfo = context.SemanticModel.GetTypeInfo(typeArg, context.CancellationToken);
			if (typeInfo.Type is null)
			{
				continue;
			}

			AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, typeArg.GetLocation(), typeInfo.Type, DependencySites.GenericInvocation);
		}
	}

	internal static void AnalyzeAttribute(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
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

		AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, attribute.Name.GetLocation(), constructor.ContainingType, DependencySites.Attribute);
	}

	internal static void AnalyzeStaticMemberAccess(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var memberAccess = (MemberAccessExpressionSyntax)context.Node;
		var symbol = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken).Symbol;
		var containingType = symbol switch
		{
			IPropertySymbol { IsStatic: true } property => property.ContainingType,
			IFieldSymbol { IsStatic: true } field => field.ContainingType,
			IEventSymbol { IsStatic: true } @event => @event.ContainingType,
			_ => null
		};
		if (containingType is null)
		{
			return;
		}

		var caller = TryGetCallerLayer(context, config, memberAccess);
		if (caller is null)
		{
			return;
		}

		AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, memberAccess.Expression.GetLocation(), containingType, DependencySites.StaticMember);
	}
}
