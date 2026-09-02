using System.Collections.Immutable;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;

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

	internal static ArchitectureConfigurationDocumentOperationResult AddReturnValuePolicy(ArchitectureLayerEditHandle handle, ImmutableDictionary<string, string> attributes, string childXml)
	{
		if (attributes.Keys.Any(attributeName => attributeName is not "description" and not "comment"))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("ReturnValuePolicy supports description and comment attributes. Configure forbidden returned expressions as child matchers.");
		}

		if (!ArchitectureConfigurationXmlEditor.TryCreateAttributes(attributes, out var xAttributes, out var attributeMessage))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure(attributeMessage);
		}

		if (!ArchitectureConfigurationXmlEditor.TryParseChildNodes(childXml, out var childNodes, out var childMessage))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure(childMessage);
		}

		var meaningfulChildNodes = childNodes
			.Where(node => node is not XText text || !string.IsNullOrWhiteSpace(text.Value))
			.ToArray();
		var rules = meaningfulChildNodes.OfType<XElement>().ToArray();
		if (rules.Length == 0 || rules.Length != meaningfulChildNodes.Length || rules.Any(IsInvalidReturnValueRule))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("ReturnValuePolicy requires one or more Literal, Invocation, New, Identifier, or MemberAccess matcher children with supported matcher attributes.");
		}

		var result = ArchitectureLayerMutationExecutor.EditLayer(
			handle,
			(_, layer) =>
			{
				layer.Add(new XElement(ArchitectureConfigurationXmlNames.ReturnValuePolicyElementName, xAttributes, rules));
				var editResult = ArchitectureConfigurationDocumentOperationResult.Success("Added ReturnValuePolicy to layer " + handle.LayerPath + ".");

				return editResult;
			});

		return result;
	}

	private static bool IsInvalidReturnValueRule(XElement rule)
	{
		if (rule.Name.LocalName is not ("Literal" or "Invocation" or "New" or "Identifier" or "MemberAccess"))
		{
			return true;
		}

		var result = rule.Attributes().Any(attribute => !IsReturnValueRuleAttribute(attribute.Name.LocalName, rule.Name.LocalName));

		return result;
	}

	private static bool IsReturnValueRuleAttribute(string attributeName, string ruleKind)
	{
		var result = attributeName is "description" or "comment" or "typeName" or "exactName" or "exactFullName"
			or "inherits" or "implements" or "withAttribute" or "withAccessModifier" or "typeKind" or "endsWith"
			or "startsWith" or "contains" or "regex"
			|| ruleKind == "Literal" && attributeName == "value";

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

	private static ArchitectureConfigurationDocumentOperationResult AddElementToLayer(ArchitectureLayerEditHandle handle, string containerKind, string elementKind, ImmutableDictionary<string, string> attributes)
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
