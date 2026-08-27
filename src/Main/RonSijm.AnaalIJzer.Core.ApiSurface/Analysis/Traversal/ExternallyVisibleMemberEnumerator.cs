using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Model;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.Core.Visibility;

namespace RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Traversal;

internal static class ExternallyVisibleMemberEnumerator
{
	internal static ImmutableArray<ExposureMemberTypeReference> GetReferences(INamedTypeSymbol type, CancellationToken cancellationToken)
	{
		var references = ImmutableArray.CreateBuilder<ExposureMemberTypeReference>();
		if (type.BaseType is { SpecialType: SpecialType.None } baseType)
		{
			references.Add(new ExposureMemberTypeReference(type.Name + ".base", baseType, DependencySites.Inheritance, GetLocation(type, cancellationToken)));
		}
		foreach (var interfaceType in type.Interfaces)
		{
			references.Add(new ExposureMemberTypeReference(type.Name + ".interface", interfaceType, DependencySites.InterfaceImplementation, GetLocation(type, cancellationToken)));
		}
		foreach (var typeParameter in type.TypeParameters)
		{
			foreach (var constraintType in typeParameter.ConstraintTypes)
			{
				references.Add(new ExposureMemberTypeReference(type.Name + "." + typeParameter.Name + " constraint", constraintType, DependencySites.GenericArgument, GetLocation(type, cancellationToken)));
			}
		}

		foreach (var member in type.GetMembers()
			         .Where(member => !member.IsImplicitlyDeclared && member.IsEffectivelyExternallyVisible())
			         .OrderBy(member => member.Kind)
			         .ThenBy(member => member.Name, StringComparer.Ordinal)
			         .ThenBy(member => member.Locations.FirstOrDefault()?.SourceSpan.Start ?? int.MaxValue))
		{
			var segmentName = type.Name + "." + member.Name;
			var location = GetLocation(member, cancellationToken);
			switch (member)
			{
				case IPropertySymbol property:
					references.Add(new ExposureMemberTypeReference(segmentName, property.Type, DependencySites.Property, location));
					foreach (var parameter in property.Parameters)
					{
						references.Add(new ExposureMemberTypeReference(segmentName + "(" + parameter.Name + ")", parameter.Type, DependencySites.Property, location));
					}
					break;
				case IFieldSymbol field:
					references.Add(new ExposureMemberTypeReference(segmentName, field.Type, DependencySites.Field, location));
					break;
				case IEventSymbol eventSymbol:
					references.Add(new ExposureMemberTypeReference(segmentName, eventSymbol.Type, DependencySites.Field, location));
					break;
				case IMethodSymbol method when IsSupportedMethod(method):
					var parameterSite = method.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor
						? DependencySites.Constructor
						: DependencySites.Method;
					foreach (var parameter in method.Parameters)
					{
						references.Add(new ExposureMemberTypeReference(segmentName + "(" + parameter.Name + ")", parameter.Type, parameterSite, location));
					}
					if (!method.ReturnsVoid && method.MethodKind is not (MethodKind.Constructor or MethodKind.StaticConstructor))
					{
						references.Add(new ExposureMemberTypeReference(segmentName, method.ReturnType, DependencySites.MethodReturn, location));
					}
					foreach (var typeParameter in method.TypeParameters)
					{
						foreach (var constraintType in typeParameter.ConstraintTypes)
						{
							references.Add(new ExposureMemberTypeReference(segmentName + "." + typeParameter.Name + " constraint", constraintType, DependencySites.GenericArgument, location));
						}
					}
					break;
			}
		}

		return references.ToImmutable();
	}

	internal static IEnumerable<(INamedTypeSymbol Type, string SegmentName)> ExpandNamedTypes(ExposureMemberTypeReference reference)
	{
		foreach (var result in ExpandNamedTypes(reference.Type, reference.SegmentName, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default)))
		{
			yield return result;
		}
	}

	private static IEnumerable<(INamedTypeSymbol Type, string SegmentName)> ExpandNamedTypes(ITypeSymbol type, string segmentName, ISet<ITypeSymbol> seen)
	{
		if (!seen.Add(type))
		{
			yield break;
		}

		switch (type)
		{
			case IArrayTypeSymbol array:
				foreach (var item in ExpandNamedTypes(array.ElementType, segmentName + "[0]", seen))
				{
					yield return item;
				}
				yield break;
			case IPointerTypeSymbol pointer:
				foreach (var item in ExpandNamedTypes(pointer.PointedAtType, segmentName + "*", seen))
				{
					yield return item;
				}
				yield break;
			case IFunctionPointerTypeSymbol functionPointer:
				foreach (var item in ExpandNamedTypes(functionPointer.Signature.ReturnType, segmentName + ".return", seen))
				{
					yield return item;
				}
				foreach (var parameter in functionPointer.Signature.Parameters)
				{
					foreach (var item in ExpandNamedTypes(parameter.Type, segmentName + "." + parameter.Name, seen))
					{
						yield return item;
					}
				}
				yield break;
		}

		if (type is not INamedTypeSymbol namedType)
		{
			yield break;
		}

		yield return (namedType, segmentName);
		for (var index = 0; index < namedType.TypeArguments.Length; index++)
		{
			foreach (var item in ExpandNamedTypes(namedType.TypeArguments[index], segmentName + "[" + index + "]", seen))
			{
				yield return item;
			}
		}

		if (namedType is { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: { } invokeMethod })
		{
			foreach (var item in ExpandNamedTypes(invokeMethod.ReturnType, segmentName + ".return", seen))
			{
				yield return item;
			}
			foreach (var parameter in invokeMethod.Parameters)
			{
				foreach (var item in ExpandNamedTypes(parameter.Type, segmentName + "." + parameter.Name, seen))
				{
					yield return item;
				}
			}
		}
	}

	private static bool IsSupportedMethod(IMethodSymbol method)
	{
		var result = method.MethodKind is MethodKind.Ordinary
			or MethodKind.Constructor
			or MethodKind.StaticConstructor
			or MethodKind.UserDefinedOperator
			or MethodKind.Conversion;

		return result;
	}

	private static Location? GetLocation(ISymbol symbol, CancellationToken cancellationToken)
	{
		var result = symbol.DeclaringSyntaxReferences
			.Select(reference => reference.GetSyntax(cancellationToken).GetLocation())
			.OrderBy(location => location.SourceTree?.FilePath, StringComparer.Ordinal)
			.ThenBy(location => location.SourceSpan.Start)
			.FirstOrDefault();

		return result;
	}
}
