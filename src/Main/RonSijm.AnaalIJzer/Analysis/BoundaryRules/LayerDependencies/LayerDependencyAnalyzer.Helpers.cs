using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Diagnostics;
using AnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer;

internal static partial class LayerDependencyAnalyzer
{
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

			if (current is INamedTypeSymbol named)
			{
				// Push in reverse so document order is preserved when popping.
				for (var i = named.TypeArguments.Length - 1; i >= 0; i--)
				{
					stack.Push(named.TypeArguments[i]);
				}
			}
			else if (current is IArrayTypeSymbol array)
			{
				stack.Push(array.ElementType);
			}
		}
	}

	/// <summary>
	///     Walks up the syntax tree to find the dotted namespace containing <paramref name="node" />.
	///     Uses syntax only — no semantic model required.
	/// </summary>
	private static string GetContainingNamespace(SyntaxNode node)
	{
		var parts = new List<string>();
		var current = node.Parent;

		while (current is not null)
		{
			if (current is NamespaceDeclarationSyntax nds)
			{
				parts.Add(nds.Name.ToString());
			}
			else if (current is FileScopedNamespaceDeclarationSyntax fsns)
			{
				parts.Add(fsns.Name.ToString());
			}

			current = current.Parent;
		}

		parts.Reverse();
		return string.Join(".", parts);
	}
}
