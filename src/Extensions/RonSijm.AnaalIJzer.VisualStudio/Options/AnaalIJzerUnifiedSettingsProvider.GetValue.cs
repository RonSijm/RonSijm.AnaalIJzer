namespace RonSijm.AnaalIJzer.VisualStudio.Options;

internal sealed partial class AnaalIJzerUnifiedSettingsProvider
{
	private static object? GetValue(string moniker, AnaalIJzerOptionsPage options)
	{
		object? result = moniker switch
		{
			AnaalIJzerUnifiedSettingMonikers.EnableInlineLayerBadges => options.EnableInlineLayerBadges,
			AnaalIJzerUnifiedSettingMonikers.EnableLayerCodeLens => options.EnableLayerCodeLens,
			AnaalIJzerUnifiedSettingMonikers.ShowLayerBadgesWhenNotInLayer => options.ShowLayerBadgesWhenNotInLayer,
			AnaalIJzerUnifiedSettingMonikers.ShowGlobalLayerRulesInBadges => options.ShowGlobalLayerRulesInBadges,
			AnaalIJzerUnifiedSettingMonikers.ShowLinearCallChainInBadges => options.ShowLinearCallChainInBadges,
			AnaalIJzerUnifiedSettingMonikers.EnableLayerGlyphs => options.EnableLayerGlyphs,
			AnaalIJzerUnifiedSettingMonikers.EnableLayerBlockHighlight => options.EnableLayerBlockHighlight,
			AnaalIJzerUnifiedSettingMonikers.EnableLayerBackgroundTint => options.EnableLayerBackgroundTint,
			AnaalIJzerUnifiedSettingMonikers.ShowAllLayerInformation => options.ShowAllLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowConstructorLayerInformation => options.ShowConstructorLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowMethodLayerInformation => options.ShowMethodLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowMethodReturnLayerInformation => options.ShowMethodReturnLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowFieldLayerInformation => options.ShowFieldLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowPropertyLayerInformation => options.ShowPropertyLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowLocalLayerInformation => options.ShowLocalLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowNewLayerInformation => options.ShowNewLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowGenericInvocationLayerInformation => options.ShowGenericInvocationLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowGenericArgumentLayerInformation => options.ShowGenericArgumentLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowInheritanceLayerInformation => options.ShowInheritanceLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowInterfaceImplementationLayerInformation => options.ShowInterfaceImplementationLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowAttributeLayerInformation => options.ShowAttributeLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowStaticMemberLayerInformation => options.ShowStaticMemberLayerInformation,
			AnaalIJzerUnifiedSettingMonikers.ShowAllSiteDiagnostics => options.ShowAllSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowConstructorSiteDiagnostics => options.ShowConstructorSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowMethodSiteDiagnostics => options.ShowMethodSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowMethodReturnSiteDiagnostics => options.ShowMethodReturnSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowFieldSiteDiagnostics => options.ShowFieldSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowPropertySiteDiagnostics => options.ShowPropertySiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowLocalSiteDiagnostics => options.ShowLocalSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowNewSiteDiagnostics => options.ShowNewSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowGenericInvocationSiteDiagnostics => options.ShowGenericInvocationSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowGenericArgumentSiteDiagnostics => options.ShowGenericArgumentSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowInheritanceSiteDiagnostics => options.ShowInheritanceSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowInterfaceImplementationSiteDiagnostics => options.ShowInterfaceImplementationSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowAttributeSiteDiagnostics => options.ShowAttributeSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.ShowStaticMemberSiteDiagnostics => options.ShowStaticMemberSiteDiagnostics,
			AnaalIJzerUnifiedSettingMonikers.DependencyGraphFocusMode => options.DependencyGraphFocusMode.ToString(),
			AnaalIJzerUnifiedSettingMonikers.OpenAnlFilesInGraphEditor => options.OpenAnlFilesInGraphEditor,
			AnaalIJzerUnifiedSettingMonikers.IncludeCodeEvidenceInDependencyGraphs => options.IncludeCodeEvidenceInDependencyGraphs,
			_ => null
		};

		return result;
	}
}
