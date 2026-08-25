using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Registration;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

public static partial class ArchitectureConfigurationEditService
{
	public static ArchitectureConfigurationEditResult CreateConfiguration(ArchitectureConfigurationCreationTarget target)
	{
		if (target.Source.Kind != ArchitectureConfigurationSourceKind.XmlFile)
		{
			return ArchitectureConfigurationEditResult.Failure("New configurations can only be created as Architecture.anl files.");
		}

		var createResult = ArchitectureConfigurationEditResult.FromDocumentResult(
			ArchitectureConfigurationDocumentPersistence.CreateXmlFile(target.Source.Path));
		if (!createResult.Succeeded)
		{
			return createResult;
		}

		var registrationResult = ArchitectureConfigurationRegistrationEditor.Register(target);
		if (!registrationResult.Succeeded)
		{
			return ArchitectureConfigurationEditResult.Failure(createResult.Message + " " + registrationResult.Message);
		}

		var result = ArchitectureConfigurationEditResult.Success(createResult.Message + " " + registrationResult.Message);

		return result;
	}

	public static ArchitectureConfigurationEditResult CreateConfiguration(ArchitectureConfigurationSource source)
	{
		var target = new ArchitectureConfigurationCreationTarget("Architecture.anl", "Create an AnaalIJzer configuration file.", source);
		var result = CreateConfiguration(target);

		return result;
	}

	public static ArchitectureConfigurationEditResult ReadConfiguration(ArchitectureConfigurationSource source, out XDocument? document)
	{
		if (!source.CanEdit)
		{
			document = null;
			return ArchitectureConfigurationEditResult.Failure("This configuration source cannot be inspected.");
		}

		var result = ArchitectureConfigurationEditResult.FromDocumentResult(
			ArchitectureConfigurationDocumentPersistence.ReadConfiguration(source, out document));

		return result;
	}
}
