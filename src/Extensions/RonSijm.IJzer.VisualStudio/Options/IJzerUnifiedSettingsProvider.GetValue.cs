namespace RonSijm.IJzer.VisualStudio.Options;

internal sealed partial class IJzerUnifiedSettingsProvider
{
    private static object? GetValue(string moniker, IJzerOptionsPage options)
    {
        object? result = moniker switch
        {
            IJzerUnifiedSettingMonikers.EnableInlineLayerBadges => options.EnableInlineLayerBadges,
            IJzerUnifiedSettingMonikers.EnableLayerCodeLens => options.EnableLayerCodeLens,
            IJzerUnifiedSettingMonikers.ShowLayerBadgesWhenNotInLayer => options.ShowLayerBadgesWhenNotInLayer,
            IJzerUnifiedSettingMonikers.ShowGlobalLayerRulesInBadges => options.ShowGlobalLayerRulesInBadges,
            IJzerUnifiedSettingMonikers.ShowLinearCallChainInBadges => options.ShowLinearCallChainInBadges,
            IJzerUnifiedSettingMonikers.EnableLayerGlyphs => options.EnableLayerGlyphs,
            IJzerUnifiedSettingMonikers.EnableLayerBlockHighlight => options.EnableLayerBlockHighlight,
            IJzerUnifiedSettingMonikers.EnableLayerBackgroundTint => options.EnableLayerBackgroundTint,
            IJzerUnifiedSettingMonikers.ShowAllLayerInformation => options.ShowAllLayerInformation,
            IJzerUnifiedSettingMonikers.ShowConstructorLayerInformation => options.ShowConstructorLayerInformation,
            IJzerUnifiedSettingMonikers.ShowMethodLayerInformation => options.ShowMethodLayerInformation,
            IJzerUnifiedSettingMonikers.ShowMethodReturnLayerInformation => options.ShowMethodReturnLayerInformation,
            IJzerUnifiedSettingMonikers.ShowFieldLayerInformation => options.ShowFieldLayerInformation,
            IJzerUnifiedSettingMonikers.ShowPropertyLayerInformation => options.ShowPropertyLayerInformation,
            IJzerUnifiedSettingMonikers.ShowLocalLayerInformation => options.ShowLocalLayerInformation,
            IJzerUnifiedSettingMonikers.ShowNewLayerInformation => options.ShowNewLayerInformation,
            IJzerUnifiedSettingMonikers.ShowGenericInvocationLayerInformation => options.ShowGenericInvocationLayerInformation,
            IJzerUnifiedSettingMonikers.ShowGenericArgumentLayerInformation => options.ShowGenericArgumentLayerInformation,
            IJzerUnifiedSettingMonikers.ShowInheritanceLayerInformation => options.ShowInheritanceLayerInformation,
            IJzerUnifiedSettingMonikers.ShowInterfaceImplementationLayerInformation => options.ShowInterfaceImplementationLayerInformation,
            IJzerUnifiedSettingMonikers.ShowAttributeLayerInformation => options.ShowAttributeLayerInformation,
            IJzerUnifiedSettingMonikers.ShowStaticMemberLayerInformation => options.ShowStaticMemberLayerInformation,
            IJzerUnifiedSettingMonikers.ShowAllSiteDiagnostics => options.ShowAllSiteDiagnostics,
            IJzerUnifiedSettingMonikers.ShowConstructorSiteDiagnostics => options.ShowConstructorSiteDiagnostics,
            IJzerUnifiedSettingMonikers.ShowMethodSiteDiagnostics => options.ShowMethodSiteDiagnostics,
            IJzerUnifiedSettingMonikers.ShowMethodReturnSiteDiagnostics => options.ShowMethodReturnSiteDiagnostics,
            IJzerUnifiedSettingMonikers.ShowFieldSiteDiagnostics => options.ShowFieldSiteDiagnostics,
            IJzerUnifiedSettingMonikers.ShowPropertySiteDiagnostics => options.ShowPropertySiteDiagnostics,
            IJzerUnifiedSettingMonikers.ShowLocalSiteDiagnostics => options.ShowLocalSiteDiagnostics,
            IJzerUnifiedSettingMonikers.ShowNewSiteDiagnostics => options.ShowNewSiteDiagnostics,
            IJzerUnifiedSettingMonikers.ShowGenericInvocationSiteDiagnostics => options.ShowGenericInvocationSiteDiagnostics,
            IJzerUnifiedSettingMonikers.ShowGenericArgumentSiteDiagnostics => options.ShowGenericArgumentSiteDiagnostics,
            IJzerUnifiedSettingMonikers.ShowInheritanceSiteDiagnostics => options.ShowInheritanceSiteDiagnostics,
            IJzerUnifiedSettingMonikers.ShowInterfaceImplementationSiteDiagnostics => options.ShowInterfaceImplementationSiteDiagnostics,
            IJzerUnifiedSettingMonikers.ShowAttributeSiteDiagnostics => options.ShowAttributeSiteDiagnostics,
            IJzerUnifiedSettingMonikers.ShowStaticMemberSiteDiagnostics => options.ShowStaticMemberSiteDiagnostics,
            IJzerUnifiedSettingMonikers.DependencyGraphFocusMode => options.DependencyGraphFocusMode.ToString(),
            IJzerUnifiedSettingMonikers.OpenAnlFilesInGraphEditor => options.OpenAnlFilesInGraphEditor,
            IJzerUnifiedSettingMonikers.IncludeCodeEvidenceInDependencyGraphs => options.IncludeCodeEvidenceInDependencyGraphs,
            _ => null
        };

        return result;
    }
}