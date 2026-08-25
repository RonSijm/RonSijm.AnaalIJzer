using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

internal static class ArchitectureConfigurationEditExecution
{
	internal static ArchitectureConfigurationDocumentOperationResult EditConfiguration(
		ArchitectureConfigurationSourceKind sourceKind,
		string sourcePath,
		Func<XDocument, ArchitectureConfigurationDocumentOperationResult> edit)
	{
		var result = ArchitectureConfigurationDocumentPersistence.EditConfiguration(
			sourceKind,
			sourcePath,
			edit);

		return result;
	}
}
