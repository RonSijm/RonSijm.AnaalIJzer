using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Core.Observations;
using RonSijm.AnaalIJzer.Core.RuntimeConfig.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Core.Violations;
using RonSijm.AnaalIJzer.Diagnostics;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine.Analysis.BoundaryRules.LayerDependencies;

public static partial class LayerDependencyAnalyzer
{
	private static void AnalyzeTypeReference(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ObservedDependencyCollector? observedDependencies, string callerTypeName, LayerMatch callerMatch, Location reportLocation, ITypeSymbol depType, string site)
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

			if (depTypeName == callerTypeName || IsIgnoredRecognitionType(current))
			{
				outerTypeIsIgnored |= isOuter;
				continue;
			}

			var depNamespace = current.ContainingNamespace?.ToString() ?? string.Empty;
			var depMatch = config.Engine.FindLayer(depTypeName, depNamespace, current);

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
			if (!depLayer.IsForbidden)
			{
				observedDependencies?.Record(callerTypeName, callerLayer.Name, depTypeName, depLayer.Name, effectiveSite, reportLocation);
			}

			var ruleProperties = BuildRuleProperties(depMatch.Value, depTypeName);
			var decision = DependencyRuleEvaluator.Evaluate(config, callerMatch, depMatch.Value, current, effectiveSite);

			if (decision.IsForbiddenLayer)
			{
				var properties = AddViolationProperties(
					ruleProperties.Add(ArchitecturalDiagnostics.PropertySite, effectiveSite),
					callerTypeName,
					callerLayer.Name,
					depTypeName,
					depLayer.Name,
					decision.Reason,
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
					callerTypeName, callerLayer.Name, depTypeName, decision.Reason));

				violations.Add(new ViolationRecord(ArchitecturalDiagnosticIds.ForbiddenDependency, callerTypeName, callerLayer.Name, depTypeName, depLayer.Name, decision.Reason, depLayer.Comment));
				continue;
			}

			if (decision.TypePolicyViolation is { } policyViolation)
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

			if (decision.IsAllowed)
			{
				if (config.Engine.HasEntryPointPolicies)
				{
					var entryPointEvaluation = config.Engine.EvaluateBoundaryEntryPoints(callerMatch, depMatch.Value, depTypeName, depNamespace, current, effectiveSite);
					if (!entryPointEvaluation.IsAllowed)
					{
						ReportBoundaryEntryPointViolation(context, violations, callerTypeName, callerLayer.Name, depTypeName, depLayer.Name, reportLocation, effectiveSite, ruleProperties, entryPointEvaluation);
					}
				}

				continue;
			}

			ReportIllegalDependency(context, violations, callerTypeName, callerLayer.Name, depTypeName, depLayer.Name, reportLocation, effectiveSite, config, ruleProperties, decision.EdgeEvaluation!.Value);
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
