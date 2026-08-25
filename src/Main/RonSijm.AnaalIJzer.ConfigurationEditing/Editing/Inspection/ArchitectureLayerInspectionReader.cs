using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Inspection;

internal static class ArchitectureLayerInspectionReader
{
	internal static ArchitectureLayerInspectionResult GetLayerDetails(ArchitectureLayerEditHandle handle)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureLayerInspectionResult.Failure("This layer does not have an editable configuration origin.");
		}

		var readResult = ArchitectureConfigurationEditResult.FromDocumentResult(
			ArchitectureConfigurationDocumentPersistence.ReadConfiguration(handle.SourceKind, handle.SourcePath, out var document));
		if (!readResult.Succeeded || document is null)
		{
			return ArchitectureLayerInspectionResult.Failure(readResult.Message);
		}

		var element = ArchitectureConfigurationXmlNavigator.FindLayerElement(document, handle);
		if (element is null)
		{
			return ArchitectureLayerInspectionResult.Failure("Could not find layer '" + handle.LayerPath + "' in " + handle.SourcePath + ".");
		}

		var matchers = element
			.Elements()
			.Where(ArchitectureConfigurationXmlEditor.IsMatcherElement)
			.Select(child => ArchitectureConfigurationXmlEditor.CreateElementDetails(child, handle, "LayerMatcher"))
			.ToImmutableArray();
		var exceptionMatchers = ArchitectureExceptionMatcherInspectionReader.GetExceptionMatchersForLayer(element, handle);
		var allowedPolicies = element
			.Elements(ArchitectureConfigurationXmlNames.AllowedElementName)
			.SelectMany(container => container.Elements().Where(ArchitectureConfigurationXmlEditor.IsPolicyMatcherElement))
			.Select(child => ArchitectureConfigurationXmlEditor.CreateElementDetails(child, handle, ArchitectureConfigurationXmlNames.AllowedElementName))
			.ToImmutableArray();
		var forbiddenPolicies = element
			.Elements(ArchitectureConfigurationXmlNames.ForbiddenElementName)
			.SelectMany(container => container.Elements().Where(ArchitectureConfigurationXmlEditor.IsPolicyMatcherElement))
			.Select(child => ArchitectureConfigurationXmlEditor.CreateElementDetails(child, handle, ArchitectureConfigurationXmlNames.ForbiddenElementName))
			.ToImmutableArray();
		var nameRules = element
			.Elements(ArchitectureConfigurationXmlNames.NameRulesElementName)
			.SelectMany(container => container.Elements().Where(ArchitectureConfigurationXmlEditor.IsNameRuleElement))
			.Select(child => ArchitectureConfigurationXmlEditor.CreateElementDetails(child, handle, ArchitectureConfigurationXmlNames.NameRulesElementName))
			.ToImmutableArray();
		var inheritancePolicies = element
			.Elements(ArchitectureConfigurationXmlNames.InheritancePolicyElementName)
			.Select(child => ArchitectureConfigurationXmlEditor.CreateElementDetails(child, handle, ArchitectureConfigurationXmlNames.InheritancePolicyElementName))
			.ToImmutableArray();
		var visibilityPolicies = element
			.Elements(ArchitectureConfigurationXmlNames.VisibilityPolicyElementName)
			.Select(child => ArchitectureConfigurationXmlEditor.CreateElementDetails(child, handle, ArchitectureConfigurationXmlNames.VisibilityPolicyElementName))
			.ToImmutableArray();
		var apiSurfacePolicies = element
			.Elements(ArchitectureConfigurationXmlNames.ApiSurfaceElementName)
			.Select(child => ArchitectureConfigurationXmlEditor.CreateElementDetails(child, handle, ArchitectureConfigurationXmlNames.ApiSurfaceElementName))
			.ToImmutableArray();
		var result = ArchitectureLayerInspectionResult.Success(
			element.Attribute("name")?.Value ?? handle.ConfiguredName,
			element.Attribute(ArchitectureConfigurationXmlNames.DescriptionAttributeName)?.Value,
			element.Attribute(ArchitectureConfigurationXmlNames.RequireRecognizedDependenciesAttributeName)?.Value,
			matchers,
			exceptionMatchers,
			allowedPolicies,
			forbiddenPolicies,
			nameRules,
			inheritancePolicies,
			visibilityPolicies,
			apiSurfacePolicies);

		return result;
	}
}
