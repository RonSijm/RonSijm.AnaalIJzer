using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.Graphing.ViewModels;

public sealed class ArchitectureGraphEdgeViewModel
{
	public ArchitectureGraphEdgeViewModel(
		string from,
		string to,
		string kind,
		string siteText,
		bool appliesToDescendants,
		bool isActive,
		string scopePath = "",
		string configuredFrom = "",
		string configuredTo = "",
		string sourcePath = "",
		ArchitectureConfigurationSourceKind sourceKind = ArchitectureConfigurationSourceKind.None,
		int xmlLineNumber = 0,
		int xmlLinePosition = 0,
		ImmutableArray<string> allowedSites = default,
		ImmutableArray<string> blockedSites = default,
		string? description = null,
		bool isEvidence = false,
		int observedUsageCount = 0,
		int violationCount = 0)
	{
		From = from;
		To = to;
		Kind = kind;
		SiteText = siteText;
		AppliesToDescendants = appliesToDescendants;
		IsActive = isActive;
		ScopePath = scopePath;
		ConfiguredFrom = string.IsNullOrWhiteSpace(configuredFrom) ? from : configuredFrom;
		ConfiguredTo = string.IsNullOrWhiteSpace(configuredTo) ? to : configuredTo;
		SourcePath = sourcePath;
		SourceKind = sourceKind;
		XmlLineNumber = xmlLineNumber;
		XmlLinePosition = xmlLinePosition;
		AllowedSites = allowedSites.IsDefault ? ImmutableArray<string>.Empty : allowedSites;
		BlockedSites = blockedSites.IsDefault ? ImmutableArray<string>.Empty : blockedSites;
		Description = description;
		IsEvidence = isEvidence;
		ObservedUsageCount = observedUsageCount;
		ViolationCount = violationCount;
		EditHandle = IsEvidence
			? ArchitectureDependencyRuleEditHandle.None
			: new ArchitectureDependencyRuleEditHandle(
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

	public string Kind { get; }

	public string SiteText { get; }

	public bool AppliesToDescendants { get; }

	public bool IsActive { get; }

	public string ScopePath { get; }

	public string ConfiguredFrom { get; }

	public string ConfiguredTo { get; }

	public string SourcePath { get; }

	public ArchitectureConfigurationSourceKind SourceKind { get; }

	public int XmlLineNumber { get; }

	public int XmlLinePosition { get; }

	public ImmutableArray<string> AllowedSites { get; }

	public ImmutableArray<string> BlockedSites { get; }

	public string? Description { get; }

	public bool IsEvidence { get; }

	public int ObservedUsageCount { get; }

	public int ViolationCount { get; }

	public ArchitectureDependencyRuleEditHandle EditHandle { get; }

	public bool IsBlocked
	{
		get
		{
			var result = Kind == "BlockedDependency";

			return result;
		}
	}
}
