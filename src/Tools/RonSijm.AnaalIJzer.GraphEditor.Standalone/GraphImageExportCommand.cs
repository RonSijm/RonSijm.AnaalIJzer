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
	private readonly ArchitectureGraphImageExportMode _mode;
	private readonly string _inputPath;
	private readonly string _outputPath;
	private readonly string _configuration;
	private readonly int _width;
	private readonly int _height;
	private readonly bool _failOnError;

	private GraphImageExportCommand(ArchitectureGraphImageExportMode mode, string inputPath, string outputPath, string configuration, int width, int height, bool failOnError)
	{
		this._mode = mode;
		this._inputPath = inputPath;
		this._outputPath = outputPath;
		this._configuration = configuration;
		this._width = width;
		this._height = height;
		this._failOnError = failOnError;
	}

	public int Execute(ILogger logger, CancellationToken cancellationToken = default)
	{
		var request = new ArchitectureGraphImageExportRequest(_mode, _inputPath, _outputPath, _failOnError);
		var service = new ArchitectureGraphImageExportService();
		var result = service.ExportAsync(request, this, this, cancellationToken).GetAwaiter().GetResult();
		LogResult(result, logger);

		return result.ExitCode;
	}
}
