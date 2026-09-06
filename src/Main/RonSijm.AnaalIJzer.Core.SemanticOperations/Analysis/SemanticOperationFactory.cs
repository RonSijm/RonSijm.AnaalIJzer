using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Model;

namespace RonSijm.AnaalIJzer.Core.SemanticOperations.Analysis;

public static class SemanticOperationFactory
{
	public static bool TryCreate(IOperation? operation, SemanticModel semanticModel, CancellationToken cancellationToken, out SemanticOperation semanticOperation)
	{
		if (operation is null)
		{
			semanticOperation = default;
			var missingOperationResult = false;

			return missingOperationResult;
		}

		var callerSymbol = semanticModel.GetEnclosingSymbol(operation.Syntax.SpanStart, cancellationToken);
		var created = TryCreateCore(operation, callerSymbol, out semanticOperation);

		return created;
	}

	public static bool TryCreate(IOperation? operation, ISymbol? callerSymbol, out SemanticOperation semanticOperation)
	{
		if (operation is null)
		{
			semanticOperation = default;
			var missingOperationResult = false;

			return missingOperationResult;
		}

		var result = TryCreateCore(operation, callerSymbol, out semanticOperation);

		return result;
	}

	private static bool TryCreateCore(IOperation operation, ISymbol? callerSymbol, out SemanticOperation semanticOperation)
	{
		switch (operation)
		{
			case IInvocationOperation invocation:
				semanticOperation = CreateInvocation(invocation, callerSymbol);
				var invocationResult = true;

				return invocationResult;
			case IPropertyReferenceOperation propertyReference:
				semanticOperation = CreateMemberReference(
					IsWriteReference(propertyReference) ? SemanticOperationKind.PropertyWrite : SemanticOperationKind.PropertyRead,
					propertyReference,
					propertyReference.Property,
					propertyReference.Type,
					callerSymbol,
					propertyReference.Syntax.GetLocation());
				var propertyResult = true;

				return propertyResult;
			case IFieldReferenceOperation fieldReference:
				semanticOperation = CreateMemberReference(
					IsWriteReference(fieldReference) ? SemanticOperationKind.FieldWrite : SemanticOperationKind.FieldRead,
					fieldReference,
					fieldReference.Field,
					fieldReference.Type,
					callerSymbol,
					fieldReference.Syntax.GetLocation());
				var fieldResult = true;

				return fieldResult;
			case IEventReferenceOperation eventReference:
				semanticOperation = CreateMemberReference(
					SemanticOperationKind.EventAccess,
					eventReference,
					eventReference.Event,
					eventReference.Type,
					callerSymbol,
					eventReference.Syntax.GetLocation());
				var eventResult = true;

				return eventResult;
			case IObjectCreationOperation objectCreation:
				semanticOperation = CreateOperation(
					objectCreation,
					SemanticOperationKind.ObjectCreation,
					objectCreation.Constructor,
					objectCreation.Type,
					callerSymbol,
					objectCreation.Syntax.GetLocation(),
					false,
					GetGenericTypeArguments(objectCreation.Type),
					objectCreation.Type as INamedTypeSymbol);
				var objectCreationResult = true;

				return objectCreationResult;
			case IConversionOperation conversion:
				semanticOperation = CreateOperation(
					conversion,
					SemanticOperationKind.Conversion,
					conversion.OperatorMethod,
					conversion.Type,
					callerSymbol,
					conversion.Syntax.GetLocation(),
					conversion.OperatorMethod?.IsStatic == true,
					GetGenericTypeArguments(conversion.OperatorMethod),
					conversion.OperatorMethod?.ContainingType ?? conversion.Type as INamedTypeSymbol);
				var conversionResult = true;

				return conversionResult;
			case IAssignmentOperation assignment:
				var targetSymbol = GetSelectedSymbol(assignment.Target);
				semanticOperation = CreateOperation(
					assignment,
					SemanticOperationKind.Assignment,
					targetSymbol,
					assignment.Target.Type,
					callerSymbol,
					assignment.Syntax.GetLocation(),
					IsStaticMember(targetSymbol),
					GetGenericTypeArguments(targetSymbol),
					GetContainingType(targetSymbol) ?? assignment.Target.Type as INamedTypeSymbol);
				var assignmentResult = true;

				return assignmentResult;
			case IReturnOperation returnOperation:
				semanticOperation = CreateOperation(
					returnOperation,
					SemanticOperationKind.Return,
					null,
					returnOperation.ReturnedValue?.Type,
					callerSymbol,
					returnOperation.Syntax.GetLocation(),
					false,
					ImmutableArray<ITypeSymbol>.Empty,
					callerSymbol?.ContainingType);
				var returnResult = true;

				return returnResult;
			case IArgumentOperation argument:
				semanticOperation = CreateOperation(
					argument,
					SemanticOperationKind.Argument,
					argument.Parameter,
					argument.Value.Type,
					callerSymbol,
					argument.Syntax.GetLocation(),
					false,
					ImmutableArray<ITypeSymbol>.Empty,
					argument.Parameter?.ContainingType);
				var argumentResult = true;

				return argumentResult;
			default:
				semanticOperation = default;
				var result = false;

				return result;
		}
	}

