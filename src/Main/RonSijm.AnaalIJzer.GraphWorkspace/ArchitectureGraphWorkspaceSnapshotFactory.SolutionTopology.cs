using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.ProjectArchitecture.SolutionTopology;
using RonSijm.AnaalIJzer.GraphModel.Model;
using RonSijm.AnaalIJzer.Workspace.Analysis;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.GraphWorkspace;

internal static partial class ArchitectureGraphWorkspaceSnapshotFactory
{
    private const string SolutionTopologyRootPath = "SolutionTopology";
    private const string SolutionTopologyPathPrefix = "SolutionTopology/";
    private const string UnrecognizedProjectPathPrefix = "SolutionTopology/Unrecognized/";

    private static ArchitectureGraphSnapshot AttachSolutionTopology(
        ArchitectureGraphSnapshot configSnapshot,
        AnalyzerConfiguration config,
        SolutionAnalysisResult? solution,
        ArchitectureGraphEvidence evidence,
        ImmutableArray<ArchitectureGraphExceptionReview> exceptionReviews)
    {
        var topologyLayers = CreateSolutionTopologyLayers(config.SolutionTopology, solution);
        var topologyRules = CreateSolutionTopologyRules(config.SolutionTopology);
        var topologyEvidence = CreateSolutionTopologyEvidence(config.SolutionTopology, solution);
        var combinedEvidence = new ArchitectureGraphEvidence(
            evidence.Types,
            evidence.Dependencies.AddRange(topologyEvidence));
        var result = new ArchitectureGraphSnapshot(
            configSnapshot.HasConfiguration,
            configSnapshot.HasConfigurationIssues,
            configSnapshot.Layers.AddRange(topologyLayers),
            configSnapshot.Rules.AddRange(topologyRules),
            configSnapshot.ActiveLayerPaths,
            configSnapshot.ConfigurationIssueMessages,
            configSnapshot.ConfigurationSource,
            combinedEvidence,
            configSnapshot.ConfigurationCreationTargets,
            exceptionReviews,
            hasSolutionTopology: true);

        return result;
    }

    private static ImmutableArray<ArchitectureGraphLayer> CreateSolutionTopologyLayers(SolutionTopologyConfig topology, SolutionAnalysisResult? solution)
    {
        var layers = ImmutableArray.CreateBuilder<ArchitectureGraphLayer>();
        layers.Add(new ArchitectureGraphLayer(
            SolutionTopologyRootPath,
            "Solution topology",
            "Configured solution modules and their allowed or blocked project references.",
            0,
            1,
            false,
            kind: ArchitectureGraphNodeKind.SolutionModule,
            readOnlyDetails: "Read-only solution topology projection. Edit <SolutionTopology> in the AnaalIJzer settings file to change modules or rules."));
        for (var index = 0; index < topology.Modules.Length; index++)
        {
            var module = topology.Modules[index];
            var assignedProjects = GetModuleProjects(module, solution);
            layers.Add(new ArchitectureGraphLayer(
                GetSolutionModulePath(module.Name),
                module.Name,
                module.Description,
                1,
                index % 16 + 1,
                false,
                module.XmlPath,
                ArchitectureConfigurationSourceKind.None,
                module.XmlLineNumber,
                ArchitectureGraphNodeKind.SolutionModule,
                CreateModuleDetails(module, assignedProjects)));
        }

        foreach (var projectName in GetUnrecognizedProjectNames(topology, solution))
        {
            layers.Add(new ArchitectureGraphLayer(
                GetUnrecognizedProjectPath(projectName),
                "Unrecognized: " + projectName,
                "This project does not match any configured solution module.",
                1,
                0,
                false,
                kind: ArchitectureGraphNodeKind.SolutionModule,
                readOnlyDetails: "No <Module> matcher assigns this loaded project. Add a focused Project matcher to SolutionTopology."));
        }

        var result = layers.ToImmutable();

        return result;
    }

    private static ImmutableArray<ArchitectureGraphRule> CreateSolutionTopologyRules(SolutionTopologyConfig topology)
    {
        var rules = topology.Rules
            .Select(rule => new ArchitectureGraphRule(
                rule.From == "*" ? "*" : GetSolutionModulePath(rule.From),
                rule.To == "*" ? "*" : GetSolutionModulePath(rule.To),
                "SolutionTopology",
                rule.Kind == SolutionModuleReferenceRuleKind.Blocked ? "BlockedModuleReference" : "AllowedModuleReference",
                "project references",
                false,
                rule.From == "*" || rule.To == "*",
                false,
                rule.From,
                rule.To,
                rule.XmlPath,
                ArchitectureConfigurationSourceKind.None,
                rule.XmlLineNumber,
                rule.XmlLinePosition,
                description: rule.Description))
            .ToImmutableArray();

        return rules;
    }

