using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

internal static class ArchitectureConfigurationElementEditor
{
	internal static ArchitectureConfigurationEditResult SetConfigurationElementAttributes(ArchitectureConfigurationElementEditHandle handle, ImmutableDictionary<string, string> attributes)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("This configuration element does not have an editable configuration origin.");
		}

		if (!ArchitectureConfigurationXmlEditor.TryCreateAttributes(attributes, out var xAttributes, out var message))
		{
			return ArchitectureConfigurationEditResult.Failure(message);
		}

		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			handle.SourceKind,
			handle.SourcePath,
			document =>
			{
				var element = ArchitectureConfigurationXmlNavigator.FindConfigurationElement(document, handle);
				if (element is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Could not find " + handle.ElementKind + " in " + handle.SourcePath + ".");
				}

				element.RemoveAttributes();
				element.Add(xAttributes);
				return ArchitectureConfigurationEditResult.Success("Updated " + handle.ElementKind + " matcher attributes.");
			});

		return result;
	}

	internal static ArchitectureConfigurationEditResult SetConfigurationElementChildren(ArchitectureConfigurationElementEditHandle handle, string childXml)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("This configuration element does not have an editable configuration origin.");
		}

		if (!ArchitectureConfigurationXmlEditor.TryParseChildNodes(childXml, out var childNodes, out var message))
		{
			return ArchitectureConfigurationEditResult.Failure(message);
		}

		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			handle.SourceKind,
			handle.SourcePath,
			document =>
			{
				var element = ArchitectureConfigurationXmlNavigator.FindConfigurationElement(document, handle);
				if (element is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Could not find " + handle.ElementKind + " in " + handle.SourcePath + ".");
				}

				element.RemoveNodes();
				element.Add(childNodes);
				return ArchitectureConfigurationEditResult.Success("Updated child XML for " + handle.ElementKind + ".");
			});

		return result;
	}

	internal static ArchitectureConfigurationEditResult RemoveConfigurationElement(ArchitectureConfigurationElementEditHandle handle)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("This configuration element does not have an editable configuration origin.");
		}

		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			handle.SourceKind,
			handle.SourcePath,
			document =>
			{
				var element = ArchitectureConfigurationXmlNavigator.FindConfigurationElement(document, handle);
				if (element is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Could not find " + handle.ElementKind + " in " + handle.SourcePath + ".");
				}

				var parent = element.Parent;
				element.Remove();
				if (parent is not null
				    && (parent.Name.LocalName == ArchitectureConfigurationXmlNames.AllowedElementName || parent.Name.LocalName == ArchitectureConfigurationXmlNames.ForbiddenElementName)
				    && !parent.HasElements
				    && !parent.HasAttributes)
				{
					parent.Remove();
				}

				return ArchitectureConfigurationEditResult.Success("Removed " + handle.ElementKind + " matcher.");
			});

		return result;
	}

}
