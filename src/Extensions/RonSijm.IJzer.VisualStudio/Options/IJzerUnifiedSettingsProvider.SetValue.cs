using System.Globalization;
using RonSijm.IJzer.GraphModel.Model;

namespace RonSijm.IJzer.VisualStudio.Options;

internal sealed partial class IJzerUnifiedSettingsProvider
{
    private static bool TrySetValue<T>(string moniker, T value, IJzerOptionsPage options)
    {
        if (moniker == IJzerUnifiedSettingMonikers.DependencyGraphFocusMode)
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            var parsed = Enum.TryParse(text, ignoreCase: true, out ArchitectureGraphFocusMode focusMode);
            if (parsed)
            {
                options.DependencyGraphFocusMode = focusMode;
            }

            return parsed;
        }

        if (value is not bool booleanValue)
        {
            return false;
        }

        var result = TrySetBooleanValue(moniker, booleanValue, options);

        return result;
    }

    private static bool TrySetBooleanValue(string moniker, bool value, IJzerOptionsPage options)
    {
        var result = true;
        switch (moniker)
        {
            case IJzerUnifiedSettingMonikers.EnableInlineLayerBadges:
                options.EnableInlineLayerBadges = value;
                break;
            case IJzerUnifiedSettingMonikers.EnableLayerCodeLens:
                options.EnableLayerCodeLens = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowLayerBadgesWhenNotInLayer:
                options.ShowLayerBadgesWhenNotInLayer = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowGlobalLayerRulesInBadges:
                options.ShowGlobalLayerRulesInBadges = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowLinearCallChainInBadges:
                options.ShowLinearCallChainInBadges = value;
                break;
            case IJzerUnifiedSettingMonikers.EnableLayerGlyphs:
                options.EnableLayerGlyphs = value;
                break;
            case IJzerUnifiedSettingMonikers.EnableLayerBlockHighlight:
                options.EnableLayerBlockHighlight = value;
                break;
            case IJzerUnifiedSettingMonikers.EnableLayerBackgroundTint:
                options.EnableLayerBackgroundTint = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowAllLayerInformation:
                options.ShowAllLayerInformation = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowConstructorLayerInformation:
                options.ShowConstructorLayerInformation = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowMethodLayerInformation:
                options.ShowMethodLayerInformation = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowMethodReturnLayerInformation:
                options.ShowMethodReturnLayerInformation = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowFieldLayerInformation:
                options.ShowFieldLayerInformation = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowPropertyLayerInformation:
                options.ShowPropertyLayerInformation = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowLocalLayerInformation:
                options.ShowLocalLayerInformation = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowNewLayerInformation:
                options.ShowNewLayerInformation = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowGenericInvocationLayerInformation:
                options.ShowGenericInvocationLayerInformation = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowGenericArgumentLayerInformation:
                options.ShowGenericArgumentLayerInformation = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowInheritanceLayerInformation:
                options.ShowInheritanceLayerInformation = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowInterfaceImplementationLayerInformation:
                options.ShowInterfaceImplementationLayerInformation = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowAttributeLayerInformation:
                options.ShowAttributeLayerInformation = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowStaticMemberLayerInformation:
                options.ShowStaticMemberLayerInformation = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowAllSiteDiagnostics:
                options.ShowAllSiteDiagnostics = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowConstructorSiteDiagnostics:
                options.ShowConstructorSiteDiagnostics = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowMethodSiteDiagnostics:
                options.ShowMethodSiteDiagnostics = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowMethodReturnSiteDiagnostics:
                options.ShowMethodReturnSiteDiagnostics = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowFieldSiteDiagnostics:
                options.ShowFieldSiteDiagnostics = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowPropertySiteDiagnostics:
                options.ShowPropertySiteDiagnostics = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowLocalSiteDiagnostics:
                options.ShowLocalSiteDiagnostics = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowNewSiteDiagnostics:
                options.ShowNewSiteDiagnostics = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowGenericInvocationSiteDiagnostics:
                options.ShowGenericInvocationSiteDiagnostics = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowGenericArgumentSiteDiagnostics:
                options.ShowGenericArgumentSiteDiagnostics = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowInheritanceSiteDiagnostics:
                options.ShowInheritanceSiteDiagnostics = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowInterfaceImplementationSiteDiagnostics:
                options.ShowInterfaceImplementationSiteDiagnostics = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowAttributeSiteDiagnostics:
                options.ShowAttributeSiteDiagnostics = value;
                break;
            case IJzerUnifiedSettingMonikers.ShowStaticMemberSiteDiagnostics:
                options.ShowStaticMemberSiteDiagnostics = value;
                break;
            case IJzerUnifiedSettingMonikers.OpenAnlFilesInGraphEditor:
                options.OpenAnlFilesInGraphEditor = value;
                break;
            case IJzerUnifiedSettingMonikers.IncludeCodeEvidenceInDependencyGraphs:
                options.IncludeCodeEvidenceInDependencyGraphs = value;
                break;
            default:
                result = false;
                break;
        }

        return result;
    }
}