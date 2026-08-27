using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.Matchers.Symbols;

namespace RonSijm.AnaalIJzer.Core.Contracts.Contracts;

public static class ContractDeclarationShapeFactory
{
	public static ContractDeclarationShape CreateTypeShape(INamedTypeSymbol typeSymbol)
	{
		var result = new ContractDeclarationShape(
			typeSymbol.Name,
			GetTypeKind(typeSymbol),
			null,
			ImmutableHashSet<ContractPropertyAccessor>.Empty,
			typeSymbol.IsStatic,
			false,
			typeSymbol.ContainingType is not null);

		return result;
	}

	public static IEnumerable<(ContractDeclarationShape Shape, Location Location)> CreateMemberShapes(ISymbol member, CancellationToken cancellationToken)
	{
		switch (member)
		{
			case INamedTypeSymbol nestedType:
				yield return (CreateTypeShape(nestedType), GetTypeLocation(nestedType, cancellationToken));
				yield break;
			case IMethodSymbol method when TryCreateMethodShape(method, cancellationToken, out var methodShape, out var methodLocation):
				yield return (methodShape, methodLocation);
				yield break;
			case IPropertySymbol property when TryCreatePropertyShape(property, cancellationToken, out var propertyShape, out var propertyLocation):
				yield return (propertyShape, propertyLocation);
				yield break;
			case IFieldSymbol field:
				yield return (new ContractDeclarationShape(field.Name, GetTypeKind(field.ContainingType), ContractMemberKind.Field, ImmutableHashSet<ContractPropertyAccessor>.Empty, field.IsStatic, false, false), GetMemberLocation(field, cancellationToken));
				yield break;
			case IEventSymbol @event:
				yield return (new ContractDeclarationShape(@event.Name, GetTypeKind(@event.ContainingType), ContractMemberKind.Event, ImmutableHashSet<ContractPropertyAccessor>.Empty, @event.IsStatic, HasAccessorBody(@event.AddMethod, cancellationToken) || HasAccessorBody(@event.RemoveMethod, cancellationToken), false), GetMemberLocation(@event, cancellationToken));
				yield break;
		}
	}

	private static bool TryCreateMethodShape(IMethodSymbol method, CancellationToken cancellationToken, out ContractDeclarationShape shape, out Location location)
	{
		switch (method.MethodKind)
		{
			case MethodKind.Constructor:
			case MethodKind.StaticConstructor:
				shape = new ContractDeclarationShape(method.Name, GetTypeKind(method.ContainingType), ContractMemberKind.Constructor, ImmutableHashSet<ContractPropertyAccessor>.Empty, method.IsStatic, HasMethodBody(method, cancellationToken), false);
				location = GetMemberLocation(method, cancellationToken);
				return true;
			case MethodKind.Ordinary:
			case MethodKind.ExplicitInterfaceImplementation:
				shape = new ContractDeclarationShape(method.Name, GetTypeKind(method.ContainingType), ContractMemberKind.Method, ImmutableHashSet<ContractPropertyAccessor>.Empty, method.IsStatic, HasMethodBody(method, cancellationToken), false);
				location = GetMemberLocation(method, cancellationToken);
				return true;
			case MethodKind.UserDefinedOperator:
				shape = new ContractDeclarationShape(method.Name, GetTypeKind(method.ContainingType), ContractMemberKind.Operator, ImmutableHashSet<ContractPropertyAccessor>.Empty, method.IsStatic, HasMethodBody(method, cancellationToken), false);
				location = GetMemberLocation(method, cancellationToken);
				return true;
			case MethodKind.Conversion:
				shape = new ContractDeclarationShape(method.Name, GetTypeKind(method.ContainingType), ContractMemberKind.Conversion, ImmutableHashSet<ContractPropertyAccessor>.Empty, method.IsStatic, HasMethodBody(method, cancellationToken), false);
				location = GetMemberLocation(method, cancellationToken);
				return true;
			default:
				shape = default;
				location = Location.None;
				return false;
		}
	}

