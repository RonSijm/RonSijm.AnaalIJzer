using RonSijm.AnaalIJzer.Config.Parsing;
using RonSijm.AnaalIJzer.Documentation;
using RonSijm.AnaalIJzer.Violations;

namespace RonSijm.AnaalIJzer.Application;

internal static class ApplicationDocumentationOperations
{
	public static async Task<ApplicationRunResult> GenerateDocumentationAsync(ApplicationRequest request, ApplicationWorkspaceAnalysisService workspace, CancellationToken cancellationToken)
	{
		if (request.InputKind == ApplicationInputKind.ConfigurationFile)
		{
			var configPath = Path.GetFullPath(request.InputPaths[0]);
			var result = ConfigurationDocumentationHost.Load(configPath, cancellationToken);
			workspace.EnsureConfigHasRules(result.Config);
			var outputPath = ApplicationOutputPathService.ResolveOutputPath(
				request.OutputPath,
				result.Config.EnableDocumentation ? result.Config.DocumentationPath : Path.Combine(result.ConfigDirectory, "architecture-documentation.md"),
				result.ConfigDirectory);
			var documentation = ArchitectureDocumentationGenerator.GenerateMarkdown(result.Config, result.Title);
			if (request.IncludeDocumentationInput)
			{
				documentation = ArchitectureDocumentationInputAppender.Append(documentation, await File.ReadAllTextAsync(configPath, cancellationToken), configPath);
			}
			await ApplicationOutputPathService.WriteOutputAsync(outputPath, documentation, request.Force, cancellationToken);
			var configRunResult = new ApplicationRunResult(outputPath, $"Wrote {outputPath}");

			return configRunResult;
		}

		var projectResult = await workspace.AnalyzeProjectAsync(request, cancellationToken);
		workspace.EnsureConfigHasRules(projectResult.Config);
		var projectOutputPath = ApplicationOutputPathService.ResolveOutputPath(
			request.OutputPath,
			projectResult.Config.EnableDocumentation ? projectResult.Config.DocumentationPath : Path.Combine(projectResult.ProjectDirectory, "architecture-documentation.md"),
			projectResult.ProjectDirectory);
		var projectDocumentation = ArchitectureDocumentationGenerator.GenerateMarkdown(projectResult.Config, projectResult.AssemblyName);
		if (request.IncludeDocumentationInput)
		{
			if (projectResult.ConfigInputXml is null || projectResult.ConfigInputPath is null)
			{
				throw new ApplicationOperationException("The project does not expose architecture configuration XML to include.");
			}
			projectDocumentation = ArchitectureDocumentationInputAppender.Append(projectDocumentation, projectResult.ConfigInputXml, projectResult.ConfigInputPath);
		}
		if (request.IncludeCodeEvidence)
		{
			projectDocumentation = ArchitectureCodeEvidenceGenerator.Append(projectDocumentation, projectResult.Compilation, projectResult.Config, projectResult.AnalyzerDiagnostics, projectResult.ProjectDirectory, cancellationToken);
		}
		await ApplicationOutputPathService.WriteOutputAsync(projectOutputPath, projectDocumentation, request.Force, cancellationToken);
		var projectRunResult = new ApplicationRunResult(projectOutputPath, $"Wrote {projectOutputPath}");

		return projectRunResult;
	}

	public static async Task<ApplicationRunResult> GenerateReportAsync(ApplicationRequest request, ApplicationWorkspaceAnalysisService workspace, CancellationToken cancellationToken)
	{
		if (request.InputKind == ApplicationInputKind.Solution)
		{
			var solutionResult = await GenerateSolutionReportAsync(request, workspace, cancellationToken);

			return solutionResult;
		}

		var result = await workspace.AnalyzeProjectAsync(request, cancellationToken);
		workspace.EnsureConfigHasRules(result.Config);
		var outputPath = ApplicationOutputPathService.ResolveOutputPath(
			request.OutputPath,
			result.Config.EnableReport ? result.Config.ReportPath : Path.Combine(result.ProjectDirectory, "architectural-violations.md"),
			result.ProjectDirectory);
		var report = ArchitecturalViolationReporter.GenerateMarkdownReport(result.AnalyzerDiagnostics, result.Config, result.AssemblyName);
		await ApplicationOutputPathService.WriteOutputAsync(outputPath, report, request.Force, cancellationToken);
		var toolRunResult = new ApplicationRunResult(outputPath, $"Wrote {outputPath}");

		return toolRunResult;
	}

	private static async Task<ApplicationRunResult> GenerateSolutionReportAsync(ApplicationRequest request, ApplicationWorkspaceAnalysisService workspace, CancellationToken cancellationToken)
	{
		var result = await workspace.AnalyzeSolutionAsync(request, cancellationToken);
		var representativeProject = workspace.EnsureSolutionHasLayers(result);
		var outputPath = ApplicationOutputPathService.ResolveOutputPath(
			request.OutputPath,
			representativeProject.Config.EnableReport ? representativeProject.Config.ReportPath : Path.Combine(result.SolutionDirectory, "architectural-violations.md"),
			result.SolutionDirectory);
		var report = ArchitecturalViolationReporter.GenerateMarkdownReport(result.AnalyzerDiagnostics, representativeProject.Config, result.SolutionName, "Solution");
		await ApplicationOutputPathService.WriteOutputAsync(outputPath, report, request.Force, cancellationToken);
		var toolRunResult = new ApplicationRunResult(outputPath, $"Wrote {outputPath}");

		return toolRunResult;
	}
}

