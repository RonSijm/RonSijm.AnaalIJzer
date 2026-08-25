using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

public static partial class ArchitectureConfigurationEditService
{
	public static ArchitectureConfigurationEditResult RemoveDependency(ArchitectureDependencyRuleEditHandle handle)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureDependencyRuleEditor.RemoveDependency(handle));

		return result;
	}

	public static ArchitectureConfigurationEditResult SetDependencySites(ArchitectureDependencyRuleEditHandle handle, ArchitectureSiteFilterEditMode mode, ImmutableArray<string> sites)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureDependencyRuleEditor.SetDependencySites(handle, mode, sites));

		return result;
	}

	public static ArchitectureConfigurationEditResult SetDependencyKind(ArchitectureDependencyRuleEditHandle handle, string elementKind)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureDependencyRuleEditor.SetDependencyKind(handle, elementKind));

		return result;
	}

	public static ArchitectureConfigurationEditResult SetDependencyAppliesToDescendants(ArchitectureDependencyRuleEditHandle handle, bool appliesToDescendants)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureDependencyRuleEditor.SetDependencyAppliesToDescendants(handle, appliesToDescendants));

		return result;
	}

	public static ArchitectureConfigurationEditResult SetDependencyDescription(ArchitectureDependencyRuleEditHandle handle, string? description)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureDependencyRuleEditor.SetDependencyDescription(handle, description));

		return result;
	}

	public static ArchitectureConfigurationEditResult AddAllowedDependency(ArchitectureConfigurationSource source, string from, string to)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureDependencyRuleEditor.AddAllowedDependency(source, from, to));

		return result;
	}

	public static ArchitectureConfigurationEditResult AddDependency(ArchitectureConfigurationSource source, string from, string to, string elementKind)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureDependencyRuleEditor.AddDependency(source, from, to, elementKind));

		return result;
	}
}
