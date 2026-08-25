using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.GraphModel.Building;

public sealed class ArchitectureGraphRuleInput(
	string from,
	string to,
	string scopePath,
	string kind,
	string siteText,
	bool appliesToDescendants,
	bool isWildcard,
	bool isActive,
	string configuredFrom = "",
	string configuredTo = "",
	string sourcePath = "",
	ArchitectureConfigurationSourceKind sourceKind = ArchitectureConfigurationSourceKind.None,
	int xmlLineNumber = 0,
	int xmlLinePosition = 0,
	ImmutableArray<string> allowedSites = default,
	ImmutableArray<string> blockedSites = default,
	string? description = null)
{
	public string From { get; } = from;

	public string To { get; } = to;

	public string ScopePath { get; } = scopePath;

	public string Kind { get; } = kind;

	public string SiteText { get; } = siteText;

	public bool AppliesToDescendants { get; } = appliesToDescendants;

	public bool IsWildcard { get; } = isWildcard;

	public bool IsActive { get; } = isActive;

	public string ConfiguredFrom { get; } = configuredFrom;

	public string ConfiguredTo { get; } = configuredTo;

	public string SourcePath { get; } = sourcePath;

	public ArchitectureConfigurationSourceKind SourceKind { get; } = sourceKind;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public ImmutableArray<string> AllowedSites { get; } = allowedSites.IsDefault ? ImmutableArray<string>.Empty : allowedSites;

	public ImmutableArray<string> BlockedSites { get; } = blockedSites.IsDefault ? ImmutableArray<string>.Empty : blockedSites;

	public string? Description { get; } = description;
}
