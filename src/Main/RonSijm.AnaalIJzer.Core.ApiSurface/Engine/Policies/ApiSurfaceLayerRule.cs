using RonSijm.AnaalIJzer.Core.DependencyRules;

namespace RonSijm.AnaalIJzer.Core.ApiSurface.Engine.Policies;

public readonly struct ApiSurfaceLayerRule(
	string layerPath,
	string configuredPath,
	DependencySiteFilter siteFilter,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public string LayerPath { get; } = layerPath;
	public string ConfiguredPath { get; } = configuredPath;
	public DependencySiteFilter SiteFilter { get; } = siteFilter;
	public string? Description { get; } = description;
	public string XmlPath { get; } = xmlPath;
	public int XmlLineNumber { get; } = xmlLineNumber;
	public int XmlLinePosition { get; } = xmlLinePosition;
}
