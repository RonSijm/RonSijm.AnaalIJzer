using System.Collections.Immutable;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Layers;

internal static class ArchitectureLayerPolicyEditor
{
	internal static ArchitectureConfigurationDocumentOperationResult AddLayerMatcher(ArchitectureLayerEditHandle handle, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var result = AddElementToLayer(handle, "LayerMatcher", elementKind, attributes);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddTypePolicyMatcher(ArchitectureLayerEditHandle handle, string policyKind, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		if (policyKind is not ArchitectureConfigurationXmlNames.AllowedElementName and not ArchitectureConfigurationXmlNames.ForbiddenElementName)
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("Type policy kind must be Allowed or Forbidden.");
		}

		var result = AddElementToLayer(handle, policyKind, elementKind, attributes);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddNameRule(ArchitectureLayerEditHandle handle, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var result = AddElementToLayer(handle, ArchitectureConfigurationXmlNames.NameRulesElementName, elementKind, attributes);

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddVisibilityPolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes)
	{
		if (!ArchitectureConfigurationXmlEditor.TryCreateAttributes(attributes, out var xAttributes, out var message))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure(message);
		}

		var targets = attributes.TryGetValue("targets", out var configuredTargets) ? configuredTargets : null;
		var hasAllowed = attributes.TryGetValue("allowedAccessibilities", out var allowed) && !string.IsNullOrWhiteSpace(allowed);
		var hasBlocked = attributes.TryGetValue("blockedAccessibilities", out var blocked) && !string.IsNullOrWhiteSpace(blocked);
		if (string.IsNullOrWhiteSpace(targets) || hasAllowed == hasBlocked)
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("VisibilityPolicy requires targets and exactly one accessibility allowlist or blocklist.");
		}

		var result = ArchitectureLayerMutationExecutor.EditLayer(
			handle,
			(_, layer) =>
			{
				layer.Add(new XElement(ArchitectureConfigurationXmlNames.VisibilityPolicyElementName, xAttributes));
				var editResult = ArchitectureConfigurationDocumentOperationResult.Success("Added VisibilityPolicy to layer " + handle.LayerPath + ".");

				return editResult;
			});

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddInheritancePolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes)
	{
		if (!ArchitectureConfigurationXmlEditor.TryCreateAttributes(attributes, out var xAttributes, out var message))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure(message);
		}

		var hasTypeKinds = attributes.TryGetValue("typeKinds", out var typeKinds) && !string.IsNullOrWhiteSpace(typeKinds);
		var hasBaseTypes = attributes.TryGetValue("requiredBaseTypes", out var requiredBaseTypes) && !string.IsNullOrWhiteSpace(requiredBaseTypes);
		var hasInterfaces = attributes.TryGetValue("requiredInterfaces", out var requiredInterfaces) && !string.IsNullOrWhiteSpace(requiredInterfaces);
		if (!hasTypeKinds || (!hasBaseTypes && !hasInterfaces))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("InheritancePolicy requires typeKinds and at least one of requiredBaseTypes or requiredInterfaces.");
		}

		var result = ArchitectureLayerMutationExecutor.EditLayer(
			handle,
			(_, layer) =>
			{
				layer.Add(new XElement(ArchitectureConfigurationXmlNames.InheritancePolicyElementName, xAttributes));
				var editResult = ArchitectureConfigurationDocumentOperationResult.Success("Added InheritancePolicy to layer " + handle.LayerPath + ".");

				return editResult;
			});

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddApiSurfacePolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes, string childXml)
	{
		if (!ArchitectureConfigurationXmlEditor.TryCreateAttributes(attributes, out var xAttributes, out var attributeMessage))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure(attributeMessage);
		}

		if (!ArchitectureConfigurationXmlEditor.TryParseChildNodes(childXml, out var childNodes, out var childMessage))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure(childMessage);
		}

		var children = childNodes.OfType<XElement>().ToArray();
		var layerRules = children.Where(child => child.Name.LocalName is "AllowedLayer" or "BlockedLayer").ToArray();
		var invalidChildren = children.Any(child =>
			child.Name.LocalName switch
			{
				"AllowedLayer" or "BlockedLayer" => string.IsNullOrWhiteSpace(child.Attribute("path")?.Value),
				"TransitiveExposure" => false,
				_ => true
			});
		if (layerRules.Length == 0 || invalidChildren)
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("ApiSurface requires at least one AllowedLayer or BlockedLayer with a path and supports one optional TransitiveExposure element.");
		}

		var result = ArchitectureLayerMutationExecutor.EditLayer(
			handle,
			(_, layer) =>
			{
				layer.Add(new XElement(ArchitectureConfigurationXmlNames.ApiSurfaceElementName, xAttributes, children));
				var editResult = ArchitectureConfigurationDocumentOperationResult.Success("Added ApiSurface to layer " + handle.LayerPath + ".");

				return editResult;
			});

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddElementToLayer(ArchitectureLayerEditHandle handle, string containerKind, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		if (!ArchitectureConfigurationXmlEditor.IsSupportedElementKind(elementKind, containerKind))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("Unsupported element kind '" + elementKind + "'.");
		}

		if (!ArchitectureConfigurationXmlEditor.TryCreateAttributes(attributes, out var xAttributes, out var message))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure(message);
		}

		var result = ArchitectureLayerMutationExecutor.EditLayer(
			handle,
			(_, layer) =>
			{
				if (containerKind == "LayerMatcher")
				{
					layer.Add(new XElement(elementKind, xAttributes));
					var matcherResult = ArchitectureConfigurationDocumentOperationResult.Success("Added " + elementKind + " matcher to layer " + handle.LayerPath + ".");

					return matcherResult;
				}

				var container = layer.Elements(containerKind).FirstOrDefault();
				if (container is null)
				{
					container = new XElement(containerKind);
					layer.Add(container);
				}

				container.Add(new XElement(elementKind, xAttributes));
				var editResult = ArchitectureConfigurationDocumentOperationResult.Success("Added " + elementKind + " to " + containerKind + " in layer " + handle.LayerPath + ".");

				return editResult;
			});

		return result;
	}
}