	private static SemanticOperation CreateInvocation(IInvocationOperation invocation, ISymbol? callerSymbol)
	{
		var targetMethod = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;
		var result = CreateOperation(
			invocation,
			SemanticOperationKind.Invocation,
			targetMethod,
			invocation.Type,
			callerSymbol,
			invocation.Syntax.GetLocation(),
			targetMethod.IsStatic || invocation.TargetMethod.ReducedFrom is not null,
			GetGenericTypeArguments(targetMethod),
			targetMethod.ContainingType);

		return result;
	}

	private static SemanticOperation CreateMemberReference(SemanticOperationKind kind, IOperation operation, ISymbol selectedSymbol, ITypeSymbol? valueType, ISymbol? callerSymbol, Location location)
	{
		var result = CreateOperation(
			operation,
			kind,
			selectedSymbol,
			valueType,
			callerSymbol,
			location,
			IsStaticMember(selectedSymbol),
			GetGenericTypeArguments(selectedSymbol),
			GetContainingType(selectedSymbol));

		return result;
	}

	private static SemanticOperation CreateOperation(IOperation operation, SemanticOperationKind kind, ISymbol? selectedSymbol, ITypeSymbol? valueType, ISymbol? callerSymbol, Location location, bool isStaticAccess, ImmutableArray<ITypeSymbol> genericTypeArguments, INamedTypeSymbol? containingType)
	{
		var displayName = CreateDisplayName(kind, selectedSymbol, containingType, valueType);
		var site = GetSite(operation, isStaticAccess);
		var result = new SemanticOperation(
			kind,
			selectedSymbol,
			containingType,
			valueType,
			callerSymbol,
			location,
			isStaticAccess,
			genericTypeArguments,
			site,
			displayName);

		return result;
	}