	private static bool TryCreatePropertyShape(IPropertySymbol property, CancellationToken cancellationToken, out ContractDeclarationShape shape, out Location location)
	{
		var accessors = ImmutableHashSet.CreateBuilder<ContractPropertyAccessor>();
		if (property.GetMethod is not null)
		{
			accessors.Add(ContractPropertyAccessor.Get);
		}

		if (property.SetMethod is not null)
		{
			accessors.Add(property.SetMethod.IsInitOnly ? ContractPropertyAccessor.Init : ContractPropertyAccessor.Set);
		}

		var hasBody = HasAccessorBody(property.GetMethod, cancellationToken) || HasAccessorBody(property.SetMethod, cancellationToken);
		shape = new ContractDeclarationShape(property.Name, GetTypeKind(property.ContainingType), ContractMemberKind.Property, accessors.ToImmutable(), property.IsStatic, hasBody, false);
		location = GetMemberLocation(property, cancellationToken);
		return true;
	}

	private static bool HasMethodBody(IMethodSymbol method, CancellationToken cancellationToken)
	{
		var syntax = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken);
		var result = syntax switch
		{
			BaseMethodDeclarationSyntax declaration => declaration.Body is not null || declaration.ExpressionBody is not null,
			AccessorDeclarationSyntax declaration => declaration.Body is not null || declaration.ExpressionBody is not null,
			_ => false
		};

		return result;
	}

	private static bool HasAccessorBody(IMethodSymbol? method, CancellationToken cancellationToken)
	{
		var result = method is not null && HasMethodBody(method, cancellationToken);

		return result;
	}

	private static Location GetTypeLocation(INamedTypeSymbol typeSymbol, CancellationToken cancellationToken)
	{
		var syntax = typeSymbol.DeclaringSyntaxReferences.First().GetSyntax(cancellationToken) as TypeDeclarationSyntax;
		var result = syntax?.Identifier.GetLocation() ?? Location.None;

		return result;
	}

	private static Location GetMemberLocation(ISymbol symbol, CancellationToken cancellationToken)
	{
		var syntax = symbol.DeclaringSyntaxReferences.First().GetSyntax(cancellationToken);
		var result = syntax switch
		{
			BaseMethodDeclarationSyntax declaration => declaration switch
			{
				MethodDeclarationSyntax method => method.Identifier.GetLocation(),
				ConstructorDeclarationSyntax constructor => constructor.Identifier.GetLocation(),
				DestructorDeclarationSyntax destructor => destructor.Identifier.GetLocation(),
				OperatorDeclarationSyntax @operator => @operator.OperatorToken.GetLocation(),
				ConversionOperatorDeclarationSyntax conversion => conversion.Type.GetLocation(),
				_ => declaration.GetLocation()
			},
			PropertyDeclarationSyntax property => property.Identifier.GetLocation(),
			EventDeclarationSyntax @event => @event.Identifier.GetLocation(),
			EventFieldDeclarationSyntax eventField => eventField.Declaration.Variables.First().Identifier.GetLocation(),
			VariableDeclaratorSyntax variable => variable.Identifier.GetLocation(),
			TypeDeclarationSyntax type => type.Identifier.GetLocation(),
			_ => syntax.GetLocation()
		};

		return result;
	}

	private static string GetTypeKind(INamedTypeSymbol typeSymbol)
	{
		var result = typeSymbol.HasTypeKind("class") ? "Class"
			: typeSymbol.HasTypeKind("interface") ? "Interface"
			: typeSymbol.HasTypeKind("struct") ? "Struct"
			: typeSymbol.HasTypeKind("record") ? "Record"
			: typeSymbol.HasTypeKind("recordstruct") ? "RecordStruct"
			: typeSymbol.HasTypeKind("enum") ? "Enum"
			: "Delegate";

		return result;
	}
}
