using RonSijm.IJzer.Forge.FileExtension;

namespace RonSijm.IJzer.Forge;

internal static partial class ForgeCommandLine
{
    private static bool TryRunFileAssociationCommand(string commandName, out FileAssociationResult result)
    {
        if (string.Equals(commandName, "associate-anl", StringComparison.OrdinalIgnoreCase))
        {
            result = ForgeFileAssociation.AssociateAnlFiles();
            return true;
        }

        if (string.Equals(commandName, "unassociate-anl", StringComparison.OrdinalIgnoreCase))
        {
            result = ForgeFileAssociation.UnassociateAnlFiles();
            return true;
        }

        result = new FileAssociationResult(false, string.Empty);
        return false;
    }
}