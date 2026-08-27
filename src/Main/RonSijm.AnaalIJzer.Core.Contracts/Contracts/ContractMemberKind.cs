using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.Contracts.Contracts;

public enum ContractMemberKind
{
	Constructor,
	Method,
	Property,
	Event,
	Field,
	Operator,
	Conversion
}

public static class ContractMemberKindParser
{
	public static readonly ImmutableArray<ContractMemberKind> CanonicalOrder =
	[
		ContractMemberKind.Constructor,
		ContractMemberKind.Method,
		ContractMemberKind.Property,
		ContractMemberKind.Event,
		ContractMemberKind.Field,
		ContractMemberKind.Operator,
		ContractMemberKind.Conversion
	];

	public static bool TryParse(string value, out ContractMemberKind result)
	{
		switch (value.Trim().ToLowerInvariant())
		{
			case "constructor":
				result = ContractMemberKind.Constructor;
				return true;
			case "method":
				result = ContractMemberKind.Method;
				return true;
			case "property":
				result = ContractMemberKind.Property;
				return true;
			case "event":
				result = ContractMemberKind.Event;
				return true;
			case "field":
				result = ContractMemberKind.Field;
				return true;
			case "operator":
				result = ContractMemberKind.Operator;
				return true;
			case "conversion":
				result = ContractMemberKind.Conversion;
				return true;
			default:
				result = default;
				return false;
		}
	}

	public static string ToDisplayText(this ContractMemberKind value)
	{
		var result = value.ToString();

		return result;
	}
}
