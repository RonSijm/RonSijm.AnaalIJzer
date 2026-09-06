using RonSijm.AnaalIJzer.Outputs.Inspection;

namespace RonSijm.AnaalIJzer.Application;

internal static class ApplicationInspectionOperations
{
	public static async Task<ApplicationRunResult> InspectArchitectureAsync(ApplicationRequest request, ApplicationWorkspaceAnalysisService workspace, CancellationToken cancellationToken)
	{
		ArchitectureHealthReport report;
		string workingDirectory;
		if (request.InputKind == ApplicationInputKind.ConfigurationFile)
		{
			var configPath = Path.GetFullPath(request.InputPaths[0]);
			var result = ConfigurationDocumentationHost.Load(configPath, cancellationToken);
			report = ArchitectureHealthReportGenerator.Generate(result.Config, Path.GetFileName(configPath));
			workingDirectory = result.ConfigDirectory;
		}
		else if (request.InputKind == ApplicationInputKind.Solution)
		{
			var result = await workspace.AnalyzeSolutionAsync(request, cancellationToken);
			report = ArchitectureHealthReportGenerator.Generate(result, cancellationToken, request.EnforceSolutionTopology);
			workingDirectory = result.SolutionDirectory;
		}
		else
		{
			var result = await workspace.AnalyzeProjectAsync(request, cancellationToken);
			report = ArchitectureHealthReportGenerator.Generate(result, cancellationToken);
			workingDirectory = result.ProjectDirectory;
		}

		var outputPath = ApplicationOutputPathService.ResolveOutputPath(request.OutputPath, Path.Combine(workingDirectory, "architecture-health.md"), workingDirectory);
		if (request.WriteOutput)
		{
			var outputContent = string.Equals(Path.GetExtension(outputPath), ".json", StringComparison.OrdinalIgnoreCase)
				? ArchitectureHealthReportJsonSerializer.Serialize(report, request.InputKind, request.InputPaths)
				: report.Markdown;
			await ApplicationOutputPathService.WriteOutputAsync(outputPath, outputContent, request.Force, cancellationToken);
		}

		var message = report.FindingCount == 0
			? "Architecture inspection passed."
			: $"Architecture inspection found {report.FindingCount} issue(s).";
		if (request.WriteOutput)
		{
			message += $" Wrote {outputPath}";
		}

		var toolRunResult = new ApplicationRunResult(outputPath, message, report.FindingCount > 0, report.Markdown, report.Findings);

		return toolRunResult;
	}
}

