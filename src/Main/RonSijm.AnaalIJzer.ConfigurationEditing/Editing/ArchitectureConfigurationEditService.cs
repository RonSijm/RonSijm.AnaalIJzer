using System.Collections.Immutable;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

public static class ArchitectureConfigurationEditService
{
	public static ArchitectureConfigurationEditResult RemoveDependency(ArchitectureDependencyRuleEditHandle handle)
	{
		var result = ArchitectureDependencyRuleEditor.RemoveDependency(handle);

		return result;
	}

	public static ArchitectureConfigurationEditResult SetDependencySites(ArchitectureDependencyRuleEditHandle handle, ArchitectureSiteFilterEditMode mode, ImmutableArray<string> sites)
	{
		var result = ArchitectureDependencyRuleEditor.SetDependencySites(handle, mode, sites);

		return result;
	}

	public static ArchitectureConfigurationEditResult SetDependencyKind(ArchitectureDependencyRuleEditHandle handle, string elementKind)
	{
		var result = ArchitectureDependencyRuleEditor.SetDependencyKind(handle, elementKind);

		return result;
	}

	public static ArchitectureConfigurationEditResult SetDependencyAppliesToDescendants(ArchitectureDependencyRuleEditHandle handle, bool appliesToDescendants)
	{
		var result = ArchitectureDependencyRuleEditor.SetDependencyAppliesToDescendants(handle, appliesToDescendants);

		return result;
	}

	public static ArchitectureConfigurationEditResult SetDependencyDescription(ArchitectureDependencyRuleEditHandle handle, string? description)
	{
		var result = ArchitectureDependencyRuleEditor.SetDependencyDescription(handle, description);

		return result;
	}

	public static ArchitectureConfigurationEditResult SetLayerDescription(ArchitectureLayerEditHandle handle, string? description)
	{
		var result = ArchitectureLayerEditor.SetLayerDescription(handle, description);

		return result;
	}

	public static ArchitectureConfigurationEditResult SetLayerName(ArchitectureLayerEditHandle handle, string name)
	{
		var result = ArchitectureLayerEditor.SetLayerName(handle, name);

		return result;
	}

	public static ArchitectureConfigurationEditResult SetLayerRequireRecognizedDependencies(ArchitectureLayerEditHandle handle, string? sites)
	{
		var result = ArchitectureLayerEditor.SetLayerRequireRecognizedDependencies(handle, sites);

		return result;
	}

	public static ArchitectureConfigurationEditResult RemoveLayer(ArchitectureLayerEditHandle handle)
	{
		var result = ArchitectureLayerEditor.RemoveLayer(handle);

		return result;
	}

	public static ArchitectureConfigurationEditResult MoveLayer(ArchitectureLayerEditHandle handle, string newParentPath)
	{
		var result = ArchitectureLayerEditor.MoveLayer(handle, newParentPath);

		return result;
	}

	public static ArchitectureConfigurationEditResult AddLayer(ArchitectureConfigurationSource source, string parentLayerPath, string name, string matcherKind, ImmutableDictionary<string, string> matcherAttributes)
	{
		var result = ArchitectureLayerEditor.AddLayer(source, parentLayerPath, name, matcherKind, matcherAttributes);

		return result;
	}

	public static ArchitectureLayerInspectionResult GetLayerDetails(ArchitectureLayerEditHandle handle)
	{
		var result = ArchitectureConfigurationInspectionReader.GetLayerDetails(handle);

		return result;
	}

	public static ArchitectureRootInspectionResult GetRootDetails(ArchitectureConfigurationSource source)
	{
		var result = ArchitectureConfigurationInspectionReader.GetRootDetails(source);

		return result;
	}

	public static ArchitectureConfigurationEditResult ReadConfiguration(ArchitectureConfigurationSource source, out XDocument? document)
	{
		if (!source.CanEdit)
		{
			document = null;
			return ArchitectureConfigurationEditResult.Failure("This configuration source cannot be inspected.");
		}

		var result = ArchitectureConfigurationDocumentStore.ReadConfiguration(source.Kind, source.Path, out document);

		return result;
	}

	public static ArchitectureConfigurationEditResult SetRootSettings(
		ArchitectureConfigurationSource source,
		string? description,
		string? requireRecognizedDependencies,
		bool enforceAcyclic,
		bool enableReport,
		string? reportPath,
		bool enableDocumentation,
		string? documentationPath)
	{
		var result = ArchitectureRootEditor.SetRootSettings(source, description, requireRecognizedDependencies, enforceAcyclic, enableReport, reportPath, enableDocumentation, documentationPath);

		return result;
	}

	public static ArchitectureConfigurationEditResult SetConfigurationElementAttributes(ArchitectureConfigurationElementEditHandle handle, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureConfigurationElementEditor.SetConfigurationElementAttributes(handle, attributes);

		return result;
	}

	public static ArchitectureConfigurationEditResult SetConfigurationElementChildren(ArchitectureConfigurationElementEditHandle handle, string childXml)
	{
		var result = ArchitectureConfigurationElementEditor.SetConfigurationElementChildren(handle, childXml);

		return result;
	}

	public static ArchitectureConfigurationEditResult RemoveConfigurationElement(ArchitectureConfigurationElementEditHandle handle)
	{
		var result = ArchitectureConfigurationElementEditor.RemoveConfigurationElement(handle);

		return result;
	}

	public static ArchitectureConfigurationEditResult AddLayerMatcher(ArchitectureLayerEditHandle handle, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureLayerEditor.AddLayerMatcher(handle, elementKind, attributes);

		return result;
	}

	public static ArchitectureConfigurationEditResult AddTypePolicyMatcher(ArchitectureLayerEditHandle handle, string policyKind, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureLayerEditor.AddTypePolicyMatcher(handle, policyKind, elementKind, attributes);

		return result;
	}

	public static ArchitectureConfigurationEditResult AddGlobalTypePolicyMatcher(ArchitectureConfigurationSource source, string policyKind, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureRootEditor.AddGlobalTypePolicyMatcher(source, policyKind, elementKind, attributes);

		return result;
	}

	public static ArchitectureConfigurationEditResult AddInclude(ArchitectureConfigurationSource source, string path)
	{
		var result = ArchitectureRootEditor.AddInclude(source, path);

		return result;
	}

	public static ArchitectureConfigurationEditResult AddAllowedDependency(ArchitectureConfigurationSource source, string from, string to)
	{
		var result = ArchitectureDependencyRuleEditor.AddAllowedDependency(source, from, to);

		return result;
	}

	public static ArchitectureConfigurationEditResult AddDependency(ArchitectureConfigurationSource source, string from, string to, string elementKind)
	{
		var result = ArchitectureDependencyRuleEditor.AddDependency(source, from, to, elementKind);

		return result;
	}
}