	private static string GetSite(IOperation operation, bool isStaticAccess)
	{
		if (isStaticAccess)
		{
			return DependencySites.StaticMember;
		}

		if (operation is IObjectCreationOperation)
		{
			return DependencySites.New;
		}

		if (operation is IInvocationOperation { TargetMethod: { IsGenericMethod: true } })
		{
			return DependencySites.GenericInvocation;
		}

		var syntax = operation.Syntax;
		if (operation is IReturnOperation || syntax.FirstAncestorOrSelf<ReturnStatementSyntax>() is not null)
		{
			return DependencySites.MethodReturn;
		}

		if (syntax.FirstAncestorOrSelf<ArrowExpressionClauseSyntax>() is { Parent: { } arrowOwner })
		{
			var arrowSite = arrowOwner switch
			{
				MethodDeclarationSyntax => DependencySites.MethodReturn,
				LocalFunctionStatementSyntax => DependencySites.MethodReturn,
				OperatorDeclarationSyntax => DependencySites.MethodReturn,
				ConversionOperatorDeclarationSyntax => DependencySites.MethodReturn,
				PropertyDeclarationSyntax => DependencySites.Property,
				IndexerDeclarationSyntax => DependencySites.Property,
				_ => DependencySites.Method
			};

			return arrowSite;
		}

		if (syntax.FirstAncestorOrSelf<AttributeSyntax>() is not null)
		{
			return DependencySites.Attribute;
		}

		if (syntax.FirstAncestorOrSelf<LocalDeclarationStatementSyntax>() is not null)
		{
			return DependencySites.Local;
		}

		if (syntax.FirstAncestorOrSelf<FieldDeclarationSyntax>() is not null)
		{
			return DependencySites.Field;
		}

		if (syntax.FirstAncestorOrSelf<PropertyDeclarationSyntax>() is not null)
		{
			return DependencySites.Property;
		}

		if (syntax.FirstAncestorOrSelf<ConstructorDeclarationSyntax>() is not null)
		{
			return DependencySites.Constructor;
		}

		var result = DependencySites.Method;

		return result;
	}

	private static bool IsWriteReference(IOperation operation)
	{
		var result = operation.Parent is IAssignmentOperation assignment && ReferenceEquals(assignment.Target, operation)
			|| operation.Parent is IIncrementOrDecrementOperation increment && ReferenceEquals(increment.Target, operation);

		return result;
	}

	private static ISymbol? GetSelectedSymbol(IOperation operation)
	{
		ISymbol? result = operation switch
		{
			IPropertyReferenceOperation propertyReference => propertyReference.Property,
			IFieldReferenceOperation fieldReference => fieldReference.Field,
			IEventReferenceOperation eventReference => eventReference.Event,
			ILocalReferenceOperation localReference => localReference.Local,
			IParameterReferenceOperation parameterReference => parameterReference.Parameter,
			_ => null
		};

		return result;
	}

	private static bool IsStaticMember(ISymbol? symbol)
	{
		var result = symbol switch
		{
			IMethodSymbol { IsStatic: true } => true,
			IPropertySymbol { IsStatic: true } => true,
			IFieldSymbol { IsStatic: true } => true,
			IEventSymbol { IsStatic: true } => true,
			_ => false
		};

		return result;
	}

	private static ImmutableArray<ITypeSymbol> GetGenericTypeArguments(ISymbol? symbol)
	{
		var result = symbol is IMethodSymbol { IsGenericMethod: true } method
			? method.TypeArguments
			: ImmutableArray<ITypeSymbol>.Empty;

		return result;
	}

	private static ImmutableArray<ITypeSymbol> GetGenericTypeArguments(ITypeSymbol? type)
	{
		var result = type is INamedTypeSymbol { IsGenericType: true } namedType
			? namedType.TypeArguments
			: ImmutableArray<ITypeSymbol>.Empty;

		return result;
	}

	private static INamedTypeSymbol? GetContainingType(ISymbol? symbol)
	{
		var result = symbol?.ContainingType;

		return result;
	}

	private static string CreateDisplayName(SemanticOperationKind kind, ISymbol? selectedSymbol, INamedTypeSymbol? containingType, ITypeSymbol? valueType)
	{
		if (selectedSymbol is not null)
		{
			var symbolDisplayName = selectedSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

			return symbolDisplayName;
		}

		if (containingType is not null)
		{
			var containingTypeDisplayName = containingType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

			return containingTypeDisplayName;
		}

		if (valueType is not null)
		{
			var valueTypeDisplayName = valueType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

			return valueTypeDisplayName;
		}

		var result = kind.ToString();

		return result;
	}
}
