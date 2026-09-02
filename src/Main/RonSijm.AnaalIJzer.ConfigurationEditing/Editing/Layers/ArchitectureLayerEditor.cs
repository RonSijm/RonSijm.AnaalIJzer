using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Layers;

internal static class ArchitectureLayerEditor
{
	internal static ArchitectureConfigurationDocumentOperationResult SetLayerDescription(ArchitectureLayerEditHandle handle, string? description)
	{
		var result = ArchitectureLayerMetadataEditor.SetLayerDescription(handle, description);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult SetLayerName(ArchitectureLayerEditHandle handle, string name)
	{
		var result = ArchitectureLayerStructureEditor.SetLayerName(handle, name);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult SetLayerRequireRecognizedDependencies(ArchitectureLayerEditHandle handle, string? sites)
	{
		var result = ArchitectureLayerMetadataEditor.SetLayerRequireRecognizedDependencies(handle, sites);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult RemoveLayer(ArchitectureLayerEditHandle handle)
	{
		var result = ArchitectureLayerStructureEditor.RemoveLayer(handle);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult MoveLayer(ArchitectureLayerEditHandle handle, string newParentPath)
	{
		var result = ArchitectureLayerStructureEditor.MoveLayer(handle, newParentPath);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddLayer(ArchitectureConfigurationSource source, string parentLayerPath, string name, string matcherKind, ImmutableDictionary<string, string> matcherAttributes)
	{
		var result = ArchitectureLayerStructureEditor.AddLayer(source, parentLayerPath, name, matcherKind, matcherAttributes);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddLayerMatcher(ArchitectureLayerEditHandle handle, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureLayerPolicyEditor.AddLayerMatcher(handle, elementKind, attributes);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddTypePolicyMatcher(ArchitectureLayerEditHandle handle, string policyKind, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureLayerPolicyEditor.AddTypePolicyMatcher(handle, policyKind, elementKind, attributes);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddNameRule(ArchitectureLayerEditHandle handle, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureLayerPolicyEditor.AddNameRule(handle, elementKind, attributes);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddVisibilityPolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureLayerPolicyEditor.AddVisibilityPolicy(handle, attributes);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddInheritancePolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes)
	{
		var result = ArchitectureLayerPolicyEditor.AddInheritancePolicy(handle, attributes);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddReturnValuePolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes, string childXml)
	{
		var result = ArchitectureLayerPolicyEditor.AddReturnValuePolicy(handle, attributes, childXml);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddApiSurfacePolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes, string childXml)
	{
		var result = ArchitectureLayerPolicyEditor.AddApiSurfacePolicy(handle, attributes, childXml);

		return result;
	}
}
