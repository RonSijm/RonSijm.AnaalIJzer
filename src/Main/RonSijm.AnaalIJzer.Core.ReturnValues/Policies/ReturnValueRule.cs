using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.Matchers.Observations;

namespace RonSijm.AnaalIJzer.Core.ReturnValues.Policies;

public readonly struct ReturnValueRule(
	CodeObservationMatcher matcher,
	string displayName,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public CodeObservationMatcher Matcher { get; } = matcher;

	public string DisplayName { get; } = displayName;

	public string? Description { get; } = description;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public bool Matches(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
	{
		var result = Matcher.Matches(expression, semanticModel, cancellationToken);

		return result;
	}
}
