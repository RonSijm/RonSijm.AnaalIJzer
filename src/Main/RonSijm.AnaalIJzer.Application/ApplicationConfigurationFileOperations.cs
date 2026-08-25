using RonSijm.AnaalIJzer.Outputs;
using RonSijm.AnaalIJzer.Outputs.Configuration;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;

namespace RonSijm.AnaalIJzer.Application;

internal static class ApplicationConfigurationFileOperations
{
	public static async Task<ApplicationRunResult> MergeConfigAsync(ApplicationRequest request, CancellationToken cancellationToken)
	{
		var inputPaths = request.InputPaths.Select(Path.GetFullPath).ToArray();
		var inputDirectory = Path.GetDirectoryName(inputPaths[0])!;
		var outputPath = ApplicationOutputPathService.ResolveOutputPath(request.OutputPath, Path.Combine(inputDirectory, "Architecture.merged.anl"), inputDirectory);
		await RunFileOperationAsync(() => ArchitectureConfigurationFileService.MergeAsync(inputPaths, outputPath, request.Force, cancellationToken));
		var toolRunResult = new ApplicationRunResult(outputPath, $"Merged {inputPaths.Length} configuration file(s) into {outputPath}");

		return toolRunResult;
	}

	public static async Task<ApplicationRunResult> SplitConfigAsync(ApplicationRequest request, CancellationToken cancellationToken)
	{
		var inputPath = Path.GetFullPath(request.InputPaths[0]);
		var inputDirectory = Path.GetDirectoryName(inputPath)!;
		var outputDirectory = ApplicationOutputPathService.ResolveOutputPath(request.OutputPath, Path.Combine(inputDirectory, "Architecture.Split"), inputDirectory);
		var graphCount = await RunFileOperationAsync(() => ArchitectureConfigurationFileService.SplitAsync(inputPath, outputDirectory, request.Force, cancellationToken));
		var toolRunResult = new ApplicationRunResult(outputDirectory, $"Wrote {graphCount} dependency graphs and a manifest to {outputDirectory}");

		return toolRunResult;
	}

	public static async Task<ApplicationRunResult> FormatConfigAsync(ApplicationRequest request, CancellationToken cancellationToken)
	{
		var inputPath = Path.GetFullPath(request.InputPaths[0]);
		var inputDirectory = Path.GetDirectoryName(inputPath)!;
		var outputPath = ApplicationOutputPathService.ResolveOutputPath(request.OutputPath, inputPath, inputDirectory);
		await RunFileOperationAsync(() => ArchitectureConfigurationFileService.FormatAsync(inputPath, outputPath, request.Force, cancellationToken));
		var toolRunResult = new ApplicationRunResult(outputPath, $"Formatted architecture settings at {outputPath}");

		return toolRunResult;
	}

	public static async Task<ApplicationRunResult> ExplainConfigAsync(ApplicationRequest request, CancellationToken cancellationToken)
	{
		var inputPath = Path.GetFullPath(request.InputPaths[0]);
		var inputDirectory = Path.GetDirectoryName(inputPath)!;
		var outputPath = ApplicationOutputPathService.ResolveOutputPath(request.OutputPath, Path.Combine(inputDirectory, "architecture-explanation.md"), inputDirectory);
		string explanation;
		try
		{
			explanation = ArchitectureConfigurationExplainer.GenerateMarkdown(inputPath);
		}
		catch (OutputGenerationException ex)
		{
			throw new ApplicationOperationException(ex.Message);
		}

		await ApplicationOutputPathService.WriteOutputAsync(outputPath, explanation, request.Force, cancellationToken);
		var toolRunResult = new ApplicationRunResult(outputPath, $"Wrote architecture explanation to {outputPath}", Content: explanation);

		return toolRunResult;
	}

	private static async Task RunFileOperationAsync(Func<Task> operation)
	{
		try
		{
			await operation();
		}
		catch (ArchitectureConfigurationFileOperationException ex)
		{
			throw new ApplicationOperationException(ex.Message);
		}
	}

	private static async Task<T> RunFileOperationAsync<T>(Func<Task<T>> operation)
	{
		try
		{
			var result = await operation();

			return result;
		}
		catch (ArchitectureConfigurationFileOperationException ex)
		{
			throw new ApplicationOperationException(ex.Message);
		}
	}
}

