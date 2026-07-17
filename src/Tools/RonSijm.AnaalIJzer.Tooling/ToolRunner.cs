using System.Text;
using RonSijm.AnaalIJzer.Model;
using RonSijm.AnaalIJzer.Parsing;
using RonSijm.AnaalIJzer.Documentation;
using RonSijm.AnaalIJzer.Violations;

namespace RonSijm.AnaalIJzer.Tooling;

public sealed class ToolRunner
{
	public async Task<ToolRunResult> ExecuteAsync(ToolRequest request, CancellationToken cancellationToken = default)
	{
		var operation = ToolOperationCatalog.Get(request.Operation);
		ValidateRequest(request, operation);

		var result = request.Operation switch
		{
			ToolOperationKind.GenerateConfig => await GenerateConfigAsync(request, cancellationToken),
			ToolOperationKind.ExportConfig => await ExportConfigAsync(request, cancellationToken),
			ToolOperationKind.Documentation => await GenerateDocumentationAsync(request, cancellationToken),
			ToolOperationKind.Report => await GenerateReportAsync(request, cancellationToken),
			ToolOperationKind.Inspect => await InspectArchitectureAsync(request, cancellationToken),
			ToolOperationKind.MergeConfig => await MergeConfigAsync(request, cancellationToken),
			ToolOperationKind.SplitConfig => await SplitConfigAsync(request, cancellationToken),
			_ => throw new ToolingException($"Unsupported operation: {request.Operation}")
		};

		return result;
	}

	private static async Task<ToolRunResult> GenerateConfigAsync(ToolRequest request, CancellationToken cancellationToken)
	{
		if (request.InputKind == ToolInputKind.Solution)
		{
			return await GenerateSolutionConfigAsync(request, cancellationToken);
		}

		return await GenerateProjectConfigAsync(request, cancellationToken);
	}

	private static async Task<ToolRunResult> GenerateProjectConfigAsync(ToolRequest request, CancellationToken cancellationToken)
	{
		var result = await AnalyzeProjectAsync(request, cancellationToken);
		var outputPath = ResolveOutputPath(request.OutputPath, Path.Combine(result.ProjectDirectory, ArchitecturalConfigParser.ConfigFileName), result.ProjectDirectory);
		var schemaPath = Path.Combine(Path.GetDirectoryName(outputPath)!, "AnaalIJzer.xsd");
		var documentationPath = Path.Combine(Path.GetDirectoryName(outputPath)!, "architecture-documentation.md");
		if (string.Equals(outputPath, schemaPath, StringComparison.OrdinalIgnoreCase))
		{
			throw new ToolingException("The configuration output path may not be AnaalIJzer.xsd.");
		}

		if (File.Exists(outputPath) && !request.Force)
		{
			throw new ToolingException($"Output already exists: {outputPath}. Enable overwrite to replace it.");
		}

		if (request.GenerateDocumentation && File.Exists(documentationPath) && !request.Force)
		{
			throw new ToolingException($"Output already exists: {documentationPath}. Enable overwrite to replace it.");
		}

		var schema = ApplicationConfigurationGenerator.ReadSchema();
		if (File.Exists(schemaPath)
		    && !request.Force
		    && !string.Equals(NormalizeLineEndings(await File.ReadAllTextAsync(schemaPath, cancellationToken)), NormalizeLineEndings(schema), StringComparison.Ordinal))
		{
			throw new ToolingException($"A different schema already exists at {schemaPath}. Enable overwrite to replace it.");
		}

		var configuration = ApplicationConfigurationGenerator.Generate(result.Compilation, Path.GetFileName(schemaPath), request.GenerationOptions, cancellationToken);
		var generatedDiagnostics = await ApplicationConfigurationGenerator.ValidateAsync(result.Compilation, configuration, outputPath, cancellationToken);
		if (generatedDiagnostics.Length > 0)
		{
			var diagnosticSummary = string.Join(Environment.NewLine, generatedDiagnostics.Take(10).Select(diagnostic => diagnostic.ToString()));
			throw new ToolingException($"The inferred configuration did not cover the existing architecture:{Environment.NewLine}{diagnosticSummary}");
		}

		Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
		await File.WriteAllTextAsync(outputPath, configuration, new UTF8Encoding(false), cancellationToken);
		if (!File.Exists(schemaPath) || request.Force)
		{
			await File.WriteAllTextAsync(schemaPath, schema, new UTF8Encoding(false), cancellationToken);
		}

		if (request.GenerateDocumentation)
		{
			var generatedConfig = ApplicationConfigurationGenerator.Parse(result.Compilation, configuration, outputPath, cancellationToken);
			var documentation = ArchitectureDocumentationGenerator.GenerateMarkdown(generatedConfig, result.AssemblyName);
			if (request.IncludeDocumentationInput)
			{
				documentation = ArchitectureDocumentationInputAppender.Append(documentation, configuration, outputPath);
			}
			documentation = ArchitectureCodeEvidenceGenerator.Append(documentation, result.Compilation, generatedConfig, generatedDiagnostics, result.ProjectDirectory, cancellationToken);
			await File.WriteAllTextAsync(documentationPath, documentation, new UTF8Encoding(false), cancellationToken);
		}

		var message = $"Generated configuration for {result.AssemblyName ?? Path.GetFileNameWithoutExtension(result.ProjectPath)} at {outputPath}";
		if (request.GenerateDocumentation)
		{
			message += $"{Environment.NewLine}Generated code-backed documentation at {documentationPath}";
		}
		return new ToolRunResult(outputPath, message);
	}

