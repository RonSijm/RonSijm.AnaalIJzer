using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Dependencies;

internal static class ArchitectureDependencyRuleCreationEditor
{
	internal static ArchitectureConfigurationDocumentOperationResult AddAllowedDependency(ArchitectureConfigurationSource source, string from, string to)
	{
		var result = AddDependencyCore(source, from, to, ArchitectureConfigurationXmlNames.AllowedDependencyElementName);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddDependency(ArchitectureConfigurationSource source, string from, string to, string elementKind)
	{
		if (elementKind is not ArchitectureConfigurationXmlNames.AllowedDependencyElementName and not ArchitectureConfigurationXmlNames.BlockedDependencyElementName)
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("Dependency kind must be AllowedDependency or BlockedDependency.");
		}

		var result = AddDependencyCore(source, from, to, elementKind);

		return result;
	}

	private static ArchitectureConfigurationDocumentOperationResult AddDependencyCore(ArchitectureConfigurationSource source, string from, string to, string elementKind)
	{
		if (!source.CanEdit)
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("The current graph does not have an editable configuration source.");
		}

		var insertion = ArchitectureDependencyInsertionPlanner.CreateDependencyInsertion(from, to);
		var result = ArchitectureConfigurationEditExecution.EditConfiguration(
			source.Kind,
			source.Path,
			document =>
			{
				var container = ArchitectureConfigurationXmlNavigator.FindDependencyInsertionContainer(document, insertion.ScopePath);
				if (container is null)
				{
					return ArchitectureConfigurationDocumentOperationResult.Failure("Could not find dependency rule scope '" + ArchitectureConfigurationLayerPaths.FormatScopeName(insertion.ScopePath) + "' in the architecture configuration.");
				}

				if (ArchitectureConfigurationXmlNavigator.HasMatchingDependency(container, elementKind, insertion.ConfiguredFrom, insertion.ConfiguredTo))
				{
					return ArchitectureConfigurationDocumentOperationResult.Success(elementKind + " " + insertion.ConfiguredFrom + " -> " + insertion.ConfiguredTo + " already exists in " + ArchitectureConfigurationLayerPaths.FormatScopeName(insertion.ScopePath) + ".");
				}

				container.Add(new XElement(
					elementKind,
					new XAttribute("from", insertion.ConfiguredFrom),
					new XAttribute("to", insertion.ConfiguredTo)));

				return ArchitectureConfigurationDocumentOperationResult.Success("Added " + elementKind + " " + insertion.ConfiguredFrom + " -> " + insertion.ConfiguredTo + " in " + ArchitectureConfigurationLayerPaths.FormatScopeName(insertion.ScopePath) + ".");
			});

		return result;
	}
}
