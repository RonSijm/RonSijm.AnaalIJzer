using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

internal static class ArchitectureConfigurationInspectionReader
{
	internal static ArchitectureLayerInspectionResult GetLayerDetails(ArchitectureLayerEditHandle handle)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureLayerInspectionResult.Failure("This layer does not have an editable configuration origin.");
		}

		var readResult = ArchitectureConfigurationDocumentStore.ReadConfiguration(handle.SourceKind, handle.SourcePath, out var document);
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
		var result = ArchitectureLayerInspectionResult.Success(
			element.Attribute("name")?.Value ?? handle.ConfiguredName,
			element.Attribute(ArchitectureConfigurationXmlNames.DescriptionAttributeName)?.Value,
			element.Attribute(ArchitectureConfigurationXmlNames.RequireRecognizedDependenciesAttributeName)?.Value,
			matchers,
			allowedPolicies,
			forbiddenPolicies);

		return result;
	}

	internal static ArchitectureRootInspectionResult GetRootDetails(ArchitectureConfigurationSource source)
	{
		if (!source.CanEdit)
		{
			return ArchitectureRootInspectionResult.Failure("This configuration source is not editable.");
		}

		var readResult = ArchitectureConfigurationDocumentStore.ReadConfiguration(source.Kind, source.Path, out var document);
		if (!readResult.Succeeded || document?.Root is null)
		{
			return ArchitectureRootInspectionResult.Failure(readResult.Message);
		}

		var root = document.Root;
		var rootHandle = new ArchitectureLayerEditHandle(source.Kind, source.Path, 0, string.Empty, string.Empty, string.Empty, null);
		var includes = root
			.Elements(ArchitectureConfigurationXmlNames.IncludeElementName)
			.Select(child => ArchitectureConfigurationXmlEditor.CreateElementDetails(child, rootHandle, ArchitectureConfigurationXmlNames.IncludeElementName))
			.ToImmutableArray();
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
			includes,
			allowedPolicies,
			forbiddenPolicies);

		return result;
	}

}
