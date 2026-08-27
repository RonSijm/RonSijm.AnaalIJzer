using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Root;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

internal static class ArchitectureRootEditor
{
	internal static ArchitectureConfigurationDocumentOperationResult SetRootSettings(
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
		var result = ArchitectureRootSettingsEditor.SetRootSettings(
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
			exceptionPolicyDescription);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddGlobalTypePolicyMatcher(ArchitectureConfigurationSource source, string policyKind, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureRootCompositionEditor.AddGlobalTypePolicyMatcher(source, policyKind, elementKind, attributes);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddInclude(ArchitectureConfigurationSource source, string path)
	{
		var result = ArchitectureRootCompositionEditor.AddInclude(source, path);

		return result;
	}
}
