using RonSijm.AnaalIJzer.Findings;
using RonSijm.AnaalIJzer.Outputs.Inspection;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

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
		var projectFindings = InspectProject(result, cancellationToken);
		findings.AddRange(projectFindings);
		var report = ArchitectureHealthReportBuilder.Build(result.AssemblyName ?? Path.GetFileNameWithoutExtension(result.ProjectPath), findings, result.ProjectPath);

		return report;
	}

	public static ArchitectureHealthReport Generate(SolutionAnalysisResult result, CancellationToken cancellationToken)
	{
		var findings = new List<ArchitectureFinding>();
		foreach (var group in GroupByConfiguration(result.Projects))
		{
			if (group.Projects.Length > 1)
			{
				findings.AddRange(GetConfigurationFindings(group.Projects[0].Config));
				findings.AddRange(InspectProjects(group.Projects, cancellationToken));
				continue;
			}

			var project = group.Projects[0];
			var projectName = project.AssemblyName ?? Path.GetFileNameWithoutExtension(project.ProjectPath);
			var projectFindings = GetConfigurationFindings(project.Config);
			projectFindings.AddRange(InspectProject(project, cancellationToken));
			findings.AddRange(projectFindings.Select(finding => AddProjectContext(projectName, finding)));
		}

		var report = ArchitectureHealthReportBuilder.Build(result.SolutionName, findings, result.SolutionPath, "Solution");

		return report;
	}
}

