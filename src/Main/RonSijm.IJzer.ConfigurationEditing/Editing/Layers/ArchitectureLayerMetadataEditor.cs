using RonSijm.IJzer.ConfigurationEditing.Document;
using RonSijm.IJzer.ConfigurationEditing.Model;
using RonSijm.IJzer.ConfigurationEditing.Editing.Xml;
using RonSijm.IJzer.Core.Configuration.Document.Documents;

namespace RonSijm.IJzer.ConfigurationEditing.Editing.Layers;

internal static class ArchitectureLayerMetadataEditor
{
    internal static ArchitectureConfigurationDocumentOperationResult SetLayerDescription(ArchitectureLayerEditHandle handle, string? description)
    {
        var result = ArchitectureLayerMutationExecutor.EditLayer(
            handle,
            (_, layer) =>
            {
                ArchitectureConfigurationXmlEditor.SetOptionalAttribute(layer, ArchitectureConfigurationXmlNames.DescriptionAttributeName, description);
                var editResult = ArchitectureConfigurationDocumentOperationResult.Success("Updated description for layer " + handle.LayerPath + ".");

                return editResult;
            });

        return result;
    }

    internal static ArchitectureConfigurationDocumentOperationResult SetLayerRequireRecognizedDependencies(ArchitectureLayerEditHandle handle, string? sites)
    {
        var result = ArchitectureLayerMutationExecutor.EditLayer(
            handle,
            (_, layer) =>
            {
                ArchitectureConfigurationXmlEditor.SetOptionalAttribute(layer, ArchitectureConfigurationXmlNames.RequireRecognizedDependenciesAttributeName, sites);
                var editResult = ArchitectureConfigurationDocumentOperationResult.Success("Updated requireRecognizedDependencies for layer " + handle.LayerPath + ".");

                return editResult;
            });

        return result;
    }
}