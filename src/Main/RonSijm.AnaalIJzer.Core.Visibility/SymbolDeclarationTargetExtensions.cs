using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.Visibility;

public static class SymbolDeclarationTargetExtensions
{
	public static bool TryGetArchitectureDeclarationTarget(this ISymbol symbol, out VisibilityPolicyTarget target)
	{
		switch (symbol)
		{
			case INamedTypeSymbol namedType:
				target = namedType.ContainingType is null ? VisibilityPolicyTarget.Type : VisibilityPolicyTarget.NestedType;
				return true;
			case IMethodSymbol method:
				return TryGetMethodTarget(method, out target);
			case IPropertySymbol:
				target = VisibilityPolicyTarget.Property;
				return true;
			case IFieldSymbol:
				target = VisibilityPolicyTarget.Field;
				return true;
			case IEventSymbol:
				target = VisibilityPolicyTarget.Event;
				return true;
			default:
				target = default;
				return false;
		}
	}

	private static bool TryGetMethodTarget(IMethodSymbol method, out VisibilityPolicyTarget target)
	{
		switch (method.MethodKind)
		{
			case MethodKind.Constructor:
			case MethodKind.StaticConstructor:
				target = VisibilityPolicyTarget.Constructor;
				return true;
			case MethodKind.Ordinary:
			case MethodKind.ExplicitInterfaceImplementation:
				target = VisibilityPolicyTarget.Method;
				return true;
			case MethodKind.UserDefinedOperator:
				target = VisibilityPolicyTarget.Operator;
				return true;
			case MethodKind.Conversion:
				target = VisibilityPolicyTarget.Conversion;
				return true;
			default:
				target = default;
				return false;
		}
	}
}
