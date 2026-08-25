using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.GraphApplication;

internal sealed partial class ArchitectureGraphEditService
{
	public ArchitectureLayerInspectionResult GetLayerDetails(ArchitectureLayerEditHandle handle)
	{
		var result = ArchitectureConfigurationEditService.GetLayerDetails(handle);

		return result;
	}

	public ArchitectureRootInspectionResult GetRootDetails(ArchitectureConfigurationSource source)
	{
		var result = ArchitectureConfigurationEditService.GetRootDetails(source);

		return result;
	}

	public ArchitectureConfigurationEditResult SetRootSettings(ArchitectureConfigurationSource source, string? description, string? requireRecognizedDependencies, bool enforceAcyclic, bool enableReport, string? reportPath, bool enableDocumentation, string? documentationPath, bool enableExceptionPolicy, bool requireExceptionReason, bool requireExceptionOwner, bool requireExceptionExpiresOn, int exceptionWarnBeforeDays, string? exceptionPolicyDescription)
	{
		var result = ArchitectureConfigurationEditService.SetRootSettings(source, description, requireRecognizedDependencies, enforceAcyclic, enableReport, reportPath, enableDocumentation, documentationPath, enableExceptionPolicy, requireExceptionReason, requireExceptionOwner, requireExceptionExpiresOn, exceptionWarnBeforeDays, exceptionPolicyDescription);

		return result;
	}

	public ArchitectureConfigurationEditResult SetConfigurationElementAttributes(ArchitectureConfigurationElementEditHandle handle, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureConfigurationEditService.SetConfigurationElementAttributes(handle, attributes);

		return result;
	}

	public ArchitectureConfigurationEditResult SetConfigurationElementChildren(ArchitectureConfigurationElementEditHandle handle, string childXml)
	{
		var result = ArchitectureConfigurationEditService.SetConfigurationElementChildren(handle, childXml);

		return result;
	}

	public ArchitectureConfigurationEditResult RemoveConfigurationElement(ArchitectureConfigurationElementEditHandle handle)
	{
		var result = ArchitectureConfigurationEditService.RemoveConfigurationElement(handle);

		return result;
	}

	public ArchitectureConfigurationEditResult AddGlobalTypePolicyMatcher(ArchitectureConfigurationSource source, string policyKind, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureConfigurationEditService.AddGlobalTypePolicyMatcher(source, policyKind, elementKind, attributes);

		return result;
	}

	public ArchitectureConfigurationEditResult AddInclude(ArchitectureConfigurationSource source, string path)
	{
		var result = ArchitectureConfigurationEditService.AddInclude(source, path);

		return result;
	}
}
