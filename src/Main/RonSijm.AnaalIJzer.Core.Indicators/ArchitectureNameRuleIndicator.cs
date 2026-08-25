using Microsoft.CodeAnalysis.Text;

namespace RonSijm.AnaalIJzer.Indicators;

public sealed class ArchitectureNameRuleIndicator(
	TextSpan span,
	string site,
	string ruleKind,
	string callerTypeName,
	string callerLayerPath,
	string sourceName,
	string targetName,
	string normalizedSourceName,
	string normalizedTargetName,
	string reason)
{
	public TextSpan Span { get; } = span;
	public string Site { get; } = site;
	public string RuleKind { get; } = ruleKind;
	public string CallerTypeName { get; } = callerTypeName;
	public string CallerLayerPath { get; } = callerLayerPath;
	public string SourceName { get; } = sourceName;
	public string TargetName { get; } = targetName;
	public string NormalizedSourceName { get; } = normalizedSourceName;
	public string NormalizedTargetName { get; } = normalizedTargetName;
	public string Reason { get; } = reason;
	public string DiagnosticId => "ARCH008";
}
