using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Diagnostics;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.Violations;
using AnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer;

internal static partial class LayerDependencyAnalyzer
{
	private static void AnalyzeParameters(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, string callerTypeName, string callerNamespace, SeparatedSyntaxList<ParameterSyntax> parameters, string site)
	{
		var typeDeclaration = parameters.Count > 0 ? parameters[0].FirstAncestorOrSelf<TypeDeclarationSyntax>() : null;
		var callerSymbol = typeDeclaration is null ? null : context.SemanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken) as ITypeSymbol;

		var callerMatch = config.FindLayer(callerTypeName, callerNamespace, callerSymbol);
		if (callerMatch is null)
		{
			return;
		}

		// Forbidden types are never considered callers.
		if (callerMatch.Value.Layer.IsForbidden)
		{
			return;
		}

		foreach (var param in parameters)
		{
			var paramSymbol = context.SemanticModel.GetDeclaredSymbol(param, context.CancellationToken);
			if (paramSymbol is null)
			{
				continue;
			}

			AnalyzeTypeReference(context, config, violations, callerTypeName, callerMatch.Value, param.GetLocation(), paramSymbol.Type, site);
		}
	}

	/// <summary>
	///     Resolves the enclosing class declaration of <paramref name="node" /> to a layer.
	///     Returns <c>null</c> when the enclosing type is not configured or is a forbidden type
	///     (forbidden types never act as callers). Threads the class symbol through so semantic
	///     matchers (<c>inherits</c>, <c>implements</c>, <c>withAttribute</c>,
	///     <c>withAccessModifier</c>) can classify the caller.
	/// </summary>
	private static (string TypeName, LayerMatch Match)? TryGetCallerLayer(SyntaxNodeAnalysisContext context, AnalyzerConfig config, SyntaxNode node)
	{
		var typeDeclaration = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
		if (typeDeclaration is null)
		{
			return null;
		}

		var callerName = typeDeclaration.Identifier.ValueText;
		var callerNs = GetContainingNamespace(typeDeclaration);
		var callerSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken) as ITypeSymbol;
		var match = config.FindLayer(callerName, callerNs, callerSymbol);
		if (match is null || match.Value.Layer.IsForbidden)
		{
			return null;
		}

		return (callerName, match.Value);
	}

	/// <summary>
	///     Evaluates the rules for a single dependency type, considering the outer type and
	///     every generic type argument (recursively). Each distinct type name produces at most
	///     one diagnostic at <paramref name="reportLocation" />. ARCH002 is emitted for unrecognized
	///     types only when root-level or caller-layer <c>requireRecognizedDependencies</c>
	///     includes the effective site.
	/// </summary>
	private static void AnalyzeTypeReference(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, string callerTypeName, LayerMatch callerMatch, Location reportLocation, ITypeSymbol depType, string site)
	{
		var callerLayer = callerMatch.Layer;
		var seenDepTypeNames = new HashSet<string>(StringComparer.Ordinal);
		var unrecognizedGenericArguments = new List<ITypeSymbol>();
		var matchedAnyLayer = false;
		var outerTypeIsIgnored = false;
		var index = 0;

		foreach (var current in EnumerateTypeAndGenericArguments(depType))
		{
			var isOuter = index++ == 0;
			var effectiveSite = isOuter ? site : DependencySites.GenericArgument;

			var depTypeName = current.Name;
			if (string.IsNullOrEmpty(depTypeName))
			{
				continue;
			}

			// Self-references, primitives, void, and open type parameters are not architectural dependencies.
			if (depTypeName == callerTypeName || IsIgnoredRecognitionType(current))
			{
				outerTypeIsIgnored |= isOuter;
				continue;
			}

			var depNamespace = current.ContainingNamespace?.ToString() ?? string.Empty;
			var depMatch = config.FindLayer(depTypeName, depNamespace, current);

			if (depMatch is null)
			{
				if (!isOuter)
				{
					unrecognizedGenericArguments.Add(current);
				}
				continue;
			}

			matchedAnyLayer = true;

			if (!seenDepTypeNames.Add(depTypeName))
			{
				continue;
			}

			var (depLayer, matchedSuffix) = (depMatch.Value.Layer, depMatch.Value.MatchedSuffix);
			var ruleProperties = BuildRuleProperties(depMatch.Value, depTypeName);

			if (depLayer.IsForbidden)
			{
				var reason = "the type matches a global <Forbidden> rule";
				if (depLayer.Comment is not null)
				{
					reason += $": {depLayer.Comment}";
				}

				// Include rename fix metadata when the matched suffix has a <Fix Rename="..."> defined.
				// Only attach for the outer type — the code-fix provider renames the parameter's
				// declared type, so a suffix taken from a nested generic argument would mis-target.
				var properties = AddViolationProperties(
					ruleProperties.Add(ArchitecturalDiagnostics.PropertySite, effectiveSite),
					callerTypeName,
					callerLayer.Name,
					depTypeName,
					depLayer.Name,
					reason,
					depLayer.Comment);
				if (isOuter && matchedSuffix is not null && depLayer.FixSuffix is not null)
				{
					properties = properties
						.Add(ArchitecturalDiagnostics.PropertyMatchedSuffix, matchedSuffix)
						.Add(ArchitecturalDiagnostics.PropertyFixSuffix, depLayer.FixSuffix);
				}

				context.ReportDiagnostic(Diagnostic.Create(
					ArchitecturalDiagnostics.ForbiddenDependency,
					reportLocation,
					properties,
					callerTypeName, callerLayer.Name, depTypeName, reason));

				violations.Add(new ViolationRecord(ArchitecturalDiagnosticIds.ForbiddenDependency, callerTypeName, callerLayer.Name, depTypeName, depLayer.Name, reason, depLayer.Comment));

				continue;
			}

			var typePolicyViolation = config.EvaluateTypePolicy(depMatch.Value, depTypeName, depNamespace, current);
			if (typePolicyViolation is { } policyViolation)
			{
				var policyRuleProperties = policyViolation.Rule is { } policyRule
					? BuildRuleProperties(policyRule, depTypeName)
					: ImmutableDictionary<string, string?>.Empty.Add(ArchitecturalDiagnostics.PropertyDepTypeName, depTypeName);
				var properties = AddViolationProperties(
					policyRuleProperties.Add(ArchitecturalDiagnostics.PropertySite, effectiveSite),
					callerTypeName,
					callerLayer.Name,
					depTypeName,
					policyViolation.DependencyLayerName,
					policyViolation.Reason,
					policyViolation.Comment);

				if (isOuter && policyViolation.Rule is { } matchedRule && policyViolation.MatchedSuffix is not null && matchedRule.Layer.FixSuffix is not null)
				{
					properties = properties
						.Add(ArchitecturalDiagnostics.PropertyMatchedSuffix, policyViolation.MatchedSuffix)
						.Add(ArchitecturalDiagnostics.PropertyFixSuffix, matchedRule.Layer.FixSuffix);
				}

				context.ReportDiagnostic(Diagnostic.Create(
					ArchitecturalDiagnostics.ForbiddenDependency,
					reportLocation,
					properties,
					callerTypeName, callerLayer.Name, depTypeName, policyViolation.Reason));

				violations.Add(new ViolationRecord(ArchitecturalDiagnosticIds.ForbiddenDependency, callerTypeName, callerLayer.Name, depTypeName, policyViolation.DependencyLayerName, policyViolation.Reason, policyViolation.Comment));
				continue;
			}

			var edgeEvaluation = config.Graph.EvaluateDependency(callerMatch, depMatch.Value, effectiveSite);
			if (edgeEvaluation.IsAllowed)
			{
				continue;
			}

			ReportIllegalDependency(context, violations, callerTypeName, callerLayer.Name, depTypeName, depLayer.Name, reportLocation, effectiveSite, config, ruleProperties, edgeEvaluation);
		}

		if (!matchedAnyLayer && !outerTypeIsIgnored && config.RequiresRecognizedDependencyAt(callerMatch, site))
		{
			ReportUnrecognizedDependency(context, violations, callerTypeName, callerLayer.Name, depType.Name, reportLocation, site);
		}

		if (!config.RequiresRecognizedDependencyAt(callerMatch, DependencySites.GenericArgument))
		{
			return;
		}

		foreach (var argument in unrecognizedGenericArguments)
		{
			if (seenDepTypeNames.Add(argument.Name))
			{
				ReportUnrecognizedDependency(context, violations, callerTypeName, callerLayer.Name, argument.Name, reportLocation, DependencySites.GenericArgument);
			}
		}
	}
}
