using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Root;

internal static class ArchitectureRootSettingsEditor
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
		if (!source.CanEdit)
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("This configuration source is not editable.");
		}

		var result = ArchitectureConfigurationEditExecution.EditConfiguration(
			source.Kind,
			source.Path,
			document =>
			{
				if (document.Root is null)
				{
					return ArchitectureConfigurationDocumentOperationResult.Failure("Architecture configuration has no root element.");
				}

				ArchitectureConfigurationXmlEditor.SetOptionalAttribute(document.Root, ArchitectureConfigurationXmlNames.DescriptionAttributeName, description);
				ArchitectureConfigurationXmlEditor.SetOptionalAttribute(document.Root, ArchitectureConfigurationXmlNames.RequireRecognizedDependenciesAttributeName, requireRecognizedDependencies);
				ArchitectureConfigurationXmlEditor.SetOptionalBooleanAttribute(document.Root, "enforceAcyclic", enforceAcyclic);
				ArchitectureConfigurationXmlEditor.SetOptionalBooleanAttribute(document.Root, "enableReport", enableReport);
				ArchitectureConfigurationXmlEditor.SetOptionalAttribute(document.Root, "reportPath", reportPath);
				ArchitectureConfigurationXmlEditor.SetOptionalBooleanAttribute(document.Root, "enableDocumentation", enableDocumentation);
				ArchitectureConfigurationXmlEditor.SetOptionalAttribute(document.Root, "documentationPath", documentationPath);
				SetExceptionPolicy(
					document.Root,
					enableExceptionPolicy,
					requireExceptionReason,
					requireExceptionOwner,
					requireExceptionExpiresOn,
					exceptionWarnBeforeDays,
					exceptionPolicyDescription);

				return ArchitectureConfigurationDocumentOperationResult.Success("Updated root architecture settings.");
			});

		return result;
	}

	private static void SetExceptionPolicy(
		XElement root,
		bool enableExceptionPolicy,
		bool requireExceptionReason,
		bool requireExceptionOwner,
		bool requireExceptionExpiresOn,
		int exceptionWarnBeforeDays,
		string? exceptionPolicyDescription)
	{
		var existing = root.Element(ArchitectureConfigurationXmlNames.ExceptionPolicyElementName);
		if (!enableExceptionPolicy)
		{
			existing?.Remove();
			return;
		}

		var policy = existing ?? new XElement(ArchitectureConfigurationXmlNames.ExceptionPolicyElementName);
		ArchitectureConfigurationXmlEditor.SetOptionalBooleanAttribute(policy, ArchitectureConfigurationXmlNames.RequireExceptionReasonAttributeName, requireExceptionReason);
		ArchitectureConfigurationXmlEditor.SetOptionalBooleanAttribute(policy, ArchitectureConfigurationXmlNames.RequireExceptionOwnerAttributeName, requireExceptionOwner);
		ArchitectureConfigurationXmlEditor.SetOptionalBooleanAttribute(policy, ArchitectureConfigurationXmlNames.RequireExceptionExpiresOnAttributeName, requireExceptionExpiresOn);
		policy.SetAttributeValue(ArchitectureConfigurationXmlNames.ExceptionWarnBeforeDaysAttributeName, exceptionWarnBeforeDays != 14 ? exceptionWarnBeforeDays.ToString() : null);
		ArchitectureConfigurationXmlEditor.SetOptionalAttribute(policy, ArchitectureConfigurationXmlNames.DescriptionAttributeName, exceptionPolicyDescription);
		if (existing is null)
		{
			root.AddFirst(policy);
		}
	}
}
