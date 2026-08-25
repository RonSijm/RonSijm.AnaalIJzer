using System.Collections.Immutable;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Dependencies;

internal static class ArchitectureDependencyRuleMutationEditor
{
	internal static ArchitectureConfigurationDocumentOperationResult RemoveDependency(ArchitectureDependencyRuleEditHandle handle)
	{
		var result = EditDependencyRule(
			handle,
			element =>
			{
				element.Remove();

				return ArchitectureConfigurationDocumentOperationResult.Success("Removed " + handle.ElementKind + " " + handle.ConfiguredFrom + " -> " + handle.ConfiguredTo + ".");
			});

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult SetDependencySites(ArchitectureDependencyRuleEditHandle handle, ArchitectureSiteFilterEditMode mode, ImmutableArray<string> sites)
	{
		var result = EditDependencyRule(
			handle,
			element =>
			{
				ArchitectureConfigurationXmlEditor.ApplySiteFilter(element, mode, sites);

				return ArchitectureConfigurationDocumentOperationResult.Success("Updated sites for " + handle.ElementKind + " " + handle.ConfiguredFrom + " -> " + handle.ConfiguredTo + ".");
			});

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult SetDependencyKind(ArchitectureDependencyRuleEditHandle handle, string elementKind)
	{
		if (elementKind is not ArchitectureConfigurationXmlNames.AllowedDependencyElementName and not ArchitectureConfigurationXmlNames.BlockedDependencyElementName)
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("Dependency kind must be AllowedDependency or BlockedDependency.");
		}

		var result = EditDependencyRule(
			handle,
			element =>
			{
				element.Name = elementKind;

				return ArchitectureConfigurationDocumentOperationResult.Success("Changed dependency rule to " + elementKind + " " + handle.ConfiguredFrom + " -> " + handle.ConfiguredTo + ".");
			});

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult SetDependencyAppliesToDescendants(ArchitectureDependencyRuleEditHandle handle, bool appliesToDescendants)
	{
		var result = EditDependencyRule(
			handle,
			element =>
			{
				element.SetAttributeValue(ArchitectureConfigurationXmlNames.AppliesToDescendantsAttributeName, appliesToDescendants ? "true" : null);

				return ArchitectureConfigurationDocumentOperationResult.Success("Updated appliesToDescendants for " + handle.ElementKind + " " + handle.ConfiguredFrom + " -> " + handle.ConfiguredTo + ".");
			});

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult SetDependencyDescription(ArchitectureDependencyRuleEditHandle handle, string? description)
	{
		var result = EditDependencyRule(
			handle,
			element =>
			{
				ArchitectureConfigurationXmlEditor.SetOptionalAttribute(element, ArchitectureConfigurationXmlNames.DescriptionAttributeName, description);

				return ArchitectureConfigurationDocumentOperationResult.Success("Updated description for " + handle.ElementKind + " " + handle.ConfiguredFrom + " -> " + handle.ConfiguredTo + ".");
			});

		return result;
	}

	private static ArchitectureConfigurationDocumentOperationResult EditDependencyRule(ArchitectureDependencyRuleEditHandle handle, Func<XElement, ArchitectureConfigurationDocumentOperationResult> edit)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("This dependency rule does not have an editable configuration origin.");
		}

		var result = ArchitectureConfigurationEditExecution.EditConfiguration(
			handle.SourceKind,
			handle.SourcePath,
			document =>
			{
				var element = ArchitectureConfigurationXmlNavigator.FindDependencyElement(document, handle);
				if (element is null)
				{
					return ArchitectureConfigurationDocumentOperationResult.Failure("Could not find the dependency rule in " + handle.SourcePath + ".");
				}

				return edit(element);
			});

		return result;
	}
}
