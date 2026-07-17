using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.DependencyRules;
using RonSijm.AnaalIJzer.Violations;
using AnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer;

internal static partial class LayerDependencyAnalyzer
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

		context.ReportDiagnostic(Diagnostic.Create(
			ArchitecturalDiagnostics.UnrecognizedDependency,
			reportLocation,
			properties,
			callerTypeName, callerLayerName, dependencyTypeName, string.Empty));

		violations.Add(new ViolationRecord(ArchitecturalDiagnosticIds.UnrecognizedDependency, callerTypeName, callerLayerName, dependencyTypeName, string.Empty, string.Empty, null));
	}

	/// <summary>
	///     Picks ARCH001 / ARCH004 / ARCH005 based on the semantic reason the dependency is
	///     illegal and reports the diagnostic with a <c>Site</c> property attached.
	/// </summary>
	private static void ReportIllegalDependency(SyntaxNodeAnalysisContext context, ConcurrentBag<ViolationRecord> violations, string callerTypeName, string callerLayerName, string depTypeName, string depLayerName, Location reportLocation, string site, AnalyzerConfig config, ImmutableDictionary<string, string?> ruleProperties, DependencyEdgeEvaluation edgeEvaluation)
	{
		DiagnosticDescriptor descriptor;
		string diagnosticId;
		string reason;

		if (callerLayerName == depLayerName)
		{
			descriptor = ArchitecturalDiagnostics.SameLayerDependency;
			diagnosticId = ArchitecturalDiagnosticIds.SameLayerDependency;
			reason = edgeEvaluation.IsDeniedBySiteFilter ? edgeEvaluation.DenialReason : $"types in the same layer ('{callerLayerName}') may not depend on each other";
		}
		else if (edgeEvaluation.IsDeniedByBlockedEdge)
		{
			descriptor = ArchitecturalDiagnostics.IllegalDependency;
			diagnosticId = ArchitecturalDiagnosticIds.IllegalLevelDependency;
			reason = edgeEvaluation.DenialReason;
		}
		else if (config.Graph.HasEdge(edgeEvaluation.ScopePath, depLayerName, callerLayerName))
		{
			descriptor = ArchitecturalDiagnostics.WrongDirectionDependency;
			diagnosticId = ArchitecturalDiagnosticIds.WrongDirectionDependency;
			reason = edgeEvaluation.IsDeniedBySiteFilter ? edgeEvaluation.DenialReason : $"this dependency goes the wrong direction — the reverse ('{depLayerName}' \u2192 '{callerLayerName}') is configured";
		}
		else
		{
			descriptor = ArchitecturalDiagnostics.IllegalDependency;
			diagnosticId = ArchitecturalDiagnosticIds.IllegalLevelDependency;
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

		context.ReportDiagnostic(Diagnostic.Create(
			descriptor,
			reportLocation,
			properties,
			callerTypeName, callerLayerName, depTypeName, depLayerName, reason));

		violations.Add(new ViolationRecord(diagnosticId, callerTypeName, callerLayerName, depTypeName, depLayerName, reason, null));
	}

	private static ImmutableDictionary<string, string?> AddViolationProperties(ImmutableDictionary<string, string?> properties, string callerTypeName, string callerLayerName, string depTypeName, string depLayerName, string violationReason, string? comment)
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

	/// <summary>
	///     Builds the rule-location property bag attached to ARCH001/003/004/005 diagnostics
	///     so the "Add to exceptions" code fix can find the originating
	///     matcher tag in <c>Architecture.anl</c>
	///     and knows which type name to insert as the new exception entry.
	/// </summary>
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

	/// <summary>
	///     Yields <paramref name="root" /> followed by every generic type argument and array
	///     element type, recursively. Duplicate symbols are visited only once.
	/// </summary>
}
