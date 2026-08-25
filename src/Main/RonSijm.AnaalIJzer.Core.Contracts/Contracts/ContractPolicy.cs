using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Contracts;

public readonly struct ContractPolicy(
	string ownerLayerPath,
	ImmutableHashSet<string> allowedTypeKinds,
	ImmutableHashSet<ContractMemberKind> allowedMemberKinds,
	ImmutableHashSet<ContractPropertyAccessor> allowedPropertyAccessors,
	bool restrictPropertyAccessors,
	bool allowMethodBodies,
	bool allowStaticMembers,
	bool allowNestedTypes,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public string OwnerLayerPath { get; } = ownerLayerPath;
	public ImmutableHashSet<string> AllowedTypeKinds { get; } = allowedTypeKinds;
	public ImmutableHashSet<ContractMemberKind> AllowedMemberKinds { get; } = allowedMemberKinds;
	public ImmutableHashSet<ContractPropertyAccessor> AllowedPropertyAccessors { get; } = allowedPropertyAccessors;
	public bool RestrictPropertyAccessors { get; } = restrictPropertyAccessors;
	public bool AllowMethodBodies { get; } = allowMethodBodies;
	public bool AllowStaticMembers { get; } = allowStaticMembers;
	public bool AllowNestedTypes { get; } = allowNestedTypes;
	public string? Description { get; } = description;
	public string XmlPath { get; } = xmlPath;
	public int XmlLineNumber { get; } = xmlLineNumber;
	public int XmlLinePosition { get; } = xmlLinePosition;

	public ContractPolicyEvaluation? Evaluate(ContractDeclarationShape shape)
	{
		if (!AllowedTypeKinds.Contains(shape.TypeKind))
		{
			var reason = $"the ContractPolicy in layer '{OwnerLayerPath}' allows only type kinds {FormatTypeKinds(AllowedTypeKinds)}";
			var result = new ContractPolicyEvaluation(this, ContractViolationKind.DisallowedTypeKind, reason);

			return result;
		}

		if (shape.IsNestedType && !AllowNestedTypes)
		{
			var result = new ContractPolicyEvaluation(this, ContractViolationKind.NestedTypeForbidden, $"the ContractPolicy in layer '{OwnerLayerPath}' sets allowNestedTypes='false'");

			return result;
		}

		if (shape.MemberKind is { } memberKind && !AllowedMemberKinds.Contains(memberKind))
		{
			var result = new ContractPolicyEvaluation(this, ContractViolationKind.DisallowedMemberKind, $"the ContractPolicy in layer '{OwnerLayerPath}' allows only member kinds {FormatMemberKinds(AllowedMemberKinds)}");

			return result;
		}

		if (shape.IsStatic && !AllowStaticMembers)
		{
			var result = new ContractPolicyEvaluation(this, ContractViolationKind.StaticMemberForbidden, $"the ContractPolicy in layer '{OwnerLayerPath}' sets allowStaticMembers='false'");

			return result;
		}

		if (shape.HasBody && !AllowMethodBodies)
		{
			var result = new ContractPolicyEvaluation(this, ContractViolationKind.MethodBodyForbidden, $"the ContractPolicy in layer '{OwnerLayerPath}' sets allowMethodBodies='false'");

			return result;
		}

		if (RestrictPropertyAccessors)
		{
			foreach (var accessor in shape.PropertyAccessors)
			{
				if (!AllowedPropertyAccessors.Contains(accessor))
				{
					var result = new ContractPolicyEvaluation(this, ContractViolationKind.DisallowedPropertyAccessor, $"the ContractPolicy in layer '{OwnerLayerPath}' allows only property accessors {FormatPropertyAccessors(AllowedPropertyAccessors)}");

					return result;
				}
			}
		}

		return null;
	}

	private static string FormatTypeKinds(ImmutableHashSet<string> values)
	{
		var result = string.Join(", ", values.OrderBy(value => value, StringComparer.Ordinal));

		return result;
	}

	private static string FormatMemberKinds(ImmutableHashSet<ContractMemberKind> values)
	{
		var result = string.Join(", ", ContractMemberKindParser.CanonicalOrder.Where(values.Contains).Select(value => value.ToDisplayText()));

		return result;
	}

	private static string FormatPropertyAccessors(ImmutableHashSet<ContractPropertyAccessor> values)
	{
		var result = string.Join(", ", ContractPropertyAccessorParser.CanonicalOrder.Where(values.Contains).Select(value => value.ToString()));

		return result;
	}
}
