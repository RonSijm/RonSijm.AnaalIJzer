using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Inspection;

internal static class ArchitectureRootInspectionReader
{
	internal static ArchitectureRootInspectionResult GetRootDetails(ArchitectureConfigurationSource source)
	{
		if (!source.CanEdit)
		{
			return ArchitectureRootInspectionResult.Failure("This configuration source is not editable.");
		}

		var readResult = ArchitectureConfigurationEditResult.FromDocumentResult(
			ArchitectureConfigurationDocumentPersistence.ReadConfiguration(source, out var document));
		if (!readResult.Succeeded || document?.Root is null)
		{
			return ArchitectureRootInspectionResult.Failure(readResult.Message);
		}

		var root = document.Root;
		var rootHandle = new ArchitectureLayerEditHandle(source.Kind, source.Path, 0, string.Empty, string.Empty, string.Empty, null);
		var exceptionPolicy = root.Element(ArchitectureConfigurationXmlNames.ExceptionPolicyElementName);
		var includes = root
			.Elements(ArchitectureConfigurationXmlNames.IncludeElementName)
			.Select(child => ArchitectureConfigurationXmlEditor.CreateElementDetails(child, rootHandle, ArchitectureConfigurationXmlNames.IncludeElementName))
			.ToImmutableArray();
		var exceptionMatchers = ArchitectureExceptionMatcherInspectionReader.GetExceptionMatchersForRoot(root, rootHandle);
		var allowedPolicies = root
			.Elements(ArchitectureConfigurationXmlNames.AllowedElementName)
			.SelectMany(container => container.Elements().Where(ArchitectureConfigurationXmlEditor.IsPolicyMatcherElement))
			.Select(child => ArchitectureConfigurationXmlEditor.CreateElementDetails(child, rootHandle, ArchitectureConfigurationXmlNames.AllowedElementName))
			.ToImmutableArray();
		var forbiddenPolicies = root
			.Elements(ArchitectureConfigurationXmlNames.ForbiddenElementName)
			.SelectMany(container => container.Elements().Where(ArchitectureConfigurationXmlEditor.IsPolicyMatcherElement))
			.Select(child => ArchitectureConfigurationXmlEditor.CreateElementDetails(child, rootHandle, ArchitectureConfigurationXmlNames.ForbiddenElementName))
			.ToImmutableArray();
		var result = ArchitectureRootInspectionResult.Success(
			root.Attribute(ArchitectureConfigurationXmlNames.DescriptionAttributeName)?.Value,
			root.Attribute(ArchitectureConfigurationXmlNames.RequireRecognizedDependenciesAttributeName)?.Value,
			ArchitectureConfigurationXmlEditor.ReadBooleanAttribute(root, "enforceAcyclic"),
			ArchitectureConfigurationXmlEditor.ReadBooleanAttribute(root, "enableReport"),
			root.Attribute("reportPath")?.Value,
			ArchitectureConfigurationXmlEditor.ReadBooleanAttribute(root, "enableDocumentation"),
			root.Attribute("documentationPath")?.Value,
			exceptionPolicy is not null,
			exceptionPolicy is not null && ArchitectureConfigurationXmlEditor.ReadBooleanAttribute(exceptionPolicy, ArchitectureConfigurationXmlNames.RequireExceptionReasonAttributeName),
			exceptionPolicy is not null && ArchitectureConfigurationXmlEditor.ReadBooleanAttribute(exceptionPolicy, ArchitectureConfigurationXmlNames.RequireExceptionOwnerAttributeName),
			exceptionPolicy is not null && ArchitectureConfigurationXmlEditor.ReadBooleanAttribute(exceptionPolicy, ArchitectureConfigurationXmlNames.RequireExceptionExpiresOnAttributeName),
			exceptionPolicy is null ? 14 : ArchitectureConfigurationXmlEditor.ReadIntegerAttribute(exceptionPolicy, ArchitectureConfigurationXmlNames.ExceptionWarnBeforeDaysAttributeName, 14),
			exceptionPolicy?.Attribute(ArchitectureConfigurationXmlNames.DescriptionAttributeName)?.Value,
			includes,
			exceptionMatchers,
			allowedPolicies,
			forbiddenPolicies);

		return result;
	}
}
