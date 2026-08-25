using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Config.Parsing;
using RonSijm.AnaalIJzer.Engine.DependencyRules;
using RonSijm.AnaalIJzer;
using RonSijm.AnaalIJzer.Exceptions;
using RonSijm.AnaalIJzer.Findings;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ArchitectureHealthReportGenerator
{
	private static List<ArchitectureFinding> GetConfigurationFindings(AnalyzerConfiguration config)
	{
		var findings = config.ConfigurationIssues
			.Where(issue => issue.Kind == ConfigurationIssueKind.InvalidConfiguration)
			.Select(issue => new ArchitectureFinding(ArchitectureFindingSeverity.Error, ArchitectureFindingCodes.Configuration, issue.Message, FormatConfigLocation(issue)))
			.ToList();
		foreach (var review in config.ExceptionReviews)
		{
			findings.Add(new ArchitectureFinding(ArchitectureFindingSeverity.Warning, ArchitecturalDiagnosticIds.ExceptionReview, review.Message, FormatExceptionLocation(review.XmlPath, review.XmlLineNumber), review.Status.ToString(), review.Status.ToString()));
		}

		if (!config.Engine.HasLayers && findings.Count == 0)
		{
			findings.Add(new ArchitectureFinding(ArchitectureFindingSeverity.Error, ArchitectureFindingCodes.Configuration, "No architectural layers were found.", "Add Architecture.anl or AssemblyMetadata(\"AnaalIJzerSettings\", ...)."));
		}

		foreach (var cycle in DependencyCycleDetector.FindConfiguredCycles(config.LayerNames, config.Graph.DependencyEdges))
		{
			findings.Add(new ArchitectureFinding(
				config.EnforceAcyclic ? ArchitectureFindingSeverity.Error : ArchitectureFindingSeverity.Warning,
				ArchitectureFindingCodes.ConfiguredCycle,
				$"{string.Join(" -> ", cycle)} -> {cycle[0]}",
				config.EnforceAcyclic ? "enforceAcyclic is enabled" : "enforceAcyclic is disabled; the graph currently permits this cycle"));
		}

		return findings;
	}

	private static IReadOnlyList<ProjectConfigurationGroup> GroupByConfiguration(ImmutableArray<ProjectAnalysisResult> projects)
	{
		var groups = projects
			.GroupBy(GetConfigurationKey, StringComparer.OrdinalIgnoreCase)
			.Select(group => new ProjectConfigurationGroup(group.ToImmutableArray()))
			.ToArray();
		var result = (IReadOnlyList<ProjectConfigurationGroup>)groups;

		return result;
	}

	private static string GetConfigurationKey(ProjectAnalysisResult project)
	{
		if (!string.IsNullOrWhiteSpace(project.ConfigInputPath))
		{
			var result = "file:" + Path.GetFullPath(project.ConfigInputPath);

			return result;
		}

		if (!string.IsNullOrWhiteSpace(project.InlineConfigSourcePath))
		{
			var result = "inline:" + Path.GetFullPath(project.InlineConfigSourcePath);

			return result;
		}

		var fallback = "project:" + Path.GetFullPath(project.ProjectPath);

		return fallback;
	}

	private static string FormatConfigLocation(ConfigurationIssue issue)
	{
		var result = issue.LineNumber > 0 ? $"{issue.Path}:{issue.LineNumber}" : issue.Path;

		return result;
	}

	private static string FormatExceptionLocation(string path, int lineNumber)
	{
		var result = lineNumber > 0 ? $"{path}:{lineNumber}" : path;

		return result;
	}

	private sealed record ProjectConfigurationGroup(ImmutableArray<ProjectAnalysisResult> Projects);
}

