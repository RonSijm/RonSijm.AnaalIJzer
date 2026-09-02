using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RonSijm.AnaalIJzer.Core.ReturnValues.Policies;

public readonly struct ReturnValuePolicy(
	string ownerLayerPath,
	ImmutableArray<ReturnValueRule> rules,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public string OwnerLayerPath { get; } = ownerLayerPath;

	public ImmutableArray<ReturnValueRule> Rules { get; } = rules;

	public string? Description { get; } = description;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public ReturnValuePolicyEvaluation? Evaluate(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
	{
		foreach (var rule in Rules)
		{
			if (!rule.Matches(expression, semanticModel, cancellationToken))
			{
				continue;
			}

			var result = new ReturnValuePolicyEvaluation(
				this,
				rule,
				$"the ReturnValuePolicy in layer '{OwnerLayerPath}' blocks returned {rule.DisplayName}");

			return result;
		}

		return null;
	}
}
