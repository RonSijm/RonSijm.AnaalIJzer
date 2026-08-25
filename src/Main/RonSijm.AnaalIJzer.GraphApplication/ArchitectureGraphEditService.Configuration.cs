using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.GraphApplication;

internal sealed partial class ArchitectureGraphEditService
{
	public ArchitectureConfigurationEditResult CreateConfiguration(ArchitectureConfigurationCreationTarget target)
	{
		var result = ArchitectureConfigurationEditService.CreateConfiguration(target);

		return result;
	}

	public ArchitectureConfigurationEditResult CreateConfiguration(ArchitectureConfigurationSource source)
	{
		var result = ArchitectureConfigurationEditService.CreateConfiguration(source);

		return result;
	}

	public ArchitectureConfigurationEditResult ReadConfiguration(ArchitectureConfigurationSource source, out XDocument? document)
	{
		var result = ArchitectureConfigurationEditService.ReadConfiguration(source, out document);

		return result;
	}
}
