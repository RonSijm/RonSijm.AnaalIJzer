using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.ProjectArchitecture.SolutionTopology;
using RonSijm.AnaalIJzer.Workspace.Analysis;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ArchitectureHealthReportGenerator
{
    private static void AppendSolutionTopologyFindings(SolutionAnalysisResult result, List<ArchitectureFinding> findings)
    {
        foreach (var group in GroupByConfiguration(result.Projects))
        {
            var config = group.Projects[0].Config;
            if (!config.HasSolutionTopology)
            {
                continue;
            }

            var topology = SolutionTopologyAnalysisService.Analyze(config.SolutionTopology, result.EffectiveProjectReferences);
            foreach (var violation in topology.ReferenceViolations)
            {
                findings.Add(CreateSolutionTopologyReferenceFinding(violation));
            }

            foreach (var cycle in topology.ConfiguredCycles)
            {
                findings.Add(CreateSolutionTopologyCycleFinding(cycle));
            }
        }
    }

    private static ArchitectureFinding CreateSolutionTopologyReferenceFinding(SolutionTopologyReferenceViolation violation)
    {
        var sourceModule = violation.SourceModule ?? "unrecognized";
        var targetModule = violation.TargetModule ?? "unrecognized";
        var message = $"'{violation.Reference.SourceProjectName}' (module '{sourceModule}') may not reference '{violation.Reference.TargetProjectName}' (module '{targetModule}'): {violation.ViolationReason}";
        var context = FormatSolutionTopologyReferenceContext(violation);
        var properties = ImmutableDictionary<string, string?>.Empty
            .Add(ArchitectureDiagnosticProperties.PropertySourceProjectPath, violation.Reference.SourceProjectPath)
            .Add(ArchitectureDiagnosticProperties.PropertySourceProjectName, violation.Reference.SourceProjectName)
            .Add(ArchitectureDiagnosticProperties.PropertySourceSolutionModule, violation.SourceModule)
            .Add(ArchitectureDiagnosticProperties.PropertyTargetProjectPath, violation.Reference.TargetProjectPath)
            .Add(ArchitectureDiagnosticProperties.PropertyTargetProjectName, violation.Reference.TargetProjectName)
            .Add(ArchitectureDiagnosticProperties.PropertyTargetSolutionModule, violation.TargetModule)
            .Add(ArchitectureDiagnosticProperties.PropertyViolationReason, violation.ViolationReason);
        if (violation.MatchedRule is { } rule)
        {
            properties = properties
                .Add(ArchitectureDiagnosticProperties.PropertyRuleXmlPath, rule.XmlPath)
                .Add(ArchitectureDiagnosticProperties.PropertyRuleXmlLine, rule.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Add(ArchitectureDiagnosticProperties.PropertyRuleXmlCol, rule.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        var result = new ArchitectureFinding(
            ArchitectureFindingSeverity.Error,
            ArchitectureFindingCodes.SolutionTopologyReferenceViolation,
            message,
            context,
            properties: properties);

        return result;
    }

    private static ArchitectureFinding CreateSolutionTopologyCycleFinding(SolutionTopologyCycle cycle)
    {
        var message = $"Configured solution-module cycle: {cycle.GetDisplayPath()}.";
        var context = cycle.Rules.IsDefaultOrEmpty
            ? "enforceAcyclic is enabled"
            : string.Join("; ", cycle.Rules.Select(FormatSolutionTopologyRuleLocation));
        var properties = ImmutableDictionary<string, string?>.Empty
            .Add(ArchitectureDiagnosticProperties.PropertyCycleLayers, string.Join("|", cycle.Modules))
            .Add(ArchitectureDiagnosticProperties.PropertyCycleLength, cycle.Modules.Length.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Add(ArchitectureDiagnosticProperties.PropertyCycleScope, "SolutionTopology");
        var result = new ArchitectureFinding(
            ArchitectureFindingSeverity.Error,
            ArchitectureFindingCodes.SolutionTopologyCycle,
            message,
            context,
            properties: properties);

        return result;
    }

    private static string FormatSolutionTopologyReferenceContext(SolutionTopologyReferenceViolation violation)
    {
        var referenceContext = $"{violation.Reference.SourceProjectPath} -> {violation.Reference.TargetProjectPath}";
        var result = violation.MatchedRule is { } rule
            ? referenceContext + "; " + FormatSolutionTopologyRuleLocation(rule)
            : referenceContext;

        return result;
    }

    private static string FormatSolutionTopologyRuleLocation(SolutionModuleReferenceRule rule)
    {
        var location = rule.XmlLineNumber > 0 ? $"{rule.XmlPath}:{rule.XmlLineNumber}" : rule.XmlPath;
        var result = $"{rule.Kind} {rule.From} -> {rule.To} ({location})";

        return result;
    }
}