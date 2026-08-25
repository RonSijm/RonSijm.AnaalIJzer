using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.Outputs.GraphExports;

namespace RonSijm.AnaalIJzer.GraphEditor.Standalone;

internal sealed partial class GraphImageExportCommand : IArchitectureGraphSnapshotLoader, IArchitectureGraphImageRenderer
{
	private const int DefaultWidth = 1600;
	private const int DefaultHeight = 1000;
	private const double ExportMinimumWidth = 680;
	private const double ExportMinimumHeight = 320;
	private const double ExportNodeWidth = 210;
	private const double ExportNodeHeight = 112;
	private const double ExportPadding = 96;
	private const double ExportGroupChromeHeight = 96;
	private readonly ArchitectureGraphImageExportMode mode;
	private readonly string inputPath;
	private readonly string outputPath;
	private readonly string configuration;
	private readonly int width;
	private readonly int height;
	private readonly bool failOnError;

	private GraphImageExportCommand(ArchitectureGraphImageExportMode mode, string inputPath, string outputPath, string configuration, int width, int height, bool failOnError)
	{
		this.mode = mode;
		this.inputPath = inputPath;
		this.outputPath = outputPath;
		this.configuration = configuration;
		this.width = width;
		this.height = height;
		this.failOnError = failOnError;
	}

	public int Execute(ILogger logger, CancellationToken cancellationToken = default)
	{
		var request = new ArchitectureGraphImageExportRequest(mode, inputPath, outputPath, failOnError);
		var service = new ArchitectureGraphImageExportService();
		var result = service.ExportAsync(request, this, this, cancellationToken).GetAwaiter().GetResult();
		LogResult(result, logger);

		return result.ExitCode;
	}
}
