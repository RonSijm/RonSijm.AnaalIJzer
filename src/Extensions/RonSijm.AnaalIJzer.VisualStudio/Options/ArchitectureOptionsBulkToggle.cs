using RonSijm.AnaalIJzer.Indicators;

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
		var result = options.ShowConstructorLayerInformation
		             && options.ShowMethodLayerInformation
		             && options.ShowMethodReturnLayerInformation
		             && options.ShowFieldLayerInformation
		             && options.ShowPropertyLayerInformation
		             && options.ShowLocalLayerInformation
		             && options.ShowNewLayerInformation
		             && options.ShowGenericInvocationLayerInformation
		             && options.ShowGenericArgumentLayerInformation
		             && options.ShowInheritanceLayerInformation
		             && options.ShowInterfaceImplementationLayerInformation
		             && options.ShowAttributeLayerInformation
		             && options.ShowStaticMemberLayerInformation;

		return result;
	}

	internal static bool AreAllSiteDiagnosticOptionsEnabled(ArchitectureSiteDiagnosticOptions options)
	{
		var result = options.ShowConstructorSiteDiagnostics
		             && options.ShowMethodSiteDiagnostics
		             && options.ShowMethodReturnSiteDiagnostics
		             && options.ShowFieldSiteDiagnostics
		             && options.ShowPropertySiteDiagnostics
		             && options.ShowLocalSiteDiagnostics
		             && options.ShowNewSiteDiagnostics
		             && options.ShowGenericInvocationSiteDiagnostics
		             && options.ShowGenericArgumentSiteDiagnostics
		             && options.ShowInheritanceSiteDiagnostics
		             && options.ShowInterfaceImplementationSiteDiagnostics
		             && options.ShowAttributeSiteDiagnostics
		             && options.ShowStaticMemberSiteDiagnostics;

		return result;
	}
}
