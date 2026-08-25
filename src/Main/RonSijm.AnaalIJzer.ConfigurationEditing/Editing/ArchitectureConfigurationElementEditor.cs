using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

internal static class ArchitectureConfigurationElementEditor
{
	internal static ArchitectureConfigurationDocumentOperationResult SetConfigurationElementAttributes(ArchitectureConfigurationElementEditHandle handle, ImmutableDictionary<string, string> attributes)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("This configuration element does not have an editable configuration origin.");
		}

		if (!ArchitectureConfigurationXmlEditor.TryCreateAttributes(attributes, out var xAttributes, out var message))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure(message);
		}

		var result = ArchitectureConfigurationEditExecution.EditConfiguration(
			handle.SourceKind,
			handle.SourcePath,
			document =>
			{
				var element = ArchitectureConfigurationXmlNavigator.FindConfigurationElement(document, handle);
				if (element is null)
				{
					return ArchitectureConfigurationDocumentOperationResult.Failure("Could not find " + handle.ElementKind + " in " + handle.SourcePath + ".");
				}

				element.RemoveAttributes();
				element.Add(xAttributes);
				return ArchitectureConfigurationDocumentOperationResult.Success("Updated " + handle.ElementKind + " matcher attributes.");
			});

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult SetConfigurationElementChildren(ArchitectureConfigurationElementEditHandle handle, string childXml)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("This configuration element does not have an editable configuration origin.");
		}

		if (!ArchitectureConfigurationXmlEditor.TryParseChildNodes(childXml, out var childNodes, out var message))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure(message);
		}

		var result = ArchitectureConfigurationEditExecution.EditConfiguration(
			handle.SourceKind,
			handle.SourcePath,
			document =>
			{
				var element = ArchitectureConfigurationXmlNavigator.FindConfigurationElement(document, handle);
				if (element is null)
				{
					return ArchitectureConfigurationDocumentOperationResult.Failure("Could not find " + handle.ElementKind + " in " + handle.SourcePath + ".");
				}

				element.RemoveNodes();
				element.Add(childNodes);
				return ArchitectureConfigurationDocumentOperationResult.Success("Updated child XML for " + handle.ElementKind + ".");
			});

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult RemoveConfigurationElement(ArchitectureConfigurationElementEditHandle handle)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("This configuration element does not have an editable configuration origin.");
		}

		var result = ArchitectureConfigurationEditExecution.EditConfiguration(
			handle.SourceKind,
			handle.SourcePath,
			document =>
			{
				var element = ArchitectureConfigurationXmlNavigator.FindConfigurationElement(document, handle);
				if (element is null)
				{
					return ArchitectureConfigurationDocumentOperationResult.Failure("Could not find " + handle.ElementKind + " in " + handle.SourcePath + ".");
				}

				var parent = element.Parent;
				element.Remove();
				if (parent is not null
				    && (parent.Name.LocalName == ArchitectureConfigurationXmlNames.AllowedElementName
				        || parent.Name.LocalName == ArchitectureConfigurationXmlNames.ForbiddenElementName
				        || parent.Name.LocalName == ArchitectureConfigurationXmlNames.NameRulesElementName
				        || parent.Name.LocalName == ArchitectureConfigurationXmlNames.ExceptionsElementName)
				    && !parent.HasElements
				    && !parent.HasAttributes)
				{
					parent.Remove();
				}

				return ArchitectureConfigurationDocumentOperationResult.Success("Removed " + handle.ElementKind + " matcher.");
			});

		return result;
	}

}
