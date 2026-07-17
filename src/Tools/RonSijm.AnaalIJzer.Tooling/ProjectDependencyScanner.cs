using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Diagnostics;

namespace RonSijm.AnaalIJzer.Tooling;

internal static class ProjectDependencyScanner
{
	public static IReadOnlyList<ProjectDependencyObservation> Scan(Compilation compilation, Func<INamedTypeSymbol, string?> resolveLayer, CancellationToken cancellationToken)
	{
		var observations = new List<ProjectDependencyObservation>();
		foreach (var syntaxTree in compilation.SyntaxTrees)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (ApplicationConfigurationGenerator.IsGenerated(syntaxTree, cancellationToken))
			{
				continue;
			}

			var semanticModel = compilation.GetSemanticModel(syntaxTree);
			foreach (var node in syntaxTree.GetRoot(cancellationToken).DescendantNodes())
			{
				switch (node)
				{
					case ConstructorDeclarationSyntax constructor when constructor.Parent is TypeDeclarationSyntax:
						foreach (var parameter in constructor.ParameterList.Parameters)
						{
							AddParameterDependency(parameter, constructor, DependencySites.Constructor, semanticModel, resolveLayer, observations, cancellationToken);
						}
						break;
					case TypeDeclarationSyntax typeDeclaration:
						var parameterList = typeDeclaration switch
						{
							ClassDeclarationSyntax classDeclaration => classDeclaration.ParameterList,
							StructDeclarationSyntax structDeclaration => structDeclaration.ParameterList,
							RecordDeclarationSyntax recordDeclaration => recordDeclaration.ParameterList,
							_ => null
						};
						foreach (var parameter in parameterList?.Parameters ?? [])
						{
							AddParameterDependency(parameter, typeDeclaration, DependencySites.Constructor, semanticModel, resolveLayer, observations, cancellationToken);
						}
						foreach (var baseType in typeDeclaration.BaseList?.Types ?? [])
						{
							var type = semanticModel.GetTypeInfo(baseType.Type, cancellationToken).Type;
							var site = GetBaseListDependencySite(typeDeclaration, type);
							AddTypeDependency(typeDeclaration, type, site, semanticModel, resolveLayer, observations, cancellationToken);
						}
						break;
					case MethodDeclarationSyntax method when method.Parent is TypeDeclarationSyntax:
						AddTypeDependency(method, semanticModel.GetTypeInfo(method.ReturnType, cancellationToken).Type, DependencySites.MethodReturn, semanticModel, resolveLayer, observations, cancellationToken);
						foreach (var parameter in method.ParameterList.Parameters)
						{
							AddParameterDependency(parameter, method, DependencySites.Method, semanticModel, resolveLayer, observations, cancellationToken);
						}
						break;
					case FieldDeclarationSyntax field:
						AddTypeDependency(field, semanticModel.GetTypeInfo(field.Declaration.Type, cancellationToken).Type, DependencySites.Field, semanticModel, resolveLayer, observations, cancellationToken);
						break;
					case PropertyDeclarationSyntax property:
						AddTypeDependency(property, semanticModel.GetTypeInfo(property.Type, cancellationToken).Type, DependencySites.Property, semanticModel, resolveLayer, observations, cancellationToken);
						break;
					case LocalDeclarationStatementSyntax local:
						AddLocalDependencies(local, semanticModel, resolveLayer, observations, cancellationToken);
						break;
					case ObjectCreationExpressionSyntax objectCreation:
						AddTypeDependency(objectCreation, semanticModel.GetTypeInfo(objectCreation, cancellationToken).Type, DependencySites.New, semanticModel, resolveLayer, observations, cancellationToken);
						break;
					case ImplicitObjectCreationExpressionSyntax implicitCreation:
						AddTypeDependency(implicitCreation, semanticModel.GetTypeInfo(implicitCreation, cancellationToken).Type, DependencySites.New, semanticModel, resolveLayer, observations, cancellationToken);
						break;
					case InvocationExpressionSyntax invocation:
						AddInvocationDependencies(invocation, semanticModel, resolveLayer, observations, cancellationToken);
						break;
					case AttributeSyntax attribute:
						AddAttributeDependency(attribute, semanticModel, resolveLayer, observations, cancellationToken);
						break;
					case MemberAccessExpressionSyntax memberAccess:
						AddStaticMemberDependency(memberAccess, semanticModel, resolveLayer, observations, cancellationToken);
						break;
				}
			}
		}

		return observations;
	}

	private static void AddParameterDependency(ParameterSyntax parameter, SyntaxNode callerNode, string site, SemanticModel semanticModel, Func<INamedTypeSymbol, string?> resolveLayer, List<ProjectDependencyObservation> observations, CancellationToken cancellationToken)
	{
		var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter, cancellationToken) as IParameterSymbol;
		AddTypeDependency(callerNode, parameterSymbol?.Type, site, semanticModel, resolveLayer, observations, cancellationToken);
	}

	private static void AddLocalDependencies(LocalDeclarationStatementSyntax local, SemanticModel semanticModel, Func<INamedTypeSymbol, string?> resolveLayer, List<ProjectDependencyObservation> observations, CancellationToken cancellationToken)
	{
		var type = semanticModel.GetTypeInfo(local.Declaration.Type, cancellationToken).Type;
		if (type is not null && type.TypeKind != TypeKind.Error)
		{
			AddTypeDependency(local, type, DependencySites.Local, semanticModel, resolveLayer, observations, cancellationToken);
			return;
		}

		foreach (var variable in local.Declaration.Variables)
		{
			if (semanticModel.GetDeclaredSymbol(variable, cancellationToken) is ILocalSymbol localSymbol)
			{
				AddTypeDependency(local, localSymbol.Type, DependencySites.Local, semanticModel, resolveLayer, observations, cancellationToken);
			}
		}
	}

	private static void AddInvocationDependencies(InvocationExpressionSyntax invocation, SemanticModel semanticModel, Func<INamedTypeSymbol, string?> resolveLayer, List<ProjectDependencyObservation> observations, CancellationToken cancellationToken)
	{
		if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method)
		{
			var staticContainer = method.IsStatic ? method.ContainingType : method.ReducedFrom?.ContainingType;
			if (staticContainer is not null)
			{
				AddTypeDependency(invocation, staticContainer, DependencySites.StaticMember, semanticModel, resolveLayer, observations, cancellationToken);
			}
		}

		var generic = invocation.Expression switch
		{
			MemberAccessExpressionSyntax memberAccess => memberAccess.Name as GenericNameSyntax,
			GenericNameSyntax genericName => genericName,
			_ => null
		};
		if (generic is null)
		{
			return;
		}

		foreach (var argument in generic.TypeArgumentList.Arguments)
		{
			AddTypeDependency(invocation, semanticModel.GetTypeInfo(argument, cancellationToken).Type, DependencySites.GenericInvocation, semanticModel, resolveLayer, observations, cancellationToken);
		}
	}

	private static void AddAttributeDependency(AttributeSyntax attribute, SemanticModel semanticModel, Func<INamedTypeSymbol, string?> resolveLayer, List<ProjectDependencyObservation> observations, CancellationToken cancellationToken)
	{
		if (semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol is IMethodSymbol constructor)
		{
			AddTypeDependency(attribute, constructor.ContainingType, DependencySites.Attribute, semanticModel, resolveLayer, observations, cancellationToken);
		}
	}

	private static void AddStaticMemberDependency(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel, Func<INamedTypeSymbol, string?> resolveLayer, List<ProjectDependencyObservation> observations, CancellationToken cancellationToken)
	{
		var symbol = semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol;
		var containingType = symbol switch
		{
			IPropertySymbol { IsStatic: true } property => property.ContainingType,
			IFieldSymbol { IsStatic: true } field => field.ContainingType,
			IEventSymbol { IsStatic: true } @event => @event.ContainingType,
			_ => null
		};
		if (containingType is not null)
		{
			AddTypeDependency(memberAccess, containingType, DependencySites.StaticMember, semanticModel, resolveLayer, observations, cancellationToken);
		}
	}

	private static string GetBaseListDependencySite(TypeDeclarationSyntax typeDeclaration, ITypeSymbol? type)
	{
		var result = type?.TypeKind == TypeKind.Interface && typeDeclaration is not InterfaceDeclarationSyntax
			? DependencySites.InterfaceImplementation
			: DependencySites.Inheritance;

		return result;
	}

	private static void AddTypeDependency(SyntaxNode callerNode, ITypeSymbol? dependencyType, string site, SemanticModel semanticModel, Func<INamedTypeSymbol, string?> resolveLayer, List<ProjectDependencyObservation> observations, CancellationToken cancellationToken)
	{
		if (dependencyType is null)
		{
			return;
		}

		var callerDeclaration = callerNode.FirstAncestorOrSelf<TypeDeclarationSyntax>();
		if (callerDeclaration is null || semanticModel.GetDeclaredSymbol(callerDeclaration, cancellationToken) is not INamedTypeSymbol callerType)
		{
			return;
		}

		callerType = callerType.OriginalDefinition;
		var callerLayer = resolveLayer(callerType);
		if (callerLayer is null)
		{
			return;
		}

		var index = 0;
		foreach (var currentType in EnumerateTypeAndGenericArguments(dependencyType))
		{
			var effectiveSite = index++ == 0 ? site : DependencySites.GenericArgument;
			if (currentType is not INamedTypeSymbol namedType || namedType.Name == callerType.Name)
			{
				continue;
			}

			namedType = namedType.OriginalDefinition;
			var dependencyLayer = resolveLayer(namedType);
			if (dependencyLayer is null)
			{
				continue;
			}

			observations.Add(new ProjectDependencyObservation(callerType, callerLayer, namedType, dependencyLayer, effectiveSite, callerNode.GetLocation()));
		}
	}

	private static IEnumerable<ITypeSymbol> EnumerateTypeAndGenericArguments(ITypeSymbol root)
	{
		var visited = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
		var stack = new Stack<ITypeSymbol>();
		stack.Push(root);
		while (stack.Count > 0)
		{
			var current = stack.Pop();
			if (!visited.Add(current))
			{
				continue;
			}

			yield return current;
			if (current is INamedTypeSymbol namedType)
			{
				for (var index = namedType.TypeArguments.Length - 1; index >= 0; index--)
				{
					stack.Push(namedType.TypeArguments[index]);
				}
			}
			else if (current is IArrayTypeSymbol arrayType)
			{
				stack.Push(arrayType.ElementType);
			}
		}
	}
}

internal sealed record ProjectDependencyObservation(
	INamedTypeSymbol CallerType,
	string CallerLayer,
	INamedTypeSymbol DependencyType,
	string DependencyLayer,
	string Site,
	Location Location);
