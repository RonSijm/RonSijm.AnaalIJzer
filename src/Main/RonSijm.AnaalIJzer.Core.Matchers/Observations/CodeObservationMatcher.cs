using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;

namespace RonSijm.AnaalIJzer.Core.Matchers.Observations;

public readonly struct CodeObservationMatcher(
	CodeObservationMatchTarget target,
	ImmutableArray<MatchCondition> conditions)
{
	public CodeObservationMatchTarget Target { get; } = target;

	public ImmutableArray<MatchCondition> Conditions { get; } = conditions;

	public bool Matches(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
	{
		var result = CodeObservationMatcherEvaluator.Matches(node, this, semanticModel, cancellationToken);

		return result;
	}
}
