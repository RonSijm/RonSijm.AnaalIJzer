using System.Collections.Immutable;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

internal static class ArchitectureDependencyRuleEditor
{
	internal static ArchitectureConfigurationEditResult RemoveDependency(ArchitectureDependencyRuleEditHandle handle)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("This dependency rule does not have an editable configuration origin.");
		}

		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			handle.SourceKind,
			handle.SourcePath,
			document =>
			{
				var element = ArchitectureConfigurationXmlNavigator.FindDependencyElement(document, handle);
				if (element is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Could not find the dependency rule in " + handle.SourcePath + ".");
				}

				element.Remove();
				return ArchitectureConfigurationEditResult.Success("Removed " + handle.ElementKind + " " + handle.ConfiguredFrom + " -> " + handle.ConfiguredTo + ".");
			});

		return result;
	}

	internal static ArchitectureConfigurationEditResult SetDependencySites(ArchitectureDependencyRuleEditHandle handle, ArchitectureSiteFilterEditMode mode, ImmutableArray<string> sites)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("This dependency rule does not have an editable configuration origin.");
		}

		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			handle.SourceKind,
			handle.SourcePath,
			document =>
			{
				var element = ArchitectureConfigurationXmlNavigator.FindDependencyElement(document, handle);
				if (element is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Could not find the dependency rule in " + handle.SourcePath + ".");
				}

				ArchitectureConfigurationXmlEditor.ApplySiteFilter(element, mode, sites);
				return ArchitectureConfigurationEditResult.Success("Updated sites for " + handle.ElementKind + " " + handle.ConfiguredFrom + " -> " + handle.ConfiguredTo + ".");
			});

		return result;
	}

	internal static ArchitectureConfigurationEditResult SetDependencyKind(ArchitectureDependencyRuleEditHandle handle, string elementKind)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("This dependency rule does not have an editable configuration origin.");
		}

		if (elementKind is not ArchitectureConfigurationXmlNames.AllowedDependencyElementName and not ArchitectureConfigurationXmlNames.BlockedDependencyElementName)
		{
			return ArchitectureConfigurationEditResult.Failure("Dependency kind must be AllowedDependency or BlockedDependency.");
		}

		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			handle.SourceKind,
			handle.SourcePath,
			document =>
			{
				var element = ArchitectureConfigurationXmlNavigator.FindDependencyElement(document, handle);
				if (element is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Could not find the dependency rule in " + handle.SourcePath + ".");
				}

				element.Name = elementKind;
				return ArchitectureConfigurationEditResult.Success("Changed dependency rule to " + elementKind + " " + handle.ConfiguredFrom + " -> " + handle.ConfiguredTo + ".");
			});

		return result;
	}

	internal static ArchitectureConfigurationEditResult SetDependencyAppliesToDescendants(ArchitectureDependencyRuleEditHandle handle, bool appliesToDescendants)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("This dependency rule does not have an editable configuration origin.");
		}

		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			handle.SourceKind,
			handle.SourcePath,
			document =>
			{
				var element = ArchitectureConfigurationXmlNavigator.FindDependencyElement(document, handle);
				if (element is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Could not find the dependency rule in " + handle.SourcePath + ".");
				}

				element.SetAttributeValue(ArchitectureConfigurationXmlNames.AppliesToDescendantsAttributeName, appliesToDescendants ? "true" : null);
				return ArchitectureConfigurationEditResult.Success("Updated appliesToDescendants for " + handle.ElementKind + " " + handle.ConfiguredFrom + " -> " + handle.ConfiguredTo + ".");
			});

		return result;
	}

	internal static ArchitectureConfigurationEditResult SetDependencyDescription(ArchitectureDependencyRuleEditHandle handle, string? description)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("This dependency rule does not have an editable configuration origin.");
		}

		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			handle.SourceKind,
			handle.SourcePath,
			document =>
			{
				var element = ArchitectureConfigurationXmlNavigator.FindDependencyElement(document, handle);
				if (element is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Could not find the dependency rule in " + handle.SourcePath + ".");
				}

				ArchitectureConfigurationXmlEditor.SetOptionalAttribute(element, ArchitectureConfigurationXmlNames.DescriptionAttributeName, description);
				return ArchitectureConfigurationEditResult.Success("Updated description for " + handle.ElementKind + " " + handle.ConfiguredFrom + " -> " + handle.ConfiguredTo + ".");
			});

		return result;
	}

	internal static ArchitectureConfigurationEditResult AddAllowedDependency(ArchitectureConfigurationSource source, string from, string to)
	{
		var result = AddDependencyCore(source, from, to, ArchitectureConfigurationXmlNames.AllowedDependencyElementName);

		return result;
	}

	internal static ArchitectureConfigurationEditResult AddDependency(ArchitectureConfigurationSource source, string from, string to, string elementKind)
	{
		if (elementKind is not ArchitectureConfigurationXmlNames.AllowedDependencyElementName and not ArchitectureConfigurationXmlNames.BlockedDependencyElementName)
		{
			return ArchitectureConfigurationEditResult.Failure("Dependency kind must be AllowedDependency or BlockedDependency.");
		}

		var result = AddDependencyCore(source, from, to, elementKind);

		return result;
	}

	internal static ArchitectureConfigurationEditResult AddDependencyCore(ArchitectureConfigurationSource source, string from, string to, string elementKind)
	{
		if (!source.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("The current graph does not have an editable configuration source.");
		}

		var insertion = ArchitectureDependencyInsertionPlanner.CreateDependencyInsertion(from, to);
		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			source.Kind,
			source.Path,
			document =>
			{
				var container = ArchitectureConfigurationXmlNavigator.FindDependencyInsertionContainer(document, insertion.ScopePath);
				if (container is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Could not find dependency rule scope '" + ArchitectureConfigurationLayerPaths.FormatScopeName(insertion.ScopePath) + "' in the architecture configuration.");
				}

				if (ArchitectureConfigurationXmlNavigator.HasMatchingDependency(container, elementKind, insertion.ConfiguredFrom, insertion.ConfiguredTo))
				{
					return ArchitectureConfigurationEditResult.Success(elementKind + " " + insertion.ConfiguredFrom + " -> " + insertion.ConfiguredTo + " already exists in " + ArchitectureConfigurationLayerPaths.FormatScopeName(insertion.ScopePath) + ".");
				}

				container.Add(new XElement(
					elementKind,
					new XAttribute("from", insertion.ConfiguredFrom),
					new XAttribute("to", insertion.ConfiguredTo)));

				return ArchitectureConfigurationEditResult.Success("Added " + elementKind + " " + insertion.ConfiguredFrom + " -> " + insertion.ConfiguredTo + " in " + ArchitectureConfigurationLayerPaths.FormatScopeName(insertion.ScopePath) + ".");
			});

		return result;
	}
}
