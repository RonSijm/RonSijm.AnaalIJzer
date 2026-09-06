using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using RonSijm.AnaalIJzer.Core.OperationContracts.Evaluation;
using RonSijm.AnaalIJzer.Core.OperationContracts.Model;

namespace RonSijm.AnaalIJzer.Core.OperationContracts.Analysis;

/// <summary>Finds direct calls to an explicitly selected owner without inferring a workflow.</summary>
public static class OperationContractInvocationInspector
{
	public static bool DirectlyInvokesOwner(SemanticModel semanticModel, SyntaxNode declaration, OperationContractDefinition definition, CancellationToken cancellationToken)
	{
		foreach (var invocationSyntax in declaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
		{
			if (IsNestedFunctionInvocation(invocationSyntax, declaration)
				|| semanticModel.GetOperation(invocationSyntax, cancellationToken) is not IInvocationOperation invocation
				|| !OperationContractEvaluator.MatchesOwner(definition, invocation.TargetMethod))
			{
				continue;
			}

			return true;
		}

		return false;
	}

	private static bool IsNestedFunctionInvocation(InvocationExpressionSyntax invocation, SyntaxNode declaration)
	{
		foreach (var ancestor in invocation.Ancestors())
		{
			if (ancestor == declaration)
			{
				return false;
			}

			if (ancestor is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)
			{
				return true;
			}
		}

		return false;
	}
}
