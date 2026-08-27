namespace RonSijm.AnaalIJzer.Core.Matchers.Declarations;

public enum DeclarationMatchTarget
{
	Type,
	NestedType,
	Constructor,
	Method,
	Property,
	Field,
	Event,
	Operator,
	Conversion
}

public static class DeclarationMatchTargetParser
{
	public static bool TryParse(string value, out DeclarationMatchTarget target)
	{
		var result = Enum.TryParse(value.Trim(), true, out target)
		             && Enum.IsDefined(typeof(DeclarationMatchTarget), target);

		return result;
	}
}