	private static async Task<ToolRunResult> GenerateSolutionConfigAsync(ToolRequest request, CancellationToken cancellationToken)
	{
		var result = await AnalyzeSolutionAsync(request, cancellationToken);
		var outputPath = ResolveOutputPath(request.OutputPath, Path.Combine(result.SolutionDirectory, ArchitecturalConfigParser.ConfigFileName), result.SolutionDirectory);
		var schemaPath = Path.Combine(Path.GetDirectoryName(outputPath)!, "AnaalIJzer.xsd");
		var documentationPath = Path.Combine(Path.GetDirectoryName(outputPath)!, "architecture-documentation.md");
		if (string.Equals(outputPath, schemaPath, StringComparison.OrdinalIgnoreCase))
		{
			throw new ToolingException("The configuration output path may not be AnaalIJzer.xsd.");
		}

		if (File.Exists(outputPath) && !request.Force)
		{
			throw new ToolingException($"Output already exists: {outputPath}. Enable overwrite to replace it.");
		}

		if (request.GenerateDocumentation && File.Exists(documentationPath) && !request.Force)
		{
			throw new ToolingException($"Output already exists: {documentationPath}. Enable overwrite to replace it.");
		}

		var schema = ApplicationConfigurationGenerator.ReadSchema();
		if (File.Exists(schemaPath)
		    && !request.Force
		    && !string.Equals(NormalizeLineEndings(await File.ReadAllTextAsync(schemaPath, cancellationToken)), NormalizeLineEndings(schema), StringComparison.Ordinal))
		{
			throw new ToolingException($"A different schema already exists at {schemaPath}. Enable overwrite to replace it.");
		}

		var configuration = ApplicationConfigurationGenerator.Generate(result, Path.GetFileName(schemaPath), request.GenerationOptions, cancellationToken);
		var generatedDiagnostics = await ApplicationConfigurationGenerator.ValidateAsync(result, configuration, outputPath, cancellationToken);
		if (generatedDiagnostics.Length > 0)
		{
			var diagnosticSummary = string.Join(Environment.NewLine, generatedDiagnostics.Take(10).Select(diagnostic => diagnostic.ToString()));
			throw new ToolingException($"The inferred configuration did not cover the existing architecture:{Environment.NewLine}{diagnosticSummary}");
		}

		Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
		await File.WriteAllTextAsync(outputPath, configuration, new UTF8Encoding(false), cancellationToken);
		if (!File.Exists(schemaPath) || request.Force)
		{
			await File.WriteAllTextAsync(schemaPath, schema, new UTF8Encoding(false), cancellationToken);
		}

		if (request.GenerateDocumentation)
		{
			var generatedConfig = ApplicationConfigurationGenerator.Parse(result.Projects[0].Compilation, configuration, outputPath, cancellationToken);
			var documentation = ArchitectureDocumentationGenerator.GenerateMarkdown(generatedConfig, result.SolutionName);
			if (request.IncludeDocumentationInput)
			{
				documentation = ArchitectureDocumentationInputAppender.Append(documentation, configuration, outputPath);
			}
			await File.WriteAllTextAsync(documentationPath, documentation, new UTF8Encoding(false), cancellationToken);
		}

		var message = $"Generated configuration for solution {result.SolutionName} at {outputPath}";
		if (request.GenerateDocumentation)
		{
			message += $"{Environment.NewLine}Generated documentation at {documentationPath}";
		}
		return new ToolRunResult(outputPath, message);
	}

	private static async Task<ToolRunResult> ExportConfigAsync(ToolRequest request, CancellationToken cancellationToken)
	{
		var result = await AnalyzeProjectAsync(request, cancellationToken);
		if (string.IsNullOrWhiteSpace(result.InlineConfigXml))
		{
			throw new ToolingException("No AssemblyMetadata(\"AnaalIJzerSettings\", ...) value was found. There is no inline config to export.");
		}

		var outputPath = ResolveOutputPath(request.OutputPath, Path.Combine(result.ProjectDirectory, ArchitecturalConfigParser.ConfigFileName), result.ProjectDirectory);
		await WriteOutputAsync(outputPath, EnsureFinalNewLine(result.InlineConfigXml), request.Force, cancellationToken);
		return Success(outputPath);
	}

	private static async Task<ToolRunResult> GenerateDocumentationAsync(ToolRequest request, CancellationToken cancellationToken)
	{
		if (request.InputKind == ToolInputKind.ConfigurationFile)
		{
			var configPath = Path.GetFullPath(request.InputPaths[0]);
			var result = ConfigurationDocumentationHost.Load(configPath, cancellationToken);
			EnsureConfigHasLayers(result.Config);
			var outputPath = ResolveOutputPath(
				request.OutputPath,
				result.Config.EnableDocumentation ? result.Config.DocumentationPath : Path.Combine(result.ConfigDirectory, "architecture-documentation.md"),
				result.ConfigDirectory);
			var documentation = ArchitectureDocumentationGenerator.GenerateMarkdown(result.Config, result.Title);
			if (request.IncludeDocumentationInput)
			{
				documentation = ArchitectureDocumentationInputAppender.Append(documentation, await File.ReadAllTextAsync(configPath, cancellationToken), configPath);
			}
			await WriteOutputAsync(outputPath, documentation, request.Force, cancellationToken);
			return Success(outputPath);
		}

		var projectResult = await AnalyzeProjectAsync(request, cancellationToken);
		EnsureConfigHasLayers(projectResult.Config);
		var projectOutputPath = ResolveOutputPath(
			request.OutputPath,
			projectResult.Config.EnableDocumentation ? projectResult.Config.DocumentationPath : Path.Combine(projectResult.ProjectDirectory, "architecture-documentation.md"),
			projectResult.ProjectDirectory);
		var projectDocumentation = ArchitectureDocumentationGenerator.GenerateMarkdown(projectResult.Config, projectResult.AssemblyName);
		if (request.IncludeDocumentationInput)
		{
			if (projectResult.ConfigInputXml is null || projectResult.ConfigInputPath is null)
			{
				throw new ToolingException("The project does not expose architecture configuration XML to include.");
			}
			projectDocumentation = ArchitectureDocumentationInputAppender.Append(projectDocumentation, projectResult.ConfigInputXml, projectResult.ConfigInputPath);
		}
		if (request.IncludeCodeEvidence)
		{
			projectDocumentation = ArchitectureCodeEvidenceGenerator.Append(projectDocumentation, projectResult.Compilation, projectResult.Config, projectResult.AnalyzerDiagnostics, projectResult.ProjectDirectory, cancellationToken);
		}
		await WriteOutputAsync(projectOutputPath, projectDocumentation, request.Force, cancellationToken);
		return Success(projectOutputPath);
	}

	private static async Task<ToolRunResult> GenerateReportAsync(ToolRequest request, CancellationToken cancellationToken)
	{
		if (request.InputKind == ToolInputKind.Solution)
		{
			return await GenerateSolutionReportAsync(request, cancellationToken);
		}

		var result = await AnalyzeProjectAsync(request, cancellationToken);
		EnsureConfigHasLayers(result.Config);
		var outputPath = ResolveOutputPath(
			request.OutputPath,
			result.Config.EnableReport ? result.Config.ReportPath : Path.Combine(result.ProjectDirectory, "architectural-violations.md"),
			result.ProjectDirectory);
		var report = ArchitecturalViolationReporter.GenerateMarkdownReport(result.AnalyzerDiagnostics, result.Config, result.AssemblyName);
		await WriteOutputAsync(outputPath, report, request.Force, cancellationToken);
		return Success(outputPath);
	}

	private static async Task<ToolRunResult> GenerateSolutionReportAsync(ToolRequest request, CancellationToken cancellationToken)
	{
		var result = await AnalyzeSolutionAsync(request, cancellationToken);
		var representativeProject = EnsureSolutionHasLayers(result);
		var outputPath = ResolveOutputPath(
			request.OutputPath,
			representativeProject.Config.EnableReport ? representativeProject.Config.ReportPath : Path.Combine(result.SolutionDirectory, "architectural-violations.md"),
			result.SolutionDirectory);
		var report = ArchitecturalViolationReporter.GenerateMarkdownReport(result.AnalyzerDiagnostics, representativeProject.Config, result.SolutionName, "Solution");
		await WriteOutputAsync(outputPath, report, request.Force, cancellationToken);
		return Success(outputPath);
	}

	private static async Task<ToolRunResult> InspectArchitectureAsync(ToolRequest request, CancellationToken cancellationToken)
	{
		ArchitectureHealthReport report;
		string workingDirectory;
		if (request.InputKind == ToolInputKind.ConfigurationFile)
		{
			var configPath = Path.GetFullPath(request.InputPaths[0]);
			var result = ConfigurationDocumentationHost.Load(configPath, cancellationToken);
			report = ArchitectureHealthReportGenerator.Generate(result.Config, Path.GetFileName(configPath));
			workingDirectory = result.ConfigDirectory;
		}
		else if (request.InputKind == ToolInputKind.Solution)
		{
			var result = await AnalyzeSolutionAsync(request, cancellationToken);
			report = ArchitectureHealthReportGenerator.Generate(result, cancellationToken);
			workingDirectory = result.SolutionDirectory;
		}
		else
		{
			var result = await AnalyzeProjectAsync(request, cancellationToken);
			report = ArchitectureHealthReportGenerator.Generate(result, cancellationToken);
			workingDirectory = result.ProjectDirectory;
		}

		var outputPath = ResolveOutputPath(request.OutputPath, Path.Combine(workingDirectory, "architecture-health.md"), workingDirectory);
		if (request.WriteOutput)
		{
			await WriteOutputAsync(outputPath, report.Markdown, request.Force, cancellationToken);
		}

		var message = report.FindingCount == 0
			? "Architecture inspection passed."
			: $"Architecture inspection found {report.FindingCount} issue(s).";
		if (request.WriteOutput)
		{
			message += $" Wrote {outputPath}";
		}

		return new ToolRunResult(outputPath, message, report.FindingCount > 0, report.Markdown);
	}

	private static async Task<ToolRunResult> MergeConfigAsync(ToolRequest request, CancellationToken cancellationToken)
	{
		var inputPaths = request.InputPaths.Select(Path.GetFullPath).ToArray();
		var inputDirectory = Path.GetDirectoryName(inputPaths[0])!;
		var outputPath = ResolveOutputPath(request.OutputPath, Path.Combine(inputDirectory, "Architecture.merged.anl"), inputDirectory);
		await ArchitectureConfigurationFileService.MergeAsync(inputPaths, outputPath, request.Force, cancellationToken);
		return new ToolRunResult(outputPath, $"Merged {inputPaths.Length} configuration file(s) into {outputPath}");
	}

	private static async Task<ToolRunResult> SplitConfigAsync(ToolRequest request, CancellationToken cancellationToken)
	{
		var inputPath = Path.GetFullPath(request.InputPaths[0]);
		var inputDirectory = Path.GetDirectoryName(inputPath)!;
		var outputDirectory = ResolveOutputPath(request.OutputPath, Path.Combine(inputDirectory, "Architecture.Split"), inputDirectory);
		var graphCount = await ArchitectureConfigurationFileService.SplitAsync(inputPath, outputDirectory, request.Force, cancellationToken);
		return new ToolRunResult(outputDirectory, $"Wrote {graphCount} dependency graphs and a manifest to {outputDirectory}");
	}

	private static async Task<ProjectAnalysisResult> AnalyzeProjectAsync(ToolRequest request, CancellationToken cancellationToken)
	{
		var projectPath = Path.GetFullPath(request.InputPaths[0]);
		if (!File.Exists(projectPath))
		{
			throw new ToolingException($"Project file not found: {projectPath}");
		}

		using var host = new ProjectAnalysisHost(request.Configuration);
		var result = await host.AnalyzeAsync(projectPath, cancellationToken);
		if (result.WorkspaceFailures.Length > 0)
		{
			throw new ToolingException("Workspace failed to load the project:" + Environment.NewLine + string.Join(Environment.NewLine, result.WorkspaceFailures));
		}

		if (result.CompilerErrors.Length > 0)
		{
			throw new ToolingException("Project has compiler errors:" + Environment.NewLine + string.Join(Environment.NewLine, result.CompilerErrors));
		}

		return result;
	}

	private static async Task<SolutionAnalysisResult> AnalyzeSolutionAsync(ToolRequest request, CancellationToken cancellationToken)
	{
		var solutionPath = Path.GetFullPath(request.InputPaths[0]);
		if (!File.Exists(solutionPath))
		{
			throw new ToolingException($"Solution file not found: {solutionPath}");
		}

		using var host = new ProjectAnalysisHost(request.Configuration);
		var result = await host.AnalyzeSolutionAsync(solutionPath, cancellationToken);
		if (result.Projects.Length == 0)
		{
			throw new ToolingException($"No C# projects were found in solution: {solutionPath}");
		}

		if (result.WorkspaceFailures.Length > 0)
		{
			throw new ToolingException("Workspace failed to load the solution:" + Environment.NewLine + string.Join(Environment.NewLine, result.WorkspaceFailures));
		}

		if (result.CompilerErrors.Length > 0)
		{
			throw new ToolingException("Solution has compiler errors:" + Environment.NewLine + string.Join(Environment.NewLine, result.CompilerErrors));
		}

		return result;
	}

	private static void ValidateRequest(ToolRequest request, ToolOperationDefinition operation)
	{
		if (request.InputKind is null || request.InputPaths.Count == 0 || request.InputPaths.Any(string.IsNullOrWhiteSpace))
		{
			throw new ToolingException(operation.SupportedInputs.Count > 1
				? "Select a project or an architecture settings file."
				: $"Select {ToolInputCatalog.Get(operation.DefaultInput).DisplayName.ToLowerInvariant()} input.");
		}

		if (!operation.Supports(request.InputKind.Value))
		{
			var input = ToolInputCatalog.Get(request.InputKind.Value);
			throw new ToolingException($"{operation.DisplayName} does not support {input.DisplayName.ToLowerInvariant()} input.");
		}

		if (request.InputPaths.Count < operation.MinimumInputCount)
		{
			throw new ToolingException($"{operation.DisplayName} requires at least {operation.MinimumInputCount} input files.");
		}

		if (operation.MaximumInputCount is { } maximumInputCount && request.InputPaths.Count > maximumInputCount)
		{
			throw new ToolingException($"{operation.DisplayName} accepts at most {maximumInputCount} input file(s).");
		}

		if (request.GenerationOptions.MinimumConfidence is <= 0 or > 1)
		{
			throw new ToolingException("Minimum confidence must be greater than 0 and no greater than 1.");
		}

		if (request.GenerationOptions.MinimumSupport < 1)
		{
			throw new ToolingException("Minimum support must be at least 1.");
		}

		if (request.IncludeCodeEvidence && (request.Operation != ToolOperationKind.Documentation || request.InputKind != ToolInputKind.Project))
		{
			throw new ToolingException("Code evidence is available only when generating documentation from a project.");
		}

		if (request.GenerateDocumentation && request.Operation != ToolOperationKind.GenerateConfig)
		{
			throw new ToolingException("Automatic documentation is available only when generating a configuration.");
		}

		if (request.IncludeDocumentationInput
		    && request.Operation != ToolOperationKind.Documentation
		    && !(request.Operation == ToolOperationKind.GenerateConfig && request.GenerateDocumentation))
		{
			throw new ToolingException("Input XML can be included only in generated documentation.");
		}

		if (!request.WriteOutput && request.Operation != ToolOperationKind.Inspect)
		{
			throw new ToolingException("Preview without writing output is available only for architecture inspection.");
		}
	}

	private static void EnsureConfigHasLayers(AnalyzerConfig config)
	{
		if (!config.HasLayers)
		{
			throw new ToolingException("No ArchitecturalLevels config was found. Add Architecture.anl or AssemblyMetadata(\"AnaalIJzerSettings\", ...).");
		}
	}

	private static ProjectAnalysisResult EnsureSolutionHasLayers(SolutionAnalysisResult result)
	{
		var representativeProject = result.FirstConfiguredProject;
		if (representativeProject is null)
		{
			throw new ToolingException("No ArchitecturalLevels config was found in the solution. Add Architecture.anl or AssemblyMetadata(\"AnaalIJzerSettings\", ...) to at least one project.");
		}

		return representativeProject;
	}

	private static async Task WriteOutputAsync(string outputPath, string content, bool force, CancellationToken cancellationToken)
	{
		if (File.Exists(outputPath) && !force)
		{
			throw new ToolingException($"Output already exists: {outputPath}. Enable overwrite to replace it.");
		}

		Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
		await File.WriteAllTextAsync(outputPath, content, new UTF8Encoding(false), cancellationToken);
	}

	private static string ResolveOutputPath(string? requestedPath, string fallbackPath, string workingDirectory)
	{
		if (!string.IsNullOrWhiteSpace(requestedPath))
		{
			return Path.GetFullPath(requestedPath);
		}

		return Path.GetFullPath(Path.IsPathRooted(fallbackPath) ? fallbackPath : Path.Combine(workingDirectory, fallbackPath));
	}

	private static string EnsureFinalNewLine(string text)
	{
		var result = text.EndsWith(Environment.NewLine, StringComparison.Ordinal) ? text : text + Environment.NewLine;

		return result;
	}

	private static string NormalizeLineEndings(string text)
	{
		var result = text.Replace("\r\n", "\n").Replace('\r', '\n');

		return result;
	}

	private static ToolRunResult Success(string outputPath)
	{
		var result = new ToolRunResult(outputPath, $"Wrote {outputPath}");

		return result;
	}
}
