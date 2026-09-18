using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.EntryPoints;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Core.Violations;
using RonSijm.AnaalIJzer.Diagnostics;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine.Analysis.BoundaryRules.LayerDependencies;

public static partial class LayerDependencyAnalyzer
{
	private static bool IsIgnoredRecognitionType(ITypeSymbol type)
	{
		var result = type.SpecialType != SpecialType.None
		             || type.TypeKind is TypeKind.TypeParameter or TypeKind.Dynamic or TypeKind.Error;

		return result;
	}

	private static void ReportUnrecognizedDependency(SyntaxNodeAnalysisContext context, ConcurrentBag<ViolationRecord> violations, string callerTypeName, string callerLayerName, string dependencyTypeName, Location reportLocation, string site)
	{
		var properties = AddViolationProperties(
			ImmutableDictionary<string, string?>.Empty.Add(ArchitecturalDiagnostics.PropertySite, site),
			callerTypeName,
			callerLayerName,
			dependencyTypeName,
			string.Empty,
			string.Empty,
			null);

		context.ReportDiagnostic(ArchitecturalDiagnostics.CreateDiagnostic(
			ArchitecturalDiagnostics.DependencyRequiredMissing,
			reportLocation,
			properties,
			callerTypeName, callerLayerName, dependencyTypeName, string.Empty));

		violations.Add(new ViolationRecord(ArchitecturalDiagnosticIds.DependencyRequiredMissing, callerTypeName, callerLayerName, dependencyTypeName, string.Empty, string.Empty, null));
	}

	private static void ReportIllegalDependency(SyntaxNodeAnalysisContext context, ConcurrentBag<ViolationRecord> violations, string callerTypeName, string callerLayerName, string depTypeName, string depLayerName, Location reportLocation, string site, AnalyzerConfig config, ImmutableDictionary<string, string?> ruleProperties, DependencyEdgeEvaluation edgeEvaluation)
	{
		DiagnosticDescriptor descriptor;
		string diagnosticId;
		string reason;

		if (callerLayerName == depLayerName)
		{
			descriptor = ArchitecturalDiagnostics.DependencyPeerScope;
			diagnosticId = ArchitecturalDiagnosticIds.DependencyPeerScope;
			reason = edgeEvaluation.IsDeniedBySiteFilter ? edgeEvaluation.DenialReason : $"types in the same layer ('{callerLayerName}') may not depend on each other";
		}
		else if (edgeEvaluation.IsDeniedByBlockedEdge)
		{
			descriptor = ArchitecturalDiagnostics.DependencyNotAllowed;
			diagnosticId = ArchitecturalDiagnosticIds.DependencyNotAllowed;
			reason = edgeEvaluation.DenialReason;
		}
		else if (config.Graph.HasEdge(edgeEvaluation.ScopePath, depLayerName, callerLayerName))
		{
			descriptor = ArchitecturalDiagnostics.DependencyReverseDirection;
			diagnosticId = ArchitecturalDiagnosticIds.DependencyReverseDirection;
			reason = edgeEvaluation.IsDeniedBySiteFilter ? edgeEvaluation.DenialReason : $"this dependency goes the wrong direction — the reverse ('{depLayerName}' \u2192 '{callerLayerName}') is configured";
		}
		else
		{
			descriptor = ArchitecturalDiagnostics.DependencyNotAllowed;
			diagnosticId = ArchitecturalDiagnosticIds.DependencyNotAllowed;
			reason = edgeEvaluation.DenialReason;
		}

		var properties = AddViolationProperties(
			ruleProperties.Add(ArchitecturalDiagnostics.PropertySite, site),
			callerTypeName,
			callerLayerName,
			depTypeName,
			depLayerName,
			reason,
			null);
		properties = AddDependencyRuleProperties(properties, edgeEvaluation, site);
		if (diagnosticId == ArchitecturalDiagnosticIds.DependencyReverseDirection
		    && TryFindReverseDependencyEdge(config, edgeEvaluation.ScopePath, callerLayerName, depLayerName, out var reverseEdge))
		{
			properties = AddReverseDependencyRuleProperties(properties, reverseEdge);
		}

		context.ReportDiagnostic(ArchitecturalDiagnostics.CreateDiagnostic(
			descriptor,
			reportLocation,
			properties,
			callerTypeName, callerLayerName, depTypeName, depLayerName, reason));

		violations.Add(new ViolationRecord(diagnosticId, callerTypeName, callerLayerName, depTypeName, depLayerName, reason, null));
	}

	private static bool TryFindReverseDependencyEdge(AnalyzerConfig config, string scopePath, string callerLayerName, string dependencyLayerName, out DependencyEdge edge)
	{
		var result = config.Graph.TryFindAllowedEdge(scopePath, dependencyLayerName, callerLayerName, out edge);

		return result;
	}

	private static void ReportBoundaryEntryPointViolation(SyntaxNodeAnalysisContext context, ConcurrentBag<ViolationRecord> violations, string callerTypeName, string callerLayerName, string depTypeName, string depLayerName, Location reportLocation, string site, ImmutableDictionary<string, string?> ruleProperties, BoundaryEntryPointEvaluation evaluation)
	{
		var properties = AddViolationProperties(
			ruleProperties
				.SetItem(ArchitecturalDiagnostics.PropertySite, site)
				.SetItem(ArchitecturalDiagnostics.PropertyBoundaryLayerName, evaluation.BoundaryLayerName)
				.SetItem(ArchitecturalDiagnostics.PropertyMatchedEntryPoint, evaluation.MatchedEntryPoint)
				.SetItem(ArchitecturalDiagnostics.PropertyEntryPointFailureReason, evaluation.Reason)
				.SetItem(ArchitecturalDiagnostics.PropertyRuleXmlLine, evaluation.XmlLineNumber > 0 ? evaluation.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture) : null)
				.SetItem(ArchitecturalDiagnostics.PropertyRuleXmlCol, evaluation.XmlLinePosition > 0 ? evaluation.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture) : null)
				.SetItem(ArchitecturalDiagnostics.PropertyRuleXmlPath, evaluation.XmlPath),
			callerTypeName,
			callerLayerName,
			depTypeName,
			depLayerName,
			evaluation.Reason,
			null);

		context.ReportDiagnostic(ArchitecturalDiagnostics.CreateDiagnostic(
			ArchitecturalDiagnostics.BoundaryEntryPlacement,
			reportLocation,
			properties,
			callerTypeName, callerLayerName, depTypeName, depLayerName, evaluation.BoundaryLayerName, evaluation.Reason));

		violations.Add(new ViolationRecord(
			ArchitecturalDiagnosticIds.BoundaryEntryPlacement,
			callerTypeName,
			callerLayerName,
			depTypeName,
			depLayerName,
			$"boundary '{evaluation.BoundaryLayerName}': {evaluation.Reason}",
			null,
			boundaryLayerName: evaluation.BoundaryLayerName,
			matchedEntryPoint: evaluation.MatchedEntryPoint));
	}

    public static ImmutableDictionary<string, string?> AddViolationProperties(ImmutableDictionary<string, string?> properties, string callerTypeName, string callerLayerName, string depTypeName, string depLayerName, string violationReason, string? comment)
	{
		var result = properties
			.SetItem(ArchitecturalDiagnostics.PropertyCallerTypeName, callerTypeName)
			.SetItem(ArchitecturalDiagnostics.PropertyCallerLayerName, callerLayerName)
			.SetItem(ArchitecturalDiagnostics.PropertyDepTypeName, depTypeName)
			.SetItem(ArchitecturalDiagnostics.PropertyDepLayerName, depLayerName)
			.SetItem(ArchitecturalDiagnostics.PropertyViolationReason, violationReason)
			.SetItem(ArchitecturalDiagnostics.PropertyComment, comment);

		return result;
	}

	private static ImmutableDictionary<string, string?> AddDependencyRuleProperties(ImmutableDictionary<string, string?> properties, DependencyEdgeEvaluation edgeEvaluation, string site)
	{
		var result = properties.SetItem(ArchitecturalDiagnostics.PropertyDependencyDenialKind, edgeEvaluation.DenialKind.ToString());
		if (edgeEvaluation.DeniedByEdge is not { } edge)
		{
			return result;
		}

		result = result
			.SetItem(ArchitecturalDiagnostics.PropertyDependencyRuleKind, edge.IsBlocked ? "BlockedDependency" : "AllowedDependency")
			.SetItem(ArchitecturalDiagnostics.PropertyDependencyRuleXmlPath, edge.XmlPath)
			.SetItem(ArchitecturalDiagnostics.PropertyDependencyRuleScopePath, edge.ScopePath)
			.SetItem(ArchitecturalDiagnostics.PropertyDependencyRuleConfiguredFrom, edge.ConfiguredFrom)
			.SetItem(ArchitecturalDiagnostics.PropertyDependencyRuleConfiguredTo, edge.ConfiguredTo)
			.SetItem(ArchitecturalDiagnostics.PropertyDependencyRuleXmlLine, edge.XmlLineNumber > 0 ? edge.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture) : null)
			.SetItem(ArchitecturalDiagnostics.PropertyDependencyRuleXmlCol, edge.XmlLinePosition > 0 ? edge.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture) : null);

		if (edgeEvaluation.DenialKind == DependencyDenialKind.SiteFilter)
		{
			var filterMode = edge.SiteFilter.AllowedSites.Count > 0 && !edge.SiteFilter.AllowedSites.Contains(site)
				? "AllowedSites"
				: edge.SiteFilter.BlockedSites.Contains(site)
					? "BlockedSites"
					: null;
			result = result.SetItem(ArchitecturalDiagnostics.PropertyDependencySiteFilterMode, filterMode);
		}

		return result;
	}

	private static ImmutableDictionary<string, string?> AddReverseDependencyRuleProperties(ImmutableDictionary<string, string?> properties, DependencyEdge edge)
	{
		var result = properties
			.SetItem(ArchitecturalDiagnostics.PropertyReverseDependencyRuleKind, edge.IsBlocked ? "BlockedDependency" : "AllowedDependency")
			.SetItem(ArchitecturalDiagnostics.PropertyReverseDependencyRuleXmlPath, edge.XmlPath)
			.SetItem(ArchitecturalDiagnostics.PropertyReverseDependencyRuleScopePath, edge.ScopePath)
			.SetItem(ArchitecturalDiagnostics.PropertyReverseDependencyRuleConfiguredFrom, edge.ConfiguredFrom)
			.SetItem(ArchitecturalDiagnostics.PropertyReverseDependencyRuleConfiguredTo, edge.ConfiguredTo)
			.SetItem(ArchitecturalDiagnostics.PropertyReverseDependencyRuleXmlLine, edge.XmlLineNumber > 0 ? edge.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture) : null)
			.SetItem(ArchitecturalDiagnostics.PropertyReverseDependencyRuleXmlCol, edge.XmlLinePosition > 0 ? edge.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture) : null);

		return result;
	}

	private static ImmutableDictionary<string, string?> BuildRuleProperties(LayerMatch depMatch, string depTypeName)
	{
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitecturalDiagnostics.PropertyDepTypeName, depTypeName);

		if (depMatch.XmlLineNumber > 0)
		{
			properties = properties
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, depMatch.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture))
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, depMatch.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture))
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlPath, depMatch.XmlPath);
		}

		return properties;
	}

	private static ImmutableDictionary<string, string?> BuildRuleProperties(MatcherRule rule, string depTypeName)
	{
		var properties = ImmutableDictionary<string, string?>.Empty.Add(ArchitecturalDiagnostics.PropertyDepTypeName, depTypeName);
		if (rule.XmlLineNumber > 0)
		{
			properties = properties
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, rule.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture))
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, rule.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture))
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlPath, rule.XmlPath);
		}

		return properties;
	}
}
