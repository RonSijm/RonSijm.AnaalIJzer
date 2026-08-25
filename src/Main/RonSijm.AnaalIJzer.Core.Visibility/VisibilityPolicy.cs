using System.Collections.Immutable;
using RonSijm.AnaalIJzer.SymbolFacts;

namespace RonSijm.AnaalIJzer.Engine.Visibility;

public readonly struct VisibilityPolicy(
	string ownerLayerPath,
	ImmutableHashSet<VisibilityPolicyTarget> targets,
	bool isAllowList,
	ImmutableHashSet<ArchitectureAccessibility> accessibilities,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public string OwnerLayerPath { get; } = ownerLayerPath;
	public ImmutableHashSet<VisibilityPolicyTarget> Targets { get; } = targets;
	public bool IsAllowList { get; } = isAllowList;
	public ImmutableHashSet<ArchitectureAccessibility> Accessibilities { get; } = accessibilities;
	public string? Description { get; } = description;
	public string XmlPath { get; } = xmlPath;
	public int XmlLineNumber { get; } = xmlLineNumber;
	public int XmlLinePosition { get; } = xmlLinePosition;

	public VisibilityPolicyEvaluation? Evaluate(VisibilityPolicyTarget target, ArchitectureAccessibility accessibility)
	{
		if (!Targets.Contains(target))
		{
			return null;
		}

		var listed = Accessibilities.Contains(accessibility);
		var allowed = IsAllowList ? listed : !listed;
		if (allowed)
		{
			return null;
		}

		var configuredValues = string.Join(", ", Accessibilities.OrderBy(item => item).Select(item => item.ToDisplayText()));
		var reason = IsAllowList
			? $"the VisibilityPolicy for {target} in layer '{OwnerLayerPath}' allows only {configuredValues}"
			: $"the VisibilityPolicy for {target} in layer '{OwnerLayerPath}' blocks {configuredValues}";
		var result = new VisibilityPolicyEvaluation(this, target, accessibility, reason);

		return result;
	}
}
