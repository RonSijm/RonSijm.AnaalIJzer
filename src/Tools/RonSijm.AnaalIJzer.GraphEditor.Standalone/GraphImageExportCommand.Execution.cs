using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.GraphModel.Model;
using RonSijm.AnaalIJzer.GraphWorkspace;
using RonSijm.AnaalIJzer.Outputs.GraphExports;

namespace RonSijm.AnaalIJzer.GraphEditor.Standalone;

internal sealed partial class GraphImageExportCommand
{
	public Task<ArchitectureGraphSnapshot> LoadAsync(string inputPath, CancellationToken cancellationToken)
	{
		var loader = new ArchitectureGraphWorkspaceSnapshotLoader(_configuration);
		var result = loader.LoadAsync(inputPath, cancellationToken);

		return result;
	}

	private void LogResult(ArchitectureGraphImageExportResult result, ILogger logger)
	{
		foreach (var file in result.Files)
		{
			if (file.Succeeded)
			{
				Console.WriteLine("Wrote " + file.OutputPath);
				logger.LogInformation("Exported architecture graph image. Input: {Input}. Output: {Output}.", file.InputPath, file.OutputPath);
				continue;
			}

			if (file.PlaceholderWritten)
			{
				Console.WriteLine("Wrote placeholder " + file.OutputPath + " (" + file.Message + ")");
				logger.LogWarning("Failed to export example graph image for {Input}. Wrote placeholder to {Output}. Reason: {Reason}", file.InputPath, file.OutputPath, file.Message);
			}
		}

		if (_mode == ArchitectureGraphImageExportMode.Examples)
		{
			Console.WriteLine("Exported " + result.SuccessCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " example graph image(s) to " + _outputPath + ".");
			if (result.PlaceholderCount > 0)
			{
				Console.WriteLine("Created " + result.PlaceholderCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " placeholder image(s) for examples that could not render a graph.");
			}
		}
	}
}
