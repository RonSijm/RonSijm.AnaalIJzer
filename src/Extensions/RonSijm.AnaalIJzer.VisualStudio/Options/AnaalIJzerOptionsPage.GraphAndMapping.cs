using System.ComponentModel;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;
using RonSijm.AnaalIJzer.GraphModel.Model;

namespace RonSijm.AnaalIJzer.VisualStudio.Options;

public sealed partial class AnaalIJzerOptionsPage
{
	[Category("Dependency graphs")]
	[DisplayName("Graph focus mode")]
	[Description("Controls whether the dependency graph tool window shows all graphs, highlights graphs that affect the active editor, or filters to only those graphs.")]
	public ArchitectureGraphFocusMode DependencyGraphFocusMode { get; set; } = ArchitectureGraphFocusMode.HighlightCurrent;

	[Category("Dependency graphs")]
	[DisplayName("Open .anl files in diagram editor")]
	[Description("When an AnaalIJzer .anl settings file is opened or selected, show it in the dependency graph editor automatically.")]
	public bool OpenAnlFilesInGraphEditor { get; set; } = true;

	[Category("Dependency graphs")]
	[DisplayName("Include code evidence")]
	[Description("Include project-code evidence in dependency graph snapshots so layer boxes can show matching types and observed violations.")]
	public bool IncludeCodeEvidenceInDependencyGraphs { get; set; }

	internal ArchitectureEditorOptions ToEditorOptions()
	{
		var siteDiagnostics = ToSiteDiagnosticOptions();
		var siteLayerInformation = ToSiteLayerInformationOptions();
		var result = new ArchitectureEditorOptions(
			EnableInlineLayerBadges,
			EnableLayerGlyphs,
			EnableLayerBlockHighlight,
			false,
			DependencyGraphFocusMode,
			siteDiagnostics,
			ShowLayerBadgesWhenNotInLayer,
			EnableLayerBackgroundTint,
			ShowGlobalLayerRulesInBadges,
			ShowLinearCallChainInBadges,
			siteLayerInformation,
			EnableLayerCodeLens,
			OpenAnlFilesInGraphEditor,
			IncludeCodeEvidenceInDependencyGraphs);

		return result;
	}

	internal void ApplyEditorOptions(ArchitectureEditorOptions options)
	{
		EnableInlineLayerBadges = options.EnableInlineLayerBadges;
		EnableLayerCodeLens = options.EnableLayerCodeLens;
		EnableLayerGlyphs = options.EnableLayerGlyphs;
		EnableLayerBlockHighlight = options.EnableLayerBlockHighlight;
		EnableLayerBackgroundTint = options.EnableLayerTextBackgroundTint;
		ShowLayerBadgesWhenNotInLayer = options.ShowLayerBadgesWhenNotInLayer;
		ShowGlobalLayerRulesInBadges = options.ShowGlobalLayerRulesInBadges;
		ShowLinearCallChainInBadges = options.ShowLinearCallChainInBadges;
		DependencyGraphFocusMode = options.DependencyGraphFocusMode;
		OpenAnlFilesInGraphEditor = options.OpenAnlFilesInGraphEditor;
		IncludeCodeEvidenceInDependencyGraphs = options.IncludeCodeEvidenceInDependencyGraphs;
		ApplySiteDiagnosticOptions(options.SiteDiagnostics);
		ApplySiteLayerInformationOptions(options.SiteLayerInformation);
	}
}
