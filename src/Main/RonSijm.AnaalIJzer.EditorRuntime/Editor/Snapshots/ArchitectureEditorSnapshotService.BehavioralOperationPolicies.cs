using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.Core.OperationPolicies.Behavioral;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.EditorRuntime.Editor.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static void AddBehavioralOperationPolicyIndicators(SyntaxNode syntaxRoot, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureDependencySiteIndicator>.Builder indicators, CancellationToken cancellationToken)
	{
		var analyzedSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
		foreach (var declaration in EnumerateBehavioralOperationDeclarations(syntaxRoot))
		{
			var owningSymbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
			var bodyNode = GetBehavioralOperationBodyNode(declaration);
			var operationBlock = bodyNode is null
				? null
				: semanticModel.GetOperation(bodyNode, cancellationToken);
			if (owningSymbol is null
				|| !analyzedSymbols.Add(owningSymbol)
				|| TryGetCaller(declaration, semanticModel, config, cancellationToken) is not { } caller
				|| operationBlock is null
				|| !BehavioralOperationBodyAnalysis.TryCreate(operationBlock, owningSymbol, out var body))
			{
				continue;
			}

			var evaluations = config.EvaluateBehavioralOperationPolicies(caller.Match, body);
			foreach (var evaluation in evaluations)
			{
				indicators.Add(CreateBehavioralOperationPolicyIndicator(caller, body, evaluation));
			}
		}
	}

	private static IEnumerable<SyntaxNode> EnumerateBehavioralOperationDeclarations(SyntaxNode syntaxRoot)
	{
		var result = syntaxRoot.DescendantNodes().Where(node => node is BaseMethodDeclarationSyntax
			or AccessorDeclarationSyntax
			or LocalFunctionStatementSyntax
			or PropertyDeclarationSyntax
			or IndexerDeclarationSyntax);

		return result;
	}

	private static SyntaxNode? GetBehavioralOperationBodyNode(SyntaxNode declaration)
	{
		SyntaxNode? result = declaration switch
		{
			BaseMethodDeclarationSyntax { Body: not null } method => method.Body,
			BaseMethodDeclarationSyntax { ExpressionBody: not null } method => method.ExpressionBody.Expression,
			AccessorDeclarationSyntax { Body: not null } accessor => accessor.Body,
			AccessorDeclarationSyntax { ExpressionBody: not null } accessor => accessor.ExpressionBody.Expression,
			LocalFunctionStatementSyntax { Body: not null } localFunction => localFunction.Body,
			LocalFunctionStatementSyntax { ExpressionBody: not null } localFunction => localFunction.ExpressionBody.Expression,
			PropertyDeclarationSyntax { ExpressionBody: not null } property => property.ExpressionBody.Expression,
			IndexerDeclarationSyntax { ExpressionBody: not null } indexer => indexer.ExpressionBody.Expression,
			_ => null,
		};

		return result;
	}

	private static ArchitectureDependencySiteIndicator CreateBehavioralOperationPolicyIndicator(CallerInfo caller, BehavioralOperationBodyAnalysis body, BehavioralOperationPolicyEvaluation evaluation)
	{
		var operation = evaluation.Occurrence?.Operation;
		var site = operation?.Site ?? GetBehavioralDeclarationSite(body.OwningSymbol);
		var span = operation?.Location.SourceSpan ?? body.DeclarationLocation.SourceSpan;
		var policyName = operation?.DisplayName
			?? evaluation.Rule.Kind + ": " + evaluation.Rule.DisplayName;
		var tooltip = site
			+ ": "
			+ caller.TypeName
			+ " ("
			+ caller.LayerPath
			+ ") violates behavioral policy "
			+ evaluation.Rule.DisplayName
			+ " - "
			+ evaluation.Reason;
		var result = new ArchitectureDependencySiteIndicator(
			span,
			site,
			caller.TypeName,
			caller.LayerPath,
			policyName,
			null,
			0,
			ArchitectureDependencySiteStatus.TypePolicyViolation,
			ArchitecturalDiagnosticIds.BehavioralOperationPolicyViolation,
			tooltip,
			evaluation.Reason);

		return result;
	}

	private static string GetBehavioralDeclarationSite(ISymbol symbol)
	{
		var result = symbol switch
		{
			IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor } => DependencySites.Constructor,
			IMethodSymbol { AssociatedSymbol: IPropertySymbol or IEventSymbol } => DependencySites.Property,
			IPropertySymbol => DependencySites.Property,
			_ => DependencySites.Method,
		};

		return result;
	}
}
