namespace RonSijm.AnaalIJzer.Core.NameRules;

public readonly struct NameRuleViolation(NameRuleKind ruleKind, string sourceName, string targetName, string normalizedSourceName, string normalizedTargetName, string site, string layerName, string reason, string xmlPath, int xmlLineNumber, int xmlLinePosition)
{
	public NameRuleKind RuleKind { get; } = ruleKind;
	public string SourceName { get; } = sourceName;
	public string TargetName { get; } = targetName;
	public string NormalizedSourceName { get; } = normalizedSourceName;
	public string NormalizedTargetName { get; } = normalizedTargetName;
	public string Site { get; } = site;
	public string LayerName { get; } = layerName;
	public string Reason { get; } = reason;
	public string XmlPath { get; } = xmlPath;
	public int XmlLineNumber { get; } = xmlLineNumber;
	public int XmlLinePosition { get; } = xmlLinePosition;
}
