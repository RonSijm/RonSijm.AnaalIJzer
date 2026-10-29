using Microsoft.Extensions.Logging;

namespace RonSijm.IJzer.GraphEditor.Standalone.FileExtension;

internal static class IJzerFileAssociation
{
    private const string Extension = ".anl";
    private const string ProgramName = "RonSijm.IJzer.GraphEditor.anl";
    private const string FileTypeDescription = "IJzer architecture settings";

    internal static bool AssociateAnlFiles(ILogger logger)
    {
        logger.LogInformation("Associating {Extension} files with the IJzer Graph Editor.", Extension);
        var result = Extension.CreateFileExtensionAssociation(ProgramName, FileTypeDescription);
        logger.LogInformation("Association result for {Extension}: changed={Changed}", Extension, result);

        return result;
    }

    internal static bool UnassociateAnlFiles(ILogger logger)
    {
        logger.LogInformation("Removing {Extension} file association for the IJzer Graph Editor.", Extension);
        var result = Extension.RemoveFileExtensionAssociation(ProgramName);
        logger.LogInformation("Association removal result for {Extension}: changed={Changed}", Extension, result);

        return result;
    }
}