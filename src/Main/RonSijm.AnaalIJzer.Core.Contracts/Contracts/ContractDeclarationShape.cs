using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Contracts;

public readonly struct ContractDeclarationShape(
	string declaredSymbolName,
	string typeKind,
	ContractMemberKind? memberKind,
	ImmutableHashSet<ContractPropertyAccessor> propertyAccessors,
	bool isStatic,
	bool hasBody,
	bool isNestedType)
{
	public string DeclaredSymbolName { get; } = declaredSymbolName;
	public string TypeKind { get; } = typeKind;
	public ContractMemberKind? MemberKind { get; } = memberKind;
	public ImmutableHashSet<ContractPropertyAccessor> PropertyAccessors { get; } = propertyAccessors;
	public bool IsStatic { get; } = isStatic;
	public bool HasBody { get; } = hasBody;
	public bool IsNestedType { get; } = isNestedType;
}
