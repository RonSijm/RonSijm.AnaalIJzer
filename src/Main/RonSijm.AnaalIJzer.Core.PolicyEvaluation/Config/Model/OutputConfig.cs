using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.PolicyEvaluation.Config.Model;

/// <summary>
///     Holds the opt-in output settings (violation report and architecture documentation)
///     that were previously inlined on <see cref="AnalyzerConfig" />.
/// </summary>
public readonly struct OutputConfig(
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
