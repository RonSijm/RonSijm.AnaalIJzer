using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

public static partial class ArchitectureConfigurationEditService
{
	public static ArchitectureLayerInspectionResult GetLayerDetails(ArchitectureLayerEditHandle handle)
	{
		var result = ArchitectureConfigurationInspectionReader.GetLayerDetails(handle);

		return result;
	}

	public static ArchitectureRootInspectionResult GetRootDetails(ArchitectureConfigurationSource source)
	{
		var result = ArchitectureConfigurationInspectionReader.GetRootDetails(source);

		return result;
	}

	public static ArchitectureConfigurationEditResult SetRootSettings(
		ArchitectureConfigurationSource source,
		string? description,
		string? requireRecognizedDependencies,
		bool enforceAcyclic,
		bool enableReport,
		string? reportPath,
		bool enableDocumentation,
		string? documentationPath,
		bool enableExceptionPolicy,
		bool requireExceptionReason,
		bool requireExceptionOwner,
		bool requireExceptionExpiresOn,
		int exceptionWarnBeforeDays,
		string? exceptionPolicyDescription)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(
			ArchitectureRootEditor.SetRootSettings(
				source,
				description,
				requireRecognizedDependencies,
				enforceAcyclic,
				enableReport,
				reportPath,
				enableDocumentation,
				documentationPath,
				enableExceptionPolicy,
				requireExceptionReason,
				requireExceptionOwner,
				requireExceptionExpiresOn,
				exceptionWarnBeforeDays,
				exceptionPolicyDescription));

		return result;
	}

	public static ArchitectureConfigurationEditResult SetConfigurationElementAttributes(ArchitectureConfigurationElementEditHandle handle, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureConfigurationElementEditor.SetConfigurationElementAttributes(handle, attributes));

		return result;
	}

	public static ArchitectureConfigurationEditResult SetConfigurationElementChildren(ArchitectureConfigurationElementEditHandle handle, string childXml)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureConfigurationElementEditor.SetConfigurationElementChildren(handle, childXml));

		return result;
	}

	public static ArchitectureConfigurationEditResult RemoveConfigurationElement(ArchitectureConfigurationElementEditHandle handle)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureConfigurationElementEditor.RemoveConfigurationElement(handle));

		return result;
	}

	public static ArchitectureConfigurationEditResult AddGlobalTypePolicyMatcher(ArchitectureConfigurationSource source, string policyKind, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureRootEditor.AddGlobalTypePolicyMatcher(source, policyKind, elementKind, attributes));

		return result;
	}

	public static ArchitectureConfigurationEditResult AddInclude(ArchitectureConfigurationSource source, string path)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureRootEditor.AddInclude(source, path));

		return result;
	}
}
