using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.NamespaceHierarchy.Policies;
using RonSijm.AnaalIJzer.Core.Violations;
using RonSijm.AnaalIJzer.Diagnostics;

namespace RonSijm.AnaalIJzer.Engine.Analysis.BoundaryRules.LayerDependencies;

public static partial class LayerDependencyAnalyzer
{
    private static void ReportNamespaceHierarchyViolation(SyntaxNodeAnalysisContext context, ConcurrentBag<ViolationRecord> violations, CallerDependencyContext caller, string dependencyTypeName, string dependencyNamespace, Location reportLocation, string site, NamespaceHierarchyEvaluation evaluation)
    {
        var rule = evaluation.Rule;
        var comment = rule.Description ?? evaluation.Policy.Description;
        var properties = ImmutableDictionary<string, string?>.Empty
            .Add(ArchitecturalDiagnostics.PropertyCallerTypeName, caller.TypeName)
            .Add(ArchitecturalDiagnostics.PropertyDepTypeName, dependencyTypeName)
            .Add(ArchitecturalDiagnostics.PropertyCallerNamespace, caller.NamespaceName)
            .Add(ArchitecturalDiagnostics.PropertyDependencyNamespace, dependencyNamespace)
            .Add(ArchitecturalDiagnostics.PropertyNamespaceHierarchyRoot, evaluation.Policy.RootNamespace)
            .Add(ArchitecturalDiagnostics.PropertyNamespaceHierarchyRelation, evaluation.Relation.ToString())
            .Add(ArchitecturalDiagnostics.PropertySite, site)
            .Add(ArchitecturalDiagnostics.PropertyViolationReason, evaluation.Reason)
            .Add(ArchitecturalDiagnostics.PropertyComment, comment)
            .Add(ArchitecturalDiagnostics.PropertyNamespaceHierarchyRuleXmlPath, rule.XmlPath)
            .Add(ArchitecturalDiagnostics.PropertyNamespaceHierarchyRuleXmlLine, rule.XmlLineNumber > 0 ? rule.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture) : null)
            .Add(ArchitecturalDiagnostics.PropertyNamespaceHierarchyRuleXmlCol, rule.XmlLinePosition > 0 ? rule.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture) : null);

        context.ReportDiagnostic(ArchitecturalDiagnostics.CreateDiagnostic(
            ArchitecturalDiagnostics.NamespaceBoundaryPlacement,
            reportLocation,
            properties,
            caller.TypeName,
            caller.NamespaceName,
            dependencyTypeName,
            dependencyNamespace,
            site,
            evaluation.Reason));

        violations.Add(new ViolationRecord(
            ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement,
            caller.TypeName,
            caller.NamespaceName,
            dependencyTypeName,
            dependencyNamespace,
            evaluation.Reason,
            comment,
            site));
    }
}