using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.Matchers.Conditions;

internal readonly struct MatchContext(
	string subjectName,
	string subjectNamespaceName,
	ISymbol? subjectSymbol,
	string? associatedTypeName,
	string? associatedTypeNamespaceName,
	ITypeSymbol? associatedTypeSymbol)
{
	public string SubjectName { get; } = subjectName;

	public string SubjectNamespaceName { get; } = subjectNamespaceName;

	public ISymbol? SubjectSymbol { get; } = subjectSymbol;

	public string? AssociatedTypeName { get; } = associatedTypeName;

	public string? AssociatedTypeNamespaceName { get; } = associatedTypeNamespaceName;

	public ITypeSymbol? AssociatedTypeSymbol { get; } = associatedTypeSymbol;

	public static MatchContext Create(MatchTarget target, string typeName, string namespaceName, ITypeSymbol? symbol)
	{
		var subjectName = target.GetSubject(typeName, namespaceName, symbol);
		var result = target == MatchTarget.TypeName
			? new MatchContext(subjectName, namespaceName, symbol, typeName, namespaceName, symbol)
			: new MatchContext(subjectName, namespaceName, symbol, null, null, null);

		return result;
	}

	public string GetName(MatchOperand operand)
	{
		var result = operand == MatchOperand.AssociatedType
			? AssociatedTypeName ?? string.Empty
			: SubjectName;

		return result;
	}

	public string GetNamespace(MatchOperand operand)
	{
		var result = operand == MatchOperand.AssociatedType
			? AssociatedTypeNamespaceName ?? string.Empty
			: SubjectNamespaceName;

		return result;
	}

	public ISymbol? GetSymbol(MatchOperand operand)
	{
		var result = operand == MatchOperand.AssociatedType
			? AssociatedTypeSymbol
			: SubjectSymbol;

		return result;
	}

	public ITypeSymbol? GetTypeSymbol(MatchOperand operand)
	{
		var result = operand == MatchOperand.AssociatedType
			? AssociatedTypeSymbol
			: SubjectSymbol as ITypeSymbol;

		return result;
	}
}
