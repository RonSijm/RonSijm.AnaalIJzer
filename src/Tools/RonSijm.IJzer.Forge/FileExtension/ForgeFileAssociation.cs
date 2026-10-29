namespace RonSijm.IJzer.Forge.FileExtension;

internal static class ForgeFileAssociation
{
    private const string Extension = ".anl";
    private const string ProgramName = "RonSijm.IJzer.Forge.anl";
    private const string FileTypeDescription = "IJzer architecture settings";
    private const string OpenCommand = "inspect --config \"%1\"";

    internal static FileAssociationResult AssociateAnlFiles()
    {
        var changed = Extension.CreateFileExtensionAssociation(ProgramName, FileTypeDescription, OpenCommand);
        var message = changed
            ? ".anl files are now associated with IJzer Forge."
            : ".anl files were already associated with IJzer Forge.";
        var result = new FileAssociationResult(changed, message);

        return result;
    }

    internal static FileAssociationResult UnassociateAnlFiles()
    {
        var changed = Extension.RemoveFileExtensionAssociation(ProgramName);
        var message = changed
            ? ".anl files are no longer associated with IJzer Forge."
            : ".anl files were not associated with IJzer Forge.";
        var result = new FileAssociationResult(changed, message);

        return result;
    }
}

internal sealed record FileAssociationResult(bool Changed, string Message);