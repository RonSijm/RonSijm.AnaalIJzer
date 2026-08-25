using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RonSijm.AnaalIJzer.ObservedDependencies;

public static partial class ProjectDependencyScanner
{
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

	private static string GetBaseListDependencySite(TypeDeclarationSyntax typeDeclaration, ITypeSymbol? type)
	{
		var result = type?.TypeKind == TypeKind.Interface && typeDeclaration is not InterfaceDeclarationSyntax
			? DependencySites.InterfaceImplementation
			: DependencySites.Inheritance;

		return result;
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
