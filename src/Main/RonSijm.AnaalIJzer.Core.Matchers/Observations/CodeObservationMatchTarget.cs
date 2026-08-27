namespace RonSijm.AnaalIJzer.Core.Matchers.Observations;

public enum CodeObservationMatchTarget
{
	Throw,
	Invocation,
	New,
	Identifier,
	MemberAccess,
	Literal
}

public static class CodeObservationMatchTargetParser
{
	public static bool TryParse(string value, out CodeObservationMatchTarget target)
	{
		var result = Enum.TryParse(value.Trim(), true, out target)
		             && Enum.IsDefined(typeof(CodeObservationMatchTarget), target);

		return result;
	}
}