    private static ImmutableArray<ArchitectureGraphDependencyEvidence> CreateSolutionTopologyEvidence(SolutionTopologyConfig topology, SolutionAnalysisResult? solution)
    {
        if (solution is null)
        {
            return ImmutableArray<ArchitectureGraphDependencyEvidence>.Empty;
        }

        var evidence = solution.EffectiveProjectReferences
            .OrderBy(reference => reference.SourceProjectName, StringComparer.Ordinal)
            .ThenBy(reference => reference.TargetProjectName, StringComparer.Ordinal)
            .Select(reference => CreateSolutionTopologyEvidence(topology, reference))
            .ToImmutableArray();

        return evidence;
    }

    private static ArchitectureGraphDependencyEvidence CreateSolutionTopologyEvidence(SolutionTopologyConfig topology, SolutionProjectReference reference)
    {
        var evaluation = SolutionTopologyEvaluator.Evaluate(topology, reference.SourceProjectName, reference.TargetProjectName);
        var sourcePath = GetProjectPath(evaluation.SourceModule, reference.SourceProjectName);
        var targetPath = GetProjectPath(evaluation.TargetModule, reference.TargetProjectName);
        var status = evaluation.IsAllowed ? "Allowed" : "SolutionTopologyViolation";
        var diagnosticId = evaluation.IsAllowed ? null : ArchitectureFindingCodes.SolutionTopologyReferenceViolation;
        var reason = evaluation.IsAllowed
            ? "allowed by the configured solution topology"
            : evaluation.ViolationReason;
        var result = new ArchitectureGraphDependencyEvidence(
            sourcePath,
            targetPath,
            reference.SourceProjectName,
            reference.TargetProjectName,
            "ProjectReference",
            status,
            diagnosticId,
            reason,
            reference.SourceProjectPath,
            1);

        return result;
    }

    private static ImmutableArray<string> GetModuleProjects(SolutionModule module, SolutionAnalysisResult? solution)
    {
        if (solution is null)
        {
            return ImmutableArray<string>.Empty;
        }

        var result = GetSolutionProjectNames(solution)
            .Where(module.Matches)
            .OrderBy(projectName => projectName, StringComparer.Ordinal)
            .ToImmutableArray();

        return result;
    }

    private static ImmutableArray<string> GetUnrecognizedProjectNames(SolutionTopologyConfig topology, SolutionAnalysisResult? solution)
    {
        if (solution is null)
        {
            return ImmutableArray<string>.Empty;
        }

        var result = GetSolutionProjectNames(solution)
            .Where(projectName => SolutionTopologyEvaluator.MatchModule(topology.Modules, projectName) is null)
            .OrderBy(projectName => projectName, StringComparer.Ordinal)
            .ToImmutableArray();

        return result;
    }

    private static ImmutableArray<string> GetSolutionProjectNames(SolutionAnalysisResult solution)
    {
        var names = solution.Projects
            .Select(project => string.IsNullOrWhiteSpace(project.AssemblyName)
                ? Path.GetFileNameWithoutExtension(project.ProjectPath)
                : project.AssemblyName!)
            .Concat(solution.EffectiveProjectReferences.Select(reference => reference.SourceProjectName))
            .Concat(solution.EffectiveProjectReferences.Select(reference => reference.TargetProjectName))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .ToImmutableArray();

        return names;
    }

    private static string CreateModuleDetails(SolutionModule module, ImmutableArray<string> assignedProjects)
    {
        var matchers = string.Join(" OR ", module.Matchers.Select(FormatProjectMatcher));
        var projects = assignedProjects.Length == 0
            ? "No loaded solution projects currently match this module."
            : "Loaded projects: " + string.Join(", ", assignedProjects) + ".";
        var result = "Read-only solution topology module." + Environment.NewLine
                     + "Project matchers: " + matchers + "." + Environment.NewLine
                     + projects;

        return result;
    }

    private static string FormatProjectMatcher(RonSijm.AnaalIJzer.Core.Matchers.ProjectArchitecture.ProjectMatcher matcher)
    {
        var result = string.Join(" AND ", matcher.Conditions.Select(condition => condition.Kind + "=\"" + condition.Value + "\""));

        return result;
    }

    private static string GetProjectPath(string? moduleName, string projectName)
    {
        var result = moduleName is null ? GetUnrecognizedProjectPath(projectName) : GetSolutionModulePath(moduleName);

        return result;
    }

    private static string GetSolutionModulePath(string moduleName)
    {
        var result = SolutionTopologyPathPrefix + moduleName;

        return result;
    }

    private static string GetUnrecognizedProjectPath(string projectName)
    {
        var result = UnrecognizedProjectPathPrefix + projectName;

        return result;
    }
}