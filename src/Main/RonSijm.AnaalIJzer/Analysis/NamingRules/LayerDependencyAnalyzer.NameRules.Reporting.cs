using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Diagnostics;
using RonSijm.AnaalIJzer.Engine.NameRules;
using RonSijm.AnaalIJzer.Violations;

namespace RonSijm.AnaalIJzer;

internal static partial class LayerDependencyAnalyzer
{
	private static void ReportNameRuleViolation(SyntaxNodeAnalysisContext context, ConcurrentBag<ViolationRecord> violations, string callerTypeName, string callerLayerName, NameRuleViolation violation, Location reportLocation)
	{
		var properties = AddViolationProperties(
				ImmutableDictionary<string, string?>.Empty
					.Add(ArchitecturalDiagnostics.PropertySite, violation.Site)
					.Add(ArchitecturalDiagnostics.PropertyNameRuleKind, violation.RuleKind.ToString())
					.Add(ArchitecturalDiagnostics.PropertySourceName, violation.SourceName)
					.Add(ArchitecturalDiagnostics.PropertyTargetName, violation.TargetName)
					.Add(ArchitecturalDiagnostics.PropertyNormalizedSourceName, violation.NormalizedSourceName)
					.Add(ArchitecturalDiagnostics.PropertyNormalizedTargetName, violation.NormalizedTargetName)
					.Add(ArchitecturalDiagnostics.PropertyTypeName, violation.RuleKind == NameRuleKind.RequireDeclarationNameMatchesType ? violation.SourceName : null)
					.Add(ArchitecturalDiagnostics.PropertyDeclaredName, violation.RuleKind == NameRuleKind.RequireDeclarationNameMatchesType ? violation.TargetName : null)
					.Add(ArchitecturalDiagnostics.PropertyRuleXmlPath, violation.XmlPath)
					.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, violation.XmlLineNumber.ToString())
					.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, violation.XmlLinePosition.ToString()),
				callerTypeName,
				callerLayerName,
				violation.TargetName,
				violation.LayerName,
				violation.Reason,
				null);

		context.ReportDiagnostic(Diagnostic.Create(
			ArchitecturalDiagnostics.NameRuleViolation,
			reportLocation,
			properties,
			callerTypeName, callerLayerName, violation.RuleKind, violation.Site, violation.Reason));

		violations.Add(new ViolationRecord(ArchitecturalDiagnosticIds.NameRuleViolation, callerTypeName, callerLayerName, violation.SourceName, violation.TargetName, violation.Reason, null));
	}
}
