using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Dependencies;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

internal static class ArchitectureDependencyRuleEditor
{
	internal static ArchitectureConfigurationDocumentOperationResult RemoveDependency(ArchitectureDependencyRuleEditHandle handle)
	{
		var result = ArchitectureDependencyRuleMutationEditor.RemoveDependency(handle);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult SetDependencySites(ArchitectureDependencyRuleEditHandle handle, ArchitectureSiteFilterEditMode mode, ImmutableArray<string> sites)
	{
		var result = ArchitectureDependencyRuleMutationEditor.SetDependencySites(handle, mode, sites);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult SetDependencyKind(ArchitectureDependencyRuleEditHandle handle, string elementKind)
	{
		var result = ArchitectureDependencyRuleMutationEditor.SetDependencyKind(handle, elementKind);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult SetDependencyAppliesToDescendants(ArchitectureDependencyRuleEditHandle handle, bool appliesToDescendants)
	{
		var result = ArchitectureDependencyRuleMutationEditor.SetDependencyAppliesToDescendants(handle, appliesToDescendants);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult SetDependencyDescription(ArchitectureDependencyRuleEditHandle handle, string? description)
	{
		var result = ArchitectureDependencyRuleMutationEditor.SetDependencyDescription(handle, description);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddAllowedDependency(ArchitectureConfigurationSource source, string from, string to)
	{
		var result = ArchitectureDependencyRuleCreationEditor.AddAllowedDependency(source, from, to);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddDependency(ArchitectureConfigurationSource source, string from, string to, string elementKind)
	{
		var result = ArchitectureDependencyRuleCreationEditor.AddDependency(source, from, to, elementKind);

		return result;
	}
}
