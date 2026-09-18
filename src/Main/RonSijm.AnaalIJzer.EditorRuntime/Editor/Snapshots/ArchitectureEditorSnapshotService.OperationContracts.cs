using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.Core.OperationContracts.Analysis;
using RonSijm.AnaalIJzer.Core.OperationContracts.Evaluation;
using RonSijm.AnaalIJzer.Core.OperationContracts.Model;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.EditorRuntime.Editor.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static void AddOperationContractIndicators(SyntaxNode syntaxRoot, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureDependencySiteIndicator>.Builder indicators, CancellationToken cancellationToken)
	{
		var analyzedMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
		foreach (var declaration in syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>())
		{
			if (semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not IMethodSymbol { MethodKind: MethodKind.Ordinary } method
				|| !analyzedMethods.Add(method))
			{
				continue;
			}

			var layerPath = GetOperationContractLayerPath(config, method.ContainingType);
			foreach (var definition in config.OperationContracts.Definitions)
			{
				if (OperationContractEvaluator.MatchesOwner(definition, method))
				{
					foreach (var evaluation in OperationContractEvaluator.EvaluateOwner(definition, method, layerPath))
					{
						indicators.Add(CreateOperationContractIndicator(method, declaration, layerPath, evaluation));
					}
				}

				if (!definition.EntryPoints.Any(selector => OperationContractEvaluator.MatchesEntryPoint(selector, method)))
				{
					continue;
				}

				var invokesOwner = OperationContractInvocationInspector.DirectlyInvokesOwner(semanticModel, declaration, definition, cancellationToken);
				foreach (var evaluation in OperationContractEvaluator.EvaluateEntryPoint(definition, method, layerPath, invokesOwner))
				{
					indicators.Add(CreateOperationContractIndicator(method, declaration, layerPath, evaluation));
				}
			}
		}
	}

	private static string GetOperationContractLayerPath(ProjectAnalyzerConfig config, INamedTypeSymbol type)
	{
		var namespaceName = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();
		var result = config.FindLayer(type.Name, namespaceName, type)?.Layer.Name ?? "Unclassified";

		return result;
	}

	private static ArchitectureDependencySiteIndicator CreateOperationContractIndicator(IMethodSymbol method, MethodDeclarationSyntax declaration, string layerPath, OperationContractEvaluation evaluation)
	{
		var definition = evaluation.Definition;
		var diagnosticId = evaluation.ViolationKind switch
		{
			OperationContractViolationKind.OwnerOutsideAllowedLayer or OperationContractViolationKind.EntryPointOutsideAllowedLayer => ArchitecturalDiagnosticIds.OperationContractNotAllowed,
			OperationContractViolationKind.OwnerInvalidResponse or OperationContractViolationKind.EntryPointInvalidResponse => ArchitecturalDiagnosticIds.OperationContractShapeMismatch,
			_ => ArchitecturalDiagnosticIds.OperationContractRequiredMissing
		};
		var tooltip = DependencySites.Method
			+ ": "
			+ method.ContainingType.Name
			+ " ("
			+ layerPath
			+ ") violates operation contract "
			+ definition.Name
			+ " - "
			+ evaluation.Reason;
		var result = new ArchitectureDependencySiteIndicator(
			declaration.Identifier.Span,
			DependencySites.Method,
			method.ContainingType.Name,
			layerPath,
			definition.Name,
			null,
			0,
			ArchitectureDependencySiteStatus.TypePolicyViolation,
			diagnosticId,
			tooltip,
			evaluation.Reason);

		return result;
	}
}
