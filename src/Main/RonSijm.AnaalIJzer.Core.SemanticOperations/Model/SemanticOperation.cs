using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.SemanticOperations.Model;

public readonly struct SemanticOperation(
	SemanticOperationKind kind,
	ISymbol? selectedSymbol,
	INamedTypeSymbol? containingType,
	ITypeSymbol? valueType,
	ISymbol? callerSymbol,
	Location location,
	bool isStaticAccess,
	ImmutableArray<ITypeSymbol> genericTypeArguments,
	string site,
	string displayName)
{
	public SemanticOperationKind Kind { get; } = kind;

	public ISymbol? SelectedSymbol { get; } = selectedSymbol;

	public INamedTypeSymbol? ContainingType { get; } = containingType;

	public ITypeSymbol? ValueType { get; } = valueType;

	public ISymbol? CallerSymbol { get; } = callerSymbol;

	public Location Location { get; } = location;

	public bool IsStaticAccess { get; } = isStaticAccess;

	public ImmutableArray<ITypeSymbol> GenericTypeArguments { get; } = genericTypeArguments;

	/// <summary>Architectural site that owns this operation, using the shared DependencySites vocabulary.</summary>
	public string Site { get; } = site;

	public string DisplayName { get; } = displayName;
}
