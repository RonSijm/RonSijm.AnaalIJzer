using RonSijm.AnaalIJzer.Indicators;
using RonSijm.AnaalIJzer.Snapshots;

namespace RonSijm.AnaalIJzer.VisualStudio.Shell.Commands;

internal static class SitesDiagnosticsCommandState
{
	internal static ArchitectureEditorOptions Toggle(ArchitectureEditorOptions options)
	{
		var siteDiagnostics = options.EnableSitesDiagnostics
			? ArchitectureSiteDiagnosticOptions.None
			: ArchitectureSiteDiagnosticOptions.All;
		var result = new ArchitectureEditorOptions(
			options.EnableInlineLayerBadges,
			options.EnableLayerGlyphs,
			options.EnableLayerBackgroundTint,
			false,
			options.DependencyGraphFocusMode,
			siteDiagnostics,
			options.ShowLayerBadgesWhenNotInLayer,
			options.EnableLayerTextBackgroundTint,
			options.ShowGlobalLayerRulesInBadges,
			options.ShowLinearCallChainInBadges,
			options.SiteLayerInformation,
			options.EnableLayerCodeLens,
			options.OpenAnlFilesInGraphEditor);

		return result;
	}
}
