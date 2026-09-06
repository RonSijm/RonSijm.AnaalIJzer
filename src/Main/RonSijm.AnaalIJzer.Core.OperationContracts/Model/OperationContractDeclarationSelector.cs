using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Matching;

namespace RonSijm.AnaalIJzer.Core.OperationContracts.Model;

/// <summary>Selects a method declaration without prescribing a framework or naming convention.</summary>
public readonly struct OperationContractDeclarationSelector(
	SemanticDeclarationMatcher matcher,
	string displayName,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public SemanticDeclarationMatcher Matcher { get; } = matcher;

	public string DisplayName { get; } = displayName;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public bool Matches(IMethodSymbol method)
	{
		var result = Matcher.Matches(method);

		return result;
	}
}
