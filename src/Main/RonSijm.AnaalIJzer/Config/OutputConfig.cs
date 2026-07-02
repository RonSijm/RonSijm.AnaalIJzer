namespace RonSijm.AnaalIJzer.Config;

/// <summary>
///     Holds the opt-in output settings (violation report and architecture documentation)
///     that were previously inlined on <see cref="AnalyzerConfig" />.
/// </summary>
internal readonly struct OutputConfig(
	bool enableReport,
	string reportPath,
	bool enableDocumentation,
	string documentationPath)
{
	public bool EnableReport { get; } = enableReport;
	public string ReportPath { get; } = reportPath;
	public bool EnableDocumentation { get; } = enableDocumentation;
	public string DocumentationPath { get; } = documentationPath;
}
