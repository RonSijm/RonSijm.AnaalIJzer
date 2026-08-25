using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Symbols;

namespace RonSijm.AnaalIJzer.Inheritance;

public readonly struct InheritancePolicy(
	string ownerLayerPath,
	ImmutableHashSet<string> typeKinds,
	ImmutableHashSet<string> requiredBaseTypes,
	ImmutableHashSet<string> requiredInterfaces,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public string OwnerLayerPath { get; } = ownerLayerPath;

	public ImmutableHashSet<string> TypeKinds { get; } = typeKinds;

	public ImmutableHashSet<string> RequiredBaseTypes { get; } = requiredBaseTypes;

	public ImmutableHashSet<string> RequiredInterfaces { get; } = requiredInterfaces;

	public string? Description { get; } = description;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public InheritancePolicyEvaluation? Evaluate(INamedTypeSymbol symbol)
	{
		if (!MatchesTypeKind(symbol))
		{
			return null;
		}

		if (RequiredBaseTypes.Count > 0 && !RequiredBaseTypes.Any(symbol.InheritsFrom))
		{
			var reason = "the InheritancePolicy in layer '"
			             + OwnerLayerPath
			             + "' requires a base type matching "
			             + string.Join(" or ", RequiredBaseTypes.OrderBy(item => item, StringComparer.Ordinal));
			var result = new InheritancePolicyEvaluation(this, InheritanceViolationKind.MissingRequiredBaseType, reason);

			return result;
		}

		var missingInterfaces = RequiredInterfaces
			.Where(interfaceName => !symbol.ImplementsInterface(interfaceName))
			.OrderBy(interfaceName => interfaceName, StringComparer.Ordinal)
			.ToArray();
		if (missingInterfaces.Length > 0)
		{
			var reason = "the InheritancePolicy in layer '"
			             + OwnerLayerPath
			             + "' requires implemented interface"
			             + (missingInterfaces.Length == 1 ? " " : "s ")
			             + string.Join(", ", missingInterfaces);
			var result = new InheritancePolicyEvaluation(this, InheritanceViolationKind.MissingRequiredInterface, reason);

			return result;
		}

		return null;
	}

	private bool MatchesTypeKind(INamedTypeSymbol symbol)
	{
		var result = TypeKinds.Any(symbol.HasTypeKind);

		return result;
	}
}
