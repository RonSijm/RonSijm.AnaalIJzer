using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Analysis;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.EditorRuntime.Editor.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static void AddForbiddenOperationPolicyIndicator(SyntaxNode node, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureDependencySiteIndicator>.Builder indicators, CancellationToken cancellationToken)
	{
		if (!CanRepresentSemanticOperation(node)
			|| !SemanticOperationFactory.TryCreate(semanticModel.GetOperation(node, cancellationToken), semanticModel, cancellationToken, out var operation)
			|| TryGetCaller(node, semanticModel, config, cancellationToken) is not { } caller)
		{
			return;
		}

		var evaluation = config.EvaluateForbiddenOperationPolicies(caller.Match, operation);
		if (evaluation is null)
		{
			return;
		}

		var tooltip = operation.Site
		              + ": "
		              + caller.TypeName
		              + " ("
		              + caller.LayerPath
		              + ") may not use "
		              + operation.DisplayName
		              + " - "
		              + evaluation.Value.Reason;
		indicators.Add(new ArchitectureDependencySiteIndicator(
			operation.Location.SourceSpan,
			operation.Site,
			caller.TypeName,
			caller.LayerPath,
			operation.DisplayName,
			null,
			0,
			ArchitectureDependencySiteStatus.TypePolicyViolation,
			ArchitecturalDiagnosticIds.ForbiddenOperationPolicyViolation,
			tooltip,
			evaluation.Value.Reason));
	}

	private static bool CanRepresentSemanticOperation(SyntaxNode node)
	{
		var result = node is InvocationExpressionSyntax
			or MemberAccessExpressionSyntax
			or IdentifierNameSyntax
			or ElementAccessExpressionSyntax
			or ObjectCreationExpressionSyntax
			or ImplicitObjectCreationExpressionSyntax
			or CastExpressionSyntax
			or AssignmentExpressionSyntax
			or ReturnStatementSyntax
			or ArgumentSyntax;

		return result;
	}
}
