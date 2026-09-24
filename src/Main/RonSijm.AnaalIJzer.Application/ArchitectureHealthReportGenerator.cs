using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Application.OperationContracts;
using RonSijm.AnaalIJzer.Outputs.Inspection;
using RonSijm.AnaalIJzer.Workspace.Analysis;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ArchitectureHealthReportGenerator
{
    public static ArchitectureHealthReport Generate(AnalyzerConfiguration config, string? title)
    {
        var findings = GetConfigurationFindings(config);
        var result = ArchitectureHealthReportBuilder.Build(title, findings, null);

        return result;
    }

    public static ArchitectureHealthReport Generate(ProjectAnalysisResult result, CancellationToken cancellationToken)
    {
        var findings = GetConfigurationFindings(result.Config);
        findings.AddRange(ProjectArchitectureInspectionService.GetAssemblyReferencePolicyFindings([result]));
        findings.AddRange(OperationContractInspectionService.GetFindings([result], cancellationToken));
        if (result.Config.Engine.HasLayers)
        {
            var projectFindings = InspectProject(result, cancellationToken);
            findings.AddRange(projectFindings);
        }
        var report = ArchitectureHealthReportBuilder.Build(result.AssemblyName ?? Path.GetFileNameWithoutExtension(result.ProjectPath), findings, result.ProjectPath);

        return report;
    }

    public static ArchitectureHealthReport Generate(SolutionAnalysisResult result, CancellationToken cancellationToken, bool enforceSolutionTopology = false)
    {
        var findings = new List<ArchitectureFinding>();
        findings.AddRange(OperationContractInspectionService.GetFindings(result.Projects, cancellationToken));
        foreach (var group in GroupByConfiguration(result.Projects))
        {
            findings.AddRange(ProjectArchitectureInspectionService.GetAssemblyReferencePolicyFindings(group.Projects));
            if (group.Projects.Length > 1)
            {
                findings.AddRange(GetConfigurationFindings(group.Projects[0].Config));
                if (group.Projects[0].Config.Engine.HasLayers)
                {
                    findings.AddRange(InspectProjects(group.Projects, cancellationToken));
                }

                continue;
            }

            var project = group.Projects[0];
            var projectName = project.AssemblyName ?? Path.GetFileNameWithoutExtension(project.ProjectPath);
            var projectFindings = GetConfigurationFindings(project.Config);
            if (project.Config.Engine.HasLayers)
            {
                projectFindings.AddRange(InspectProject(project, cancellationToken));
            }

            findings.AddRange(projectFindings.Select(finding => AddProjectContext(projectName, finding)));
        }

        if (enforceSolutionTopology)
        {
            AppendSolutionTopologyFindings(result, findings);
        }

        var report = ArchitectureHealthReportBuilder.Build(result.SolutionName, findings, result.SolutionPath, "Solution");

        return report;
    }
}