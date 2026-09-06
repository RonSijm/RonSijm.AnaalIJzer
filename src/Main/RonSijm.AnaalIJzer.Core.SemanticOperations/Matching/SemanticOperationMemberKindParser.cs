using RonSijm.AnaalIJzer.Core.SemanticOperations.Model;

namespace RonSijm.AnaalIJzer.Core.SemanticOperations.Matching;

public static class SemanticOperationMemberKindParser
{
	public static bool TryParse(string value, out SemanticOperationMemberKind kind)
	{
		var result = Enum.TryParse(value.Trim(), true, out kind)
			&& Enum.IsDefined(typeof(SemanticOperationMemberKind), kind);

		return result;
	}
}
