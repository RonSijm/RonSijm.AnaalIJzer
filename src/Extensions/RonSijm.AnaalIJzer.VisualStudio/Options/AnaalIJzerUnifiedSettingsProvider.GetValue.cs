namespace RonSijm.AnaalIJzer.VisualStudio.Options;

internal sealed partial class AnaalIJzerUnifiedSettingsProvider
{
	private static object? GetValue(string moniker, AnaalIJzerOptionsPage options)
	{
		var result = moniker switch
		{
			AnaalIJzerUnifiedSettingMonikers.EnableInlineLayerBadges => (object)options.EnableInlineLayerBadges,
			AnaalIJzerUnifiedSettingMonikers.EnableLayerCodeLens => (object)options.EnableLayerCodeLens,
			AnaalIJzerUnifiedSettingMonikers.ShowLayerBadgesWhenNotInLayer => (object)options.ShowLayerBadgesWhenNotInLayer,
			AnaalIJzerUnifiedSettingMonikers.ShowGlobalLayerRulesInBadges => (object)options.ShowGlobalLayerRulesInBadges,
			AnaalIJzerUnifiedSettingMonikers.ShowLinearCallChainInBadges => (object)options.ShowLinearCallChainInBadges,
			AnaalIJzerUnifiedSettingMonikers.EnableLayerGlyphs => (object)options.EnableLayerGlyphs,
			AnaalIJzerUnifiedSettingMonikers.EnableLayerBlockHighlight => (object)options.EnableLayerBlockHighlight,
			AnaalIJzerUnifiedSettingMonikers.EnableLayerBackgroundTint => (object)options.EnableLayerBackgroundTint,
			AnaalIJzerUnifiedSettingMonikers.ShowAllLayerInformation => (object)options.ShowAllLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowConstructorLayerInformation => (object)options.ShowConstructorLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowMethodLayerInformation => (object)options.ShowMethodLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowMethodReturnLayerInformation => (object)options.ShowMethodReturnLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowFieldLayerInformation => (object)options.ShowFieldLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowPropertyLayerInformation => (object)options.ShowPropertyLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowLocalLayerInformation => (object)options.ShowLocalLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowNewLayerInformation => (object)options.ShowNewLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowGenericInvocationLayerInformation => (object)options.ShowGenericInvocationLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowGenericArgumentLayerInformation => (object)options.ShowGenericArgumentLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowInheritanceLayerInformation => (object)options.ShowInheritanceLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowInterfaceImplementationLayerInformation => (object)options.ShowInterfaceImplementationLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowAttributeLayerInformation => (object)options.ShowAttributeLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowStaticMemberLayerInformation => (object)options.ShowStaticMemberLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowAllSiteDiagnostics => (object)options.ShowAllSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowConstructorSiteDiagnostics => (object)options.ShowConstructorSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowMethodSiteDiagnostics => (object)options.ShowMethodSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowMethodReturnSiteDiagnostics => (object)options.ShowMethodReturnSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowFieldSiteDiagnostics => (object)options.ShowFieldSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowPropertySiteDiagnostics => (object)options.ShowPropertySiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowLocalSiteDiagnostics => (object)options.ShowLocalSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowNewSiteDiagnostics => (object)options.ShowNewSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowGenericInvocationSiteDiagnostics => (object)options.ShowGenericInvocationSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowGenericArgumentSiteDiagnostics => (object)options.ShowGenericArgumentSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowInheritanceSiteDiagnostics => (object)options.ShowInheritanceSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowInterfaceImplementationSiteDiagnostics => (object)options.ShowInterfaceImplementationSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowAttributeSiteDiagnostics => (object)options.ShowAttributeSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowStaticMemberSiteDiagnostics => (object)options.ShowStaticMemberSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.DependencyGraphFocusMode => (object)options.DependencyGraphFocusMode.ToString(),
			AnaalIJzerUnifiedSettingMonikers.OpenAnlFilesInGraphEditor => (object)options.OpenAnlFilesInGraphEditor,
			AnaalIJzerUnifiedSettingMonikers.IncludeCodeEvidenceInDependencyGraphs => (object)options.IncludeCodeEvidenceInDependencyGraphs,
			_ => null
		};

		return result;
	}
}
