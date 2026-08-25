namespace RonSijm.AnaalIJzer.Engine.Visibility;

public enum VisibilityPolicyTarget
{
	Type,
	Constructor,
	Method,
	Property,
	Field,
	Event,
	Operator,
	Conversion,
	NestedType
}

public static class VisibilityPolicyTargetParser
{
	public static bool TryParse(string value, out VisibilityPolicyTarget target)
	{
		var result = Enum.TryParse(value.Trim(), true, out target)
		             && Enum.IsDefined(typeof(VisibilityPolicyTarget), target);

		return result;
	}
}
