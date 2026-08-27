using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.Contracts.Contracts;

public enum ContractPropertyAccessor
{
	Get,
	Set,
	Init
}

public static class ContractPropertyAccessorParser
{
	public static readonly ImmutableArray<ContractPropertyAccessor> CanonicalOrder =
	[
		ContractPropertyAccessor.Get,
		ContractPropertyAccessor.Set,
		ContractPropertyAccessor.Init
	];

	public static bool TryParse(string value, out ContractPropertyAccessor result)
	{
		switch (value.Trim().ToLowerInvariant())
		{
			case "get":
				result = ContractPropertyAccessor.Get;
				return true;
			case "set":
				result = ContractPropertyAccessor.Set;
				return true;
			case "init":
				result = ContractPropertyAccessor.Init;
				return true;
			default:
				result = default;
				return false;
		}
	}
}
