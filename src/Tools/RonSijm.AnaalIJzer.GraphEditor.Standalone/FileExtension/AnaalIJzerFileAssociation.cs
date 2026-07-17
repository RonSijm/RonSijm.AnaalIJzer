using Microsoft.Extensions.Logging;

namespace RonSijm.AnaalIJzer.GraphEditor.Standalone.FileExtension;

internal static class AnaalIJzerFileAssociation
{
	private const string Extension = ".anl";
	private const string ProgramName = "RonSijm.AnaalIJzer.GraphEditor.anl";
	private const string FileTypeDescription = "AnaalIJzer architecture settings";

	internal static bool AssociateAnlFiles(ILogger logger)
	{
		logger.LogInformation("Associating {Extension} files with the AnaalIJzer Graph Editor.", Extension);
		var result = Extension.CreateFileExtensionAssociation(ProgramName, FileTypeDescription);
		logger.LogInformation("Association result for {Extension}: changed={Changed}", Extension, result);

		return result;
	}
}
