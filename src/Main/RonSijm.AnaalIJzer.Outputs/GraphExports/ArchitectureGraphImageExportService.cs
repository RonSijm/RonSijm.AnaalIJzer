using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Outputs.GraphExports;

public sealed class ArchitectureGraphImageExportService
{
	public async Task<ArchitectureGraphImageExportResult> ExportAsync(
		ArchitectureGraphImageExportRequest request,
		IArchitectureGraphSnapshotLoader loader,
		IArchitectureGraphImageRenderer renderer,
		CancellationToken cancellationToken = default)
	{
		if (request.Mode == ArchitectureGraphImageExportMode.Single)
		{
			var singleResult = await ExportSingleAsync(request, loader, renderer, cancellationToken);

			return singleResult;
		}

		var examplesResult = await ExportExamplesAsync(request, loader, renderer, cancellationToken);

		return examplesResult;
	}

	private static async Task<ArchitectureGraphImageExportResult> ExportSingleAsync(
		ArchitectureGraphImageExportRequest request,
		IArchitectureGraphSnapshotLoader loader,
		IArchitectureGraphImageRenderer renderer,
		CancellationToken cancellationToken)
	{
		var snapshot = await loader.LoadAsync(request.InputPath, cancellationToken);
		EnsureOutputDirectory(request.OutputPath);
		renderer.ExportGraph(snapshot, request.OutputPath);

		var file = new ArchitectureGraphImageExportFileResult(request.InputPath, request.OutputPath, succeeded: true, placeholderWritten: false, message: null);
		var result = new ArchitectureGraphImageExportResult(
			exitCode: 0,
			successCount: 1,
			placeholderCount: 0,
            [file]);

		return result;
	}

	private static async Task<ArchitectureGraphImageExportResult> ExportExamplesAsync(
		ArchitectureGraphImageExportRequest request,
		IArchitectureGraphSnapshotLoader loader,
		IArchitectureGraphImageRenderer renderer,
		CancellationToken cancellationToken)
	{
		var fullExamplesRoot = Path.GetFullPath(request.InputPath);
		if (!Directory.Exists(fullExamplesRoot))
		{
			throw new DirectoryNotFoundException("Examples directory was not found: " + fullExamplesRoot);
		}

		Directory.CreateDirectory(request.OutputPath);
		var projectPaths = Directory
			.EnumerateFiles(fullExamplesRoot, "*.csproj", SearchOption.AllDirectories)
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.ToArray();
		var files = ImmutableArray.CreateBuilder<ArchitectureGraphImageExportFileResult>(projectPaths.Length);
		var successCount = 0;
		var placeholderCount = 0;
		foreach (var projectPath in projectPaths)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var outputPath = CreateExampleOutputPath(projectPath, request.OutputPath);

			try
			{
				var snapshot = await loader.LoadAsync(projectPath, cancellationToken);
				EnsureOutputDirectory(outputPath);
				renderer.ExportGraph(snapshot, outputPath);
				files.Add(new ArchitectureGraphImageExportFileResult(projectPath, outputPath, succeeded: true, placeholderWritten: false, message: null));
				successCount++;
			}
			catch (Exception exception)
			{
				EnsureOutputDirectory(outputPath);
				renderer.ExportPlaceholder(outputPath, Path.GetFileNameWithoutExtension(projectPath), exception.Message);
				files.Add(new ArchitectureGraphImageExportFileResult(projectPath, outputPath, succeeded: false, placeholderWritten: true, message: exception.Message));
				placeholderCount++;
			}
		}

		var exitCode = placeholderCount > 0 && request.FailOnError ? 1 : 0;
		var result = new ArchitectureGraphImageExportResult(exitCode, successCount, placeholderCount, files.ToImmutable());

		return result;
	}

	private static string CreateExampleOutputPath(string projectPath, string outputDirectory)
	{
		var fileName = Path.GetFileNameWithoutExtension(projectPath) + "-Graph.png";
		var result = Path.Combine(outputDirectory, fileName);

		return result;
	}

	private static void EnsureOutputDirectory(string outputPath)
	{
		var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}
	}
}
