using System.Xml.Linq;
using RonSijm.IJzer.ConfigurationEditing.Model;
using RonSijm.IJzer.ConfigurationEditing.Editing.Xml;
using RonSijm.IJzer.Core.Configuration.Document.Documents;

namespace RonSijm.IJzer.ConfigurationEditing.Editing.Layers;

internal static class ArchitectureLayerMutationExecutor
{
    internal static ArchitectureConfigurationDocumentOperationResult EditLayer(ArchitectureLayerEditHandle handle, Func<XDocument, XElement, ArchitectureConfigurationDocumentOperationResult> mutation)
    {
        if (!handle.CanEdit)
        {
            return ArchitectureConfigurationDocumentOperationResult.Failure("This layer does not have an editable configuration origin.");
        }

        var result = ArchitectureConfigurationEditExecution.EditConfiguration(
            handle.SourceKind,
            handle.SourcePath,
            document =>
            {
                var layer = ArchitectureConfigurationXmlNavigator.FindLayerElement(document, handle);
                if (layer is null)
                {
                    return ArchitectureConfigurationDocumentOperationResult.Failure("Could not find layer '" + handle.LayerPath + "' in " + handle.SourcePath + ".");
                }

                var mutationResult = mutation(document, layer);

                return mutationResult;
            });

        return result;
    }
}