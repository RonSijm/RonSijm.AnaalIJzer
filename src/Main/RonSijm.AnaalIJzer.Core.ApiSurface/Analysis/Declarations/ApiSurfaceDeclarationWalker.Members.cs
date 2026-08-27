using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Model;
using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Declarations;

internal static partial class ApiSurfaceDeclarationWalker
{
	private static IEnumerable<ApiSurfaceTypeReference> GetMethodReferences(IMethodSymbol method, CancellationToken cancellationToken)
	{
		var syntax = method.DeclaringSyntaxReferences
			.Select(reference => reference.GetSyntax(cancellationToken))
			.OrderBy(node => node.SyntaxTree.FilePath, StringComparer.Ordinal)
			.ThenBy(node => node.SpanStart)
			.FirstOrDefault();
		if (syntax is null)
		{
			yield break;
		}

		var parameterList = syntax switch
		{
			BaseMethodDeclarationSyntax declaration => declaration.ParameterList,
			DelegateDeclarationSyntax declaration => declaration.ParameterList,
			_ => null
		};
		var parameterSite = method.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor
			? DependencySites.Constructor
			: DependencySites.Method;
		if (parameterList is not null)
		{
			foreach (var reference in GetParameterReferences(method.Parameters, parameterList.Parameters, parameterSite))
			{
				yield return reference;
			}
		}

		var returnTypeSyntax = syntax switch
		{
			MethodDeclarationSyntax declaration => declaration.ReturnType,
			OperatorDeclarationSyntax declaration => declaration.ReturnType,
			ConversionOperatorDeclarationSyntax declaration => declaration.Type,
			_ => null
		};
		if (returnTypeSyntax is not null && !method.ReturnsVoid)
		{
			foreach (var reference in ExpandType(method.ReturnType, returnTypeSyntax, DependencySites.MethodReturn))
			{
				yield return reference;
			}
		}
	}

	private static IEnumerable<ApiSurfaceTypeReference> GetPropertyReferences(IPropertySymbol property, CancellationToken cancellationToken)
	{
		var syntax = property.DeclaringSyntaxReferences
			.Select(reference => reference.GetSyntax(cancellationToken))
			.OrderBy(node => node.SyntaxTree.FilePath, StringComparer.Ordinal)
			.ThenBy(node => node.SpanStart)
			.FirstOrDefault();
		var typeSyntax = syntax switch
		{
			PropertyDeclarationSyntax declaration => declaration.Type,
			IndexerDeclarationSyntax declaration => declaration.Type,
			_ => null
		};
		if (typeSyntax is null)
		{
			yield break;
		}

		foreach (var reference in ExpandType(property.Type, typeSyntax, DependencySites.Property))
		{
			yield return reference;
		}

		if (syntax is IndexerDeclarationSyntax indexer)
		{
			foreach (var reference in GetParameterReferences(property.Parameters, indexer.ParameterList.Parameters, DependencySites.Property))
			{
				yield return reference;
			}
		}
	}

	private static IEnumerable<ApiSurfaceTypeReference> GetFieldReferences(IFieldSymbol field, CancellationToken cancellationToken)
	{
		var syntax = field.DeclaringSyntaxReferences
			.Select(reference => reference.GetSyntax(cancellationToken))
			.OrderBy(node => node.SyntaxTree.FilePath, StringComparer.Ordinal)
			.ThenBy(node => node.SpanStart)
			.FirstOrDefault();
		var typeSyntax = syntax?.FirstAncestorOrSelf<FieldDeclarationSyntax>()?.Declaration.Type;
		if (typeSyntax is null)
		{
			yield break;
		}

		foreach (var reference in ExpandType(field.Type, typeSyntax, DependencySites.Field))
		{
			yield return reference;
		}
	}

	private static IEnumerable<ApiSurfaceTypeReference> GetEventReferences(IEventSymbol eventSymbol, CancellationToken cancellationToken)
	{
		var syntax = eventSymbol.DeclaringSyntaxReferences
			.Select(reference => reference.GetSyntax(cancellationToken))
			.OrderBy(node => node.SyntaxTree.FilePath, StringComparer.Ordinal)
			.ThenBy(node => node.SpanStart)
			.FirstOrDefault();
		var typeSyntax = syntax switch
		{
			EventDeclarationSyntax declaration => declaration.Type,
			VariableDeclaratorSyntax declarator => declarator.FirstAncestorOrSelf<EventFieldDeclarationSyntax>()?.Declaration.Type,
			_ => null
		};
		if (typeSyntax is null)
		{
			yield break;
		}

		foreach (var reference in ExpandType(eventSymbol.Type, typeSyntax, DependencySites.Field))
		{
			yield return reference;
		}
	}

	private static IEnumerable<ApiSurfaceTypeReference> GetParameterReferences(ImmutableArray<IParameterSymbol> symbols, SeparatedSyntaxList<ParameterSyntax> syntax, string site)
	{
		for (var index = 0; index < symbols.Length && index < syntax.Count; index++)
		{
			if (syntax[index].Type is null)
			{
				continue;
			}

			foreach (var reference in ExpandType(symbols[index].Type, syntax[index].Type!, site))
			{
				yield return reference;
			}
		}
	}
}
