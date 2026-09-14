using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Model;

namespace RonSijm.AnaalIJzer.Core.SemanticOperations.Matching;

public static class SemanticOperationMemberKindExtensions
{
	public static bool TryGetSemanticOperationMemberKind(this ISymbol symbol, out SemanticOperationMemberKind memberKind)
	{
		switch (symbol)
		{
			case IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor }:
				memberKind = SemanticOperationMemberKind.Constructor;
				return true;
			case IMethodSymbol:
				memberKind = SemanticOperationMemberKind.Method;
				return true;
			case IPropertySymbol:
				memberKind = SemanticOperationMemberKind.Property;
				return true;
			case IFieldSymbol:
				memberKind = SemanticOperationMemberKind.Field;
				return true;
			case IEventSymbol:
				memberKind = SemanticOperationMemberKind.Event;
				return true;
			default:
				memberKind = default;
				return false;
		}
	}
}
