using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.OperationContracts.Model;

/// <summary>Explicit operation contracts declared by the configuration author.</summary>
public readonly struct OperationContractCatalog(ImmutableArray<OperationContractDefinition> definitions)
{
	public static OperationContractCatalog Empty { get; } = new([]);

	public ImmutableArray<OperationContractDefinition> Definitions { get; } = definitions.IsDefault ? [] : definitions;

	public bool HasDefinitions => !Definitions.IsDefaultOrEmpty;
}
