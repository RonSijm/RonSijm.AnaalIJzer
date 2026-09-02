using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Layers;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

public static partial class ArchitectureConfigurationEditService
{
	public static ArchitectureConfigurationEditResult SetLayerDescription(ArchitectureLayerEditHandle handle, string? description)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureLayerEditor.SetLayerDescription(handle, description));

		return result;
	}

	public static ArchitectureConfigurationEditResult SetLayerName(ArchitectureLayerEditHandle handle, string name)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureLayerEditor.SetLayerName(handle, name));

		return result;
	}

	public static ArchitectureConfigurationEditResult SetLayerRequireRecognizedDependencies(ArchitectureLayerEditHandle handle, string? sites)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureLayerEditor.SetLayerRequireRecognizedDependencies(handle, sites));

		return result;
	}

	public static ArchitectureConfigurationEditResult RemoveLayer(ArchitectureLayerEditHandle handle)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureLayerEditor.RemoveLayer(handle));

		return result;
	}

	public static ArchitectureConfigurationEditResult MoveLayer(ArchitectureLayerEditHandle handle, string newParentPath)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureLayerEditor.MoveLayer(handle, newParentPath));

		return result;
	}

	public static ArchitectureConfigurationEditResult AddLayer(ArchitectureConfigurationSource source, string parentLayerPath, string name, string matcherKind, ImmutableDictionary<string, string> matcherAttributes)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureLayerEditor.AddLayer(source, parentLayerPath, name, matcherKind, matcherAttributes));

		return result;
	}

	public static ArchitectureConfigurationEditResult AddLayerMatcher(ArchitectureLayerEditHandle handle, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureLayerEditor.AddLayerMatcher(handle, elementKind, attributes));

		return result;
	}

	public static ArchitectureConfigurationEditResult AddTypePolicyMatcher(ArchitectureLayerEditHandle handle, string policyKind, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureLayerEditor.AddTypePolicyMatcher(handle, policyKind, elementKind, attributes));

		return result;
	}

	public static ArchitectureConfigurationEditResult AddNameRule(ArchitectureLayerEditHandle handle, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureLayerEditor.AddNameRule(handle, elementKind, attributes));

		return result;
	}

	public static ArchitectureConfigurationEditResult AddVisibilityPolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureLayerEditor.AddVisibilityPolicy(handle, attributes));

		return result;
	}

	public static ArchitectureConfigurationEditResult AddInheritancePolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureLayerEditor.AddInheritancePolicy(handle, attributes));

		return result;
	}

	public static ArchitectureConfigurationEditResult AddReturnValuePolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes, string childXml)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureLayerEditor.AddReturnValuePolicy(handle, attributes, childXml));

		return result;
	}

	public static ArchitectureConfigurationEditResult AddApiSurfacePolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes, string childXml)
	{
		var result = ArchitectureConfigurationEditResult.FromDocumentResult(ArchitectureLayerEditor.AddApiSurfacePolicy(handle, attributes, childXml));

		return result;
	}
}
