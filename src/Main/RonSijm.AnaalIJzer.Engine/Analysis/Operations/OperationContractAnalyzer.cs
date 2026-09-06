using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Observations;
using RonSijm.AnaalIJzer.Core.OperationContracts.Analysis;
using RonSijm.AnaalIJzer.Core.OperationContracts.Evaluation;
using RonSijm.AnaalIJzer.Core.OperationContracts.Model;
using RonSijm.AnaalIJzer.Diagnostics;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine.Analysis.Operations;

/// <summary>Checks only the explicit source relationships declared in root-level Operations.</summary>
internal static class OperationContractAnalyzer
{
	internal static void AnalyzeCompilation(CompilationAnalysisContext context, AnalyzerConfig config)
	{
		var methods = GetProjectMethods(context.Compilation, config, context.CancellationToken);
		foreach (var definition in config.OperationContracts.Definitions)
		{
			foreach (var owner in methods.Where(method => OperationContractEvaluator.MatchesOwner(definition, method)))
			{
				var layerPath = GetLayerPath(config, owner.ContainingType);
				foreach (var evaluation in OperationContractEvaluator.EvaluateOwner(definition, owner, layerPath))
				{
					Report(context, owner, layerPath, evaluation);
				}
			}

			foreach (var entryPoint in methods.Where(method => definition.EntryPoints.Any(selector => OperationContractEvaluator.MatchesEntryPoint(selector, method))))
			{
				var layerPath = GetLayerPath(config, entryPoint.ContainingType);
				var invokesOwner = DirectlyInvokesOwner(context.Compilation, entryPoint, definition, config, context.CancellationToken);
				foreach (var evaluation in OperationContractEvaluator.EvaluateEntryPoint(definition, entryPoint, layerPath, invokesOwner))
				{
					Report(context, entryPoint, layerPath, evaluation);
				}
			}
		}
	}

	private static ImmutableArray<IMethodSymbol> GetProjectMethods(Compilation compilation, AnalyzerConfig config, CancellationToken cancellationToken)
	{
		var methods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
		foreach (var type in CompilationTypeCollector.GetProjectTypes(compilation, config.GeneratedCodeScope, cancellationToken))
		{
			foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
			{
				if (method.MethodKind != MethodKind.Ordinary || !HasAnalyzableSource(method, config, cancellationToken))
				{
					continue;
				}

				methods.Add(method);
			}
		}

		var result = methods
			.OrderBy(method => GetSourceLocation(method)?.SourceTree?.FilePath, StringComparer.OrdinalIgnoreCase)
			.ThenBy(method => GetSourceLocation(method)?.SourceSpan.Start ?? int.MaxValue)
			.ToImmutableArray();

		return result;
	}

	private static bool DirectlyInvokesOwner(Compilation compilation, IMethodSymbol entryPoint, OperationContractDefinition definition, AnalyzerConfig config, CancellationToken cancellationToken)
	{
		foreach (var syntaxReference in entryPoint.DeclaringSyntaxReferences)
		{
			var declaration = syntaxReference.GetSyntax(cancellationToken);
			if (!config.GeneratedCodeScope.ShouldAnalyze(declaration.SyntaxTree, cancellationToken))
			{
				continue;
			}

			var semanticModel = compilation.GetSemanticModel(declaration.SyntaxTree);
			if (OperationContractInvocationInspector.DirectlyInvokesOwner(semanticModel, declaration, definition, cancellationToken))
			{
				return true;
			}
		}

		return false;
	}

	private static bool HasAnalyzableSource(IMethodSymbol method, AnalyzerConfig config, CancellationToken cancellationToken)
	{
		foreach (var syntaxReference in method.DeclaringSyntaxReferences)
		{
			if (config.GeneratedCodeScope.ShouldAnalyze(syntaxReference.SyntaxTree, cancellationToken))
			{
				return true;
			}
		}

		return false;
	}

	private static string? GetLayerPath(AnalyzerConfig config, INamedTypeSymbol containingType)
	{
		var namespaceName = containingType.ContainingNamespace.IsGlobalNamespace ? string.Empty : containingType.ContainingNamespace.ToDisplayString();
		var result = config.FindLayer(containingType.Name, namespaceName, containingType)?.Layer.Name;

		return result;
	}

	private static void Report(CompilationAnalysisContext context, IMethodSymbol method, string? layerPath, OperationContractEvaluation evaluation)
	{
		var definition = evaluation.Definition;
		var location = GetSourceLocation(method) ?? Location.None;
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitecturalDiagnostics.PropertyCallerTypeName, method.ContainingType.Name)
			.Add(ArchitecturalDiagnostics.PropertyCallerLayerName, layerPath ?? "Unclassified")
			.Add(ArchitecturalDiagnostics.PropertyDeclaredSymbolName, method.Name)
			.Add(ArchitecturalDiagnostics.PropertyDeclarationTarget, evaluation.ParticipantRole.ToString())
			.Add(ArchitecturalDiagnostics.PropertyOperationContractName, definition.Name)
			.Add(ArchitecturalDiagnostics.PropertyOperationContractParticipantRole, evaluation.ParticipantRole.ToString())
			.Add(ArchitecturalDiagnostics.PropertyOperationContractViolationKind, evaluation.ViolationKind.ToString())
			.Add(ArchitecturalDiagnostics.PropertyViolationReason, evaluation.Reason)
			.Add(ArchitecturalDiagnostics.PropertyComment, definition.Description)
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlPath, definition.XmlPath)
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, definition.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture))
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, definition.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture));
		context.ReportDiagnostic(Diagnostic.Create(
			ArchitecturalDiagnostics.OperationContractViolation,
			location,
			properties,
			method.ContainingType.Name,
			layerPath ?? "Unclassified",
			definition.Name,
			evaluation.ParticipantRole.ToString(),
			evaluation.Reason));
	}

	private static Location? GetSourceLocation(IMethodSymbol method)
	{
		var result = method.Locations.FirstOrDefault(location => location.IsInSource);

		return result;
	}
}
