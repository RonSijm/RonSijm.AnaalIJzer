using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.GraphApplication;

internal sealed partial class ArchitectureGraphEditService
{
	public ArchitectureConfigurationEditResult RemoveDependency(ArchitectureDependencyRuleEditHandle handle)
	{
		var result = ArchitectureConfigurationEditService.RemoveDependency(handle);

		return result;
	}

	public ArchitectureConfigurationEditResult SetDependencySites(ArchitectureDependencyRuleEditHandle handle, ArchitectureSiteFilterEditMode mode, ImmutableArray<string> sites)
	{
		var result = ArchitectureConfigurationEditService.SetDependencySites(handle, mode, sites);

		return result;
	}

	public ArchitectureConfigurationEditResult SetDependencyKind(ArchitectureDependencyRuleEditHandle handle, string elementKind)
	{
		var result = ArchitectureConfigurationEditService.SetDependencyKind(handle, elementKind);

		return result;
	}

	public ArchitectureConfigurationEditResult SetDependencyAppliesToDescendants(ArchitectureDependencyRuleEditHandle handle, bool appliesToDescendants)
	{
		var result = ArchitectureConfigurationEditService.SetDependencyAppliesToDescendants(handle, appliesToDescendants);

		return result;
	}

	public ArchitectureConfigurationEditResult SetDependencyDescription(ArchitectureDependencyRuleEditHandle handle, string? description)
	{
		var result = ArchitectureConfigurationEditService.SetDependencyDescription(handle, description);

		return result;
	}

	public ArchitectureConfigurationEditResult AddAllowedDependency(ArchitectureConfigurationSource source, string from, string to)
	{
		var result = ArchitectureConfigurationEditService.AddAllowedDependency(source, from, to);

		return result;
	}

	public ArchitectureConfigurationEditResult AddDependency(ArchitectureConfigurationSource source, string from, string to, string elementKind)
	{
		var result = ArchitectureConfigurationEditService.AddDependency(source, from, to, elementKind);

		return result;
	}
}
