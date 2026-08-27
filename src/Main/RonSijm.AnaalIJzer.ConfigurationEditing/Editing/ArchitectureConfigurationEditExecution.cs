using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

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
