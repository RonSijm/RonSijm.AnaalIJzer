using RonSijm.AnaalIJzer.Arse.FileExtension;

namespace RonSijm.AnaalIJzer.Arse;

internal static partial class ArseCommandLine
{
	private static bool TryRunFileAssociationCommand(string commandName, out FileAssociationResult result)
	{
		if (string.Equals(commandName, "associate-anl", StringComparison.OrdinalIgnoreCase))
		{
			result = ArseFileAssociation.AssociateAnlFiles();
			return true;
		}

		if (string.Equals(commandName, "unassociate-anl", StringComparison.OrdinalIgnoreCase))
		{
			result = ArseFileAssociation.UnassociateAnlFiles();
			return true;
		}

		result = new FileAssociationResult(false, string.Empty);
		return false;
	}
}
