using RonSijm.AnaalIJzer.Graph;
using RonSijm.AnaalIJzer.Indicators;

namespace RonSijm.AnaalIJzer.Snapshots;

public sealed class ArchitectureEditorOptions
{
	public ArchitectureEditorOptions(
		bool enableInlineLayerBadges = true,
		bool enableLayerGlyphs = true,
		bool enableLayerBackgroundTint = true,
		bool enableSitesDiagnostics = false,
		ArchitectureGraphFocusMode dependencyGraphFocusMode = ArchitectureGraphFocusMode.HighlightCurrent,
		ArchitectureSiteDiagnosticOptions? siteDiagnostics = null,
		bool showLayerBadgesWhenNotInLayer = false,
		bool enableLayerTextBackgroundTint = false,
		bool showGlobalLayerRulesInBadges = false,
		bool showLinearCallChainInBadges = true,
		ArchitectureSiteLayerInformationOptions? siteLayerInformation = null,
		bool enableLayerCodeLens = true,
		bool openAnlFilesInGraphEditor = true,
		bool includeCodeEvidenceInDependencyGraphs = false)
	{
		EnableInlineLayerBadges = enableInlineLayerBadges;
		EnableLayerGlyphs = enableLayerGlyphs;
		EnableLayerBlockHighlight = enableLayerBackgroundTint;
		EnableLayerTextBackgroundTint = enableLayerTextBackgroundTint;
		SiteDiagnostics = siteDiagnostics ?? (enableSitesDiagnostics ? ArchitectureSiteDiagnosticOptions.All : ArchitectureSiteDiagnosticOptions.None);
		SiteLayerInformation = siteLayerInformation ?? ArchitectureSiteLayerInformationOptions.None;
		EnableSitesDiagnostics = SiteDiagnostics.AnyEnabled;
		EnableSiteLayerInformation = SiteLayerInformation.AnyEnabled;
		DependencyGraphFocusMode = dependencyGraphFocusMode;
		ShowLayerBadgesWhenNotInLayer = showLayerBadgesWhenNotInLayer;
		ShowGlobalLayerRulesInBadges = showGlobalLayerRulesInBadges;
		ShowLinearCallChainInBadges = showLinearCallChainInBadges;
		EnableLayerCodeLens = enableLayerCodeLens;
		OpenAnlFilesInGraphEditor = openAnlFilesInGraphEditor;
		IncludeCodeEvidenceInDependencyGraphs = includeCodeEvidenceInDependencyGraphs;
	}

	public bool EnableInlineLayerBadges { get; }

	public bool EnableLayerGlyphs { get; }

	public bool EnableLayerBlockHighlight { get; }

	public bool EnableLayerTextBackgroundTint { get; }

	public bool EnableLayerBackgroundTint
	{
		get
		{
			var result = EnableLayerBlockHighlight;

			return result;
		}
	}

	public bool EnableHighlightCodeInLayer
	{
		get
		{
			var result = EnableLayerBlockHighlight;

			return result;
		}
	}

	public bool EnableSitesDiagnostics { get; }

	public ArchitectureSiteDiagnosticOptions SiteDiagnostics { get; }

	public bool EnableSiteLayerInformation { get; }

	public ArchitectureSiteLayerInformationOptions SiteLayerInformation { get; }

	public bool ShowLayerBadgesWhenNotInLayer { get; }

	public bool ShowGlobalLayerRulesInBadges { get; }

	public bool ShowLinearCallChainInBadges { get; }

	public bool EnableLayerCodeLens { get; }

	public bool OpenAnlFilesInGraphEditor { get; }

	public bool IncludeCodeEvidenceInDependencyGraphs { get; }

	public ArchitectureGraphFocusMode DependencyGraphFocusMode { get; }

	public static ArchitectureEditorOptions Default { get; } = new();

	public bool IsSiteDiagnosticEnabled(string site)
	{
		var result = SiteDiagnostics.IsEnabled(site);

		return result;
	}

	public bool IsSiteLayerInformationEnabled(string site)
	{
		var result = SiteLayerInformation.IsEnabled(site);

		return result;
	}
}
