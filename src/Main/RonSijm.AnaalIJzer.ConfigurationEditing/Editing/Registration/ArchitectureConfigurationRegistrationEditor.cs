using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Registration;

internal static class ArchitectureConfigurationRegistrationEditor
{
	internal static ArchitectureConfigurationEditResult Register(ArchitectureConfigurationCreationTarget target)
	{
		var result = target.RegistrationKind switch
		{
			ArchitectureConfigurationRegistrationKind.None => ArchitectureConfigurationEditResult.Success("No MSBuild registration was requested."),
			ArchitectureConfigurationRegistrationKind.ProjectFile => ArchitectureConfigurationMsbuildRegistrationEditor.RegisterAdditionalFile(target.RegistrationPath, target.Source.Path, false),
			ArchitectureConfigurationRegistrationKind.DirectoryBuildProps => ArchitectureConfigurationMsbuildRegistrationEditor.RegisterAdditionalFile(target.RegistrationPath, target.Source.Path, true),
			_ => ArchitectureConfigurationEditResult.Failure("Unknown architecture configuration registration target.")
		};

		return result;
	}
}
