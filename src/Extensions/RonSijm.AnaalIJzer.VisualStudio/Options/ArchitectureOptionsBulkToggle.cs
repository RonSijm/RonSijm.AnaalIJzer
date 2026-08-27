using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.VisualStudio.Options;

internal static class ArchitectureOptionsBulkToggle
{
	internal static ArchitectureSiteLayerInformationOptions CreateLayerInformationOptions(bool enabled)
	{
		var result = new ArchitectureSiteLayerInformationOptions(
			enabled,
			enabled,
			enabled,
			enabled,
			enabled,
			enabled,
			enabled,
			enabled,
			enabled,
			enabled,
			enabled,
			enabled,
			enabled);

		return result;
	}

	internal static ArchitectureSiteDiagnosticOptions CreateSiteDiagnosticOptions(bool enabled)
	{
		var result = new ArchitectureSiteDiagnosticOptions(
			enabled,
			enabled,
			enabled,
			enabled,
			enabled,
			enabled,
			enabled,
			enabled,
			enabled,
			enabled,
			enabled,
			enabled,
			enabled);

		return result;
	}

	internal static bool AreAllLayerInformationOptionsEnabled(ArchitectureSiteLayerInformationOptions options)
	{
		var result = options is
		{
			ShowConstructorLayerInformation: true,
			ShowMethodLayerInformation: true,
			ShowMethodReturnLayerInformation: true,
			ShowFieldLayerInformation: true,
			ShowPropertyLayerInformation: true,
			ShowLocalLayerInformation: true,
			ShowNewLayerInformation: true,
			ShowGenericInvocationLayerInformation: true,
			ShowGenericArgumentLayerInformation: true,
			ShowInheritanceLayerInformation: true,
			ShowInterfaceImplementationLayerInformation: true,
			ShowAttributeLayerInformation: true,
			ShowStaticMemberLayerInformation: true
		};

		return result;
	}

	internal static bool AreAllSiteDiagnosticOptionsEnabled(ArchitectureSiteDiagnosticOptions options)
	{
		var result = options is
		{
			ShowConstructorSiteDiagnostics: true,
			ShowMethodSiteDiagnostics: true,
			ShowMethodReturnSiteDiagnostics: true,
			ShowFieldSiteDiagnostics: true,
			ShowPropertySiteDiagnostics: true,
			ShowLocalSiteDiagnostics: true,
			ShowNewSiteDiagnostics: true,
			ShowGenericInvocationSiteDiagnostics: true,
			ShowGenericArgumentSiteDiagnostics: true,
			ShowInheritanceSiteDiagnostics: true,
			ShowInterfaceImplementationSiteDiagnostics: true,
			ShowAttributeSiteDiagnostics: true,
			ShowStaticMemberSiteDiagnostics: true
		};

		return result;
	}
}
