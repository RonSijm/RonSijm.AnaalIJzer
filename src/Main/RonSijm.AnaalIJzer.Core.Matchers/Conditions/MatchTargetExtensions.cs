using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Conditions;

internal static class MatchTargetExtensions
{
	internal static string GetSubject(this MatchTarget target, string typeName, string namespaceName, ITypeSymbol? symbol)
	{
		var result = target switch
		{
			MatchTarget.Namespace => namespaceName,
			MatchTarget.Assembly => symbol?.ContainingAssembly?.Name ?? string.Empty,
			_ => typeName
		};

		return result;
	}
}
