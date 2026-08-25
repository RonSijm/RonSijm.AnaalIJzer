using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.Graphing.Model;

public sealed class ArchitectureGraphRule
{
	public ArchitectureGraphRule(
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
		From = from;
		To = to;
		ScopePath = scopePath;
		Kind = kind;
		SiteText = siteText;
		AppliesToDescendants = appliesToDescendants;
		IsWildcard = isWildcard;
		IsActive = isActive;
		ConfiguredFrom = string.IsNullOrWhiteSpace(configuredFrom) ? from : configuredFrom;
		ConfiguredTo = string.IsNullOrWhiteSpace(configuredTo) ? to : configuredTo;
		SourcePath = sourcePath;
		SourceKind = sourceKind;
		XmlLineNumber = xmlLineNumber;
		XmlLinePosition = xmlLinePosition;
		AllowedSites = allowedSites.IsDefault ? ImmutableArray<string>.Empty : allowedSites;
		BlockedSites = blockedSites.IsDefault ? ImmutableArray<string>.Empty : blockedSites;
		Description = description;
		EditHandle = new ArchitectureDependencyRuleEditHandle(
			SourceKind,
			SourcePath,
			XmlLineNumber,
			XmlLinePosition,
			Kind,
			ScopePath,
			ConfiguredFrom,
			ConfiguredTo,
			From,
			To,
			AppliesToDescendants,
			AllowedSites,
			BlockedSites,
			Description);
	}

	public string From { get; }

	public string To { get; }

	public string ScopePath { get; }

	public string Kind { get; }

	public string SiteText { get; }

	public bool AppliesToDescendants { get; }

	public bool IsWildcard { get; }

	public bool IsActive { get; }

	public string ConfiguredFrom { get; }

	public string ConfiguredTo { get; }

	public string SourcePath { get; }

	public ArchitectureConfigurationSourceKind SourceKind { get; }

	public int XmlLineNumber { get; }

	public int XmlLinePosition { get; }

	public ImmutableArray<string> AllowedSites { get; }

	public ImmutableArray<string> BlockedSites { get; }

	public string? Description { get; }

	public ArchitectureDependencyRuleEditHandle EditHandle { get; }
}
