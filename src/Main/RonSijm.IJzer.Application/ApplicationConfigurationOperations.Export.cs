using RonSijm.IJzer.ConfigurationEditing.Document;
using RonSijm.IJzer.Core.Configuration.Document.Documents;

namespace RonSijm.IJzer.Application;

internal static partial class ApplicationConfigurationOperations
{
    public static async Task<ApplicationRunResult> ExportConfigAsync(ApplicationRequest request, ApplicationWorkspaceAnalysisService workspace, CancellationToken cancellationToken)
    {
        var result = await workspace.AnalyzeProjectAsync(request, cancellationToken);
        if (string.IsNullOrWhiteSpace(result.InlineConfigXml))
        {
            throw new ApplicationOperationException("No AssemblyMetadata(\"IJzerSettings\", ...) value was found. There is no inline config to export.");
        }

        var outputPath = ApplicationOutputPathService.ResolveOutputPath(request.OutputPath, Path.Combine(result.ProjectDirectory, ArchitectureConfigurationDocumentLoader.ConfigFileName), result.ProjectDirectory);
        await ApplicationOutputPathService.WriteOutputAsync(outputPath, ApplicationOutputPathService.EnsureFinalNewLine(result.InlineConfigXml), request.Force, cancellationToken);
        var toolRunResult = new ApplicationRunResult(outputPath, $"Wrote {outputPath}");

        return toolRunResult;
    }
}