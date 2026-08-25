using RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Inspection;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

internal static class ArchitectureConfigurationInspectionReader
{
	internal static ArchitectureLayerInspectionResult GetLayerDetails(ArchitectureLayerEditHandle handle)
	{
		var result = ArchitectureLayerInspectionReader.GetLayerDetails(handle);

		return result;
	}

	internal static ArchitectureRootInspectionResult GetRootDetails(ArchitectureConfigurationSource source)
	{
		var result = ArchitectureRootInspectionReader.GetRootDetails(source);

		return result;
	}
}
