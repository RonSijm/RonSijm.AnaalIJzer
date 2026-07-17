using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Model;

public sealed class ArchitectureDependencyRuleEditHandle(
    ArchitectureConfigurationSourceKind sourceKind,
    string sourcePath,
    int xmlLineNumber,
    int xmlLinePosition,
    string elementKind,
    string scopePath,
    string configuredFrom,
    string configuredTo,
    string from,
    string to,
    bool appliesToDescendants,
    ImmutableArray<string> allowedSites = default,
    ImmutableArray<string> blockedSites = default,
    string? description = null)
{
    public ArchitectureConfigurationSourceKind SourceKind { get; } = sourceKind;

    public string SourcePath { get; } = sourcePath;

    public int XmlLineNumber { get; } = xmlLineNumber;

    public int XmlLinePosition { get; } = xmlLinePosition;

    public string ElementKind { get; } = elementKind;

    public string ScopePath { get; } = scopePath;

    public string ConfiguredFrom { get; } = configuredFrom;

    public string ConfiguredTo { get; } = configuredTo;

    public string From { get; } = from;

    public string To { get; } = to;

    public bool AppliesToDescendants { get; } = appliesToDescendants;

    public ImmutableArray<string> AllowedSites { get; } = allowedSites.IsDefault ? ImmutableArray<string>.Empty : allowedSites;

    public ImmutableArray<string> BlockedSites { get; } = blockedSites.IsDefault ? ImmutableArray<string>.Empty : blockedSites;

    public string? Description { get; } = description;

    public bool CanEdit
	{
		get
		{
			var result = SourceKind != ArchitectureConfigurationSourceKind.None
			             && !string.IsNullOrWhiteSpace(SourcePath)
			             && !string.IsNullOrWhiteSpace(ElementKind);

			return result;
		}
	}

	public static ArchitectureDependencyRuleEditHandle None { get; } = new(
		ArchitectureConfigurationSourceKind.None,
		string.Empty,
		0,
		0,
		string.Empty,
		string.Empty,
		string.Empty,
		string.Empty,
		string.Empty,
		string.Empty,
		false);
}
