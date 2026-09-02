using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.GraphApplication;

internal sealed partial class ArchitectureGraphEditService
{
	public ArchitectureConfigurationEditResult SetLayerDescription(ArchitectureLayerEditHandle handle, string? description)
	{
		var result = ArchitectureConfigurationEditService.SetLayerDescription(handle, description);

		return result;
	}

	public ArchitectureConfigurationEditResult SetLayerName(ArchitectureLayerEditHandle handle, string name)
	{
		var result = ArchitectureConfigurationEditService.SetLayerName(handle, name);

		return result;
	}

	public ArchitectureConfigurationEditResult SetLayerRequireRecognizedDependencies(ArchitectureLayerEditHandle handle, string? sites)
	{
		var result = ArchitectureConfigurationEditService.SetLayerRequireRecognizedDependencies(handle, sites);

		return result;
	}

	public ArchitectureConfigurationEditResult RemoveLayer(ArchitectureLayerEditHandle handle)
	{
		var result = ArchitectureConfigurationEditService.RemoveLayer(handle);

		return result;
	}

	public ArchitectureConfigurationEditResult MoveLayer(ArchitectureLayerEditHandle handle, string newParentPath)
	{
		var result = ArchitectureConfigurationEditService.MoveLayer(handle, newParentPath);

		return result;
	}

	public ArchitectureConfigurationEditResult AddLayer(ArchitectureConfigurationSource source, string parentLayerPath, string name, string matcherKind, ImmutableDictionary<string, string> matcherAttributes)
	{
		var result = ArchitectureConfigurationEditService.AddLayer(source, parentLayerPath, name, matcherKind, matcherAttributes);

		return result;
	}

	public ArchitectureConfigurationEditResult AddLayerMatcher(ArchitectureLayerEditHandle handle, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureConfigurationEditService.AddLayerMatcher(handle, elementKind, attributes);

		return result;
	}

	public ArchitectureConfigurationEditResult AddTypePolicyMatcher(ArchitectureLayerEditHandle handle, string policyKind, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureConfigurationEditService.AddTypePolicyMatcher(handle, policyKind, elementKind, attributes);

		return result;
	}

	public ArchitectureConfigurationEditResult AddNameRule(ArchitectureLayerEditHandle handle, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureConfigurationEditService.AddNameRule(handle, elementKind, attributes);

		return result;
	}

	public ArchitectureConfigurationEditResult AddInheritancePolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureConfigurationEditService.AddInheritancePolicy(handle, attributes);

		return result;
	}

	public ArchitectureConfigurationEditResult AddReturnValuePolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes, string childXml)
	{
		var result = ArchitectureConfigurationEditService.AddReturnValuePolicy(handle, attributes, childXml);

		return result;
	}

	public ArchitectureConfigurationEditResult AddVisibilityPolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureConfigurationEditService.AddVisibilityPolicy(handle, attributes);

		return result;
	}

	public ArchitectureConfigurationEditResult AddApiSurfacePolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes, string childXml)
	{
		var result = ArchitectureConfigurationEditService.AddApiSurfacePolicy(handle, attributes, childXml);

		return result;
	}
}
