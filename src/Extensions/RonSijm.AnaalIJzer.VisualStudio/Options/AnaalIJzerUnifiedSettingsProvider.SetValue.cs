using System.Globalization;
using RonSijm.AnaalIJzer.Graphing.Model;

namespace RonSijm.AnaalIJzer.VisualStudio.Options;

internal sealed partial class AnaalIJzerUnifiedSettingsProvider
{
	private static bool TrySetValue<T>(string moniker, T value, AnaalIJzerOptionsPage options)
	{
		if (moniker == AnaalIJzerUnifiedSettingMonikers.DependencyGraphFocusMode)
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

	private static bool TrySetBooleanValue(string moniker, bool value, AnaalIJzerOptionsPage options)
	{
		var result = true;
		switch (moniker)
		{
			case AnaalIJzerUnifiedSettingMonikers.EnableInlineLayerBadges:
				options.EnableInlineLayerBadges = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.EnableLayerCodeLens:
				options.EnableLayerCodeLens = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowLayerBadgesWhenNotInLayer:
				options.ShowLayerBadgesWhenNotInLayer = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowGlobalLayerRulesInBadges:
				options.ShowGlobalLayerRulesInBadges = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowLinearCallChainInBadges:
				options.ShowLinearCallChainInBadges = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.EnableLayerGlyphs:
				options.EnableLayerGlyphs = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.EnableLayerBlockHighlight:
				options.EnableLayerBlockHighlight = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.EnableLayerBackgroundTint:
				options.EnableLayerBackgroundTint = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowAllLayerInformation:
				options.ShowAllLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowConstructorLayerInformation:
				options.ShowConstructorLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowMethodLayerInformation:
				options.ShowMethodLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowMethodReturnLayerInformation:
				options.ShowMethodReturnLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowFieldLayerInformation:
				options.ShowFieldLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowPropertyLayerInformation:
				options.ShowPropertyLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowLocalLayerInformation:
				options.ShowLocalLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowNewLayerInformation:
				options.ShowNewLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowGenericInvocationLayerInformation:
				options.ShowGenericInvocationLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowGenericArgumentLayerInformation:
				options.ShowGenericArgumentLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowInheritanceLayerInformation:
				options.ShowInheritanceLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowInterfaceImplementationLayerInformation:
				options.ShowInterfaceImplementationLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowAttributeLayerInformation:
				options.ShowAttributeLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowStaticMemberLayerInformation:
				options.ShowStaticMemberLayerInformation = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowAllSiteDiagnostics:
				options.ShowAllSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowConstructorSiteDiagnostics:
				options.ShowConstructorSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowMethodSiteDiagnostics:
				options.ShowMethodSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowMethodReturnSiteDiagnostics:
				options.ShowMethodReturnSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowFieldSiteDiagnostics:
				options.ShowFieldSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowPropertySiteDiagnostics:
				options.ShowPropertySiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowLocalSiteDiagnostics:
				options.ShowLocalSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowNewSiteDiagnostics:
				options.ShowNewSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowGenericInvocationSiteDiagnostics:
				options.ShowGenericInvocationSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowGenericArgumentSiteDiagnostics:
				options.ShowGenericArgumentSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowInheritanceSiteDiagnostics:
				options.ShowInheritanceSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowInterfaceImplementationSiteDiagnostics:
				options.ShowInterfaceImplementationSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowAttributeSiteDiagnostics:
				options.ShowAttributeSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.ShowStaticMemberSiteDiagnostics:
				options.ShowStaticMemberSiteDiagnostics = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.OpenAnlFilesInGraphEditor:
				options.OpenAnlFilesInGraphEditor = value;
				break;
			case AnaalIJzerUnifiedSettingMonikers.IncludeCodeEvidenceInDependencyGraphs:
				options.IncludeCodeEvidenceInDependencyGraphs = value;
				break;
			default:
				result = false;
				break;
		}

		return result;
	}
}
