using System.Text;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Outputs.Documentation;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ApplicationConfigurationOperations
{
	private static async Task<ApplicationRunResult> GenerateSolutionConfigAsync(ApplicationRequest request, ApplicationWorkspaceAnalysisService workspace, CancellationToken cancellationToken)
	{
		var result = await workspace.AnalyzeSolutionAsync(request, cancellationToken);
		var outputPath = ApplicationOutputPathService.ResolveOutputPath(request.OutputPath, Path.Combine(result.SolutionDirectory, ArchitectureConfigurationDocumentLoader.ConfigFileName), result.SolutionDirectory);
		var schemaPath = Path.Combine(Path.GetDirectoryName(outputPath)!, "AnaalIJzer.xsd");
		var documentationPath = Path.Combine(Path.GetDirectoryName(outputPath)!, "architecture-documentation.md");
		EnsureSchemaOutputDoesNotCollide(outputPath, schemaPath);
		EnsureOutputIsWritable(outputPath, request.Force);
		EnsureDocumentationOutputIsWritable(request.GenerateDocumentation, documentationPath, request.Force);

		var schema = ApplicationConfigurationGenerator.ReadSchema();
		await EnsureSchemaFileCanBeReplacedAsync(schemaPath, schema, request.Force, cancellationToken);

		var configuration = ApplicationConfigurationGenerator.Generate(result, Path.GetFileName(schemaPath), request.GenerationOptions, cancellationToken);
		var generatedDiagnostics = await ApplicationConfigurationGenerator.ValidateAsync(result, configuration, outputPath, cancellationToken);
		EnsureGeneratedConfigurationIsValid(generatedDiagnostics);

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

		var toolRunResult = new ApplicationRunResult(outputPath, message);

		return toolRunResult;
	}
}
