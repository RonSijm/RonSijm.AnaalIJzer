namespace RonSijm.AnaalIJzer.Arse.FileExtension;

internal static class ArseFileAssociation
{
	private const string Extension = ".anl";
	private const string ProgramName = "RonSijm.AnaalIJzer.Arse.anl";
	private const string FileTypeDescription = "AnaalIJzer architecture settings";
	private const string OpenCommand = "inspect --config \"%1\"";

	internal static FileAssociationResult AssociateAnlFiles()
	{
		var changed = Extension.CreateFileExtensionAssociation(ProgramName, FileTypeDescription, OpenCommand);
		var message = changed
			? ".anl files are now associated with Arse."
			: ".anl files were already associated with Arse.";
		var result = new FileAssociationResult(changed, message);

		return result;
	}

	internal static FileAssociationResult UnassociateAnlFiles()
	{
		var changed = Extension.RemoveFileExtensionAssociation(ProgramName);
		var message = changed
			? ".anl files are no longer associated with Arse."
			: ".anl files were not associated with Arse.";
		var result = new FileAssociationResult(changed, message);

		return result;
	}
}

internal sealed record FileAssociationResult(bool Changed, string Message);
