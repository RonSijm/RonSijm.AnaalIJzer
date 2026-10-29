using RonSijm.IJzer.ConfigurationEditing.Editing.Inspection;
using RonSijm.IJzer.ConfigurationEditing.Model;
using RonSijm.IJzer.Core.Configuration.Document.Model;

namespace RonSijm.IJzer.ConfigurationEditing.Editing;

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