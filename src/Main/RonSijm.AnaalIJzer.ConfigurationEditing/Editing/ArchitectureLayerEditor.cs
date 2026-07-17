using System.Collections.Immutable;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

internal static class ArchitectureLayerEditor
{
	internal static ArchitectureConfigurationEditResult SetLayerDescription(ArchitectureLayerEditHandle handle, string? description)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("This layer does not have an editable configuration origin.");
		}

		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			handle.SourceKind,
			handle.SourcePath,
			document =>
			{
				var element = ArchitectureConfigurationXmlNavigator.FindLayerElement(document, handle);
				if (element is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Could not find layer '" + handle.LayerPath + "' in " + handle.SourcePath + ".");
				}

				ArchitectureConfigurationXmlEditor.SetOptionalAttribute(element, ArchitectureConfigurationXmlNames.DescriptionAttributeName, description);
				return ArchitectureConfigurationEditResult.Success("Updated description for layer " + handle.LayerPath + ".");
			});

		return result;
	}

	internal static ArchitectureConfigurationEditResult SetLayerName(ArchitectureLayerEditHandle handle, string name)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("This layer does not have an editable configuration origin.");
		}

		if (string.IsNullOrWhiteSpace(name) || name.Contains("/"))
		{
			return ArchitectureConfigurationEditResult.Failure("Layer names must be non-empty and may not contain '/'.");
		}

		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			handle.SourceKind,
			handle.SourcePath,
			document =>
			{
				var element = ArchitectureConfigurationXmlNavigator.FindLayerElement(document, handle);
				if (element is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Could not find layer '" + handle.LayerPath + "' in " + handle.SourcePath + ".");
				}

				element.SetAttributeValue("name", name.Trim());
				return ArchitectureConfigurationEditResult.Success("Renamed layer " + handle.LayerPath + " to " + name.Trim() + ".");
			});

		return result;
	}

	internal static ArchitectureConfigurationEditResult SetLayerRequireRecognizedDependencies(ArchitectureLayerEditHandle handle, string? sites)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("This layer does not have an editable configuration origin.");
		}

		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			handle.SourceKind,
			handle.SourcePath,
			document =>
			{
				var element = ArchitectureConfigurationXmlNavigator.FindLayerElement(document, handle);
				if (element is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Could not find layer '" + handle.LayerPath + "' in " + handle.SourcePath + ".");
				}

				ArchitectureConfigurationXmlEditor.SetOptionalAttribute(element, ArchitectureConfigurationXmlNames.RequireRecognizedDependenciesAttributeName, sites);
				return ArchitectureConfigurationEditResult.Success("Updated requireRecognizedDependencies for layer " + handle.LayerPath + ".");
			});

		return result;
	}

	internal static ArchitectureConfigurationEditResult RemoveLayer(ArchitectureLayerEditHandle handle)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("This layer does not have an editable configuration origin.");
		}

		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			handle.SourceKind,
			handle.SourcePath,
			document =>
			{
				var element = ArchitectureConfigurationXmlNavigator.FindLayerElement(document, handle);
				if (element is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Could not find layer '" + handle.LayerPath + "' in " + handle.SourcePath + ".");
				}

				element.Remove();
				return ArchitectureConfigurationEditResult.Success("Removed layer " + handle.LayerPath + ".");
			});

		return result;
	}

	internal static ArchitectureConfigurationEditResult MoveLayer(ArchitectureLayerEditHandle handle, string newParentPath)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("This layer does not have an editable configuration origin.");
		}

		var trimmedParentPath = newParentPath.Trim().Trim('/');
		if (string.Equals(trimmedParentPath, handle.LayerPath, StringComparison.Ordinal)
		    || trimmedParentPath.StartsWith(handle.LayerPath + "/", StringComparison.Ordinal))
		{
			return ArchitectureConfigurationEditResult.Failure("A layer cannot be moved inside itself or one of its descendants.");
		}

		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			handle.SourceKind,
			handle.SourcePath,
			document =>
			{
				var element = ArchitectureConfigurationXmlNavigator.FindLayerElement(document, handle);
				if (element is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Could not find layer '" + handle.LayerPath + "' in " + handle.SourcePath + ".");
				}

				var newParent = ArchitectureConfigurationXmlNavigator.FindLayerInsertionContainer(document, trimmedParentPath);
				if (newParent is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Could not find target parent layer '" + trimmedParentPath + "'.");
				}

				element.Remove();
				newParent.Add(element);
				return ArchitectureConfigurationEditResult.Success("Moved layer " + handle.LayerPath + " to " + ArchitectureConfigurationLayerPaths.FormatScopeName(trimmedParentPath) + ".");
			});

		return result;
	}

	internal static ArchitectureConfigurationEditResult AddLayer(ArchitectureConfigurationSource source, string parentLayerPath, string name, string matcherKind, ImmutableDictionary<string, string> matcherAttributes)
	{
		if (!source.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("This configuration source is not editable.");
		}

		if (string.IsNullOrWhiteSpace(name) || name.Contains("/"))
		{
			return ArchitectureConfigurationEditResult.Failure("Layer names must be non-empty and may not contain '/'.");
		}

		if (!ArchitectureConfigurationXmlEditor.IsSupportedElementKind(matcherKind, "LayerMatcher"))
		{
			return ArchitectureConfigurationEditResult.Failure("Unsupported matcher kind '" + matcherKind + "'.");
		}

		if (!ArchitectureConfigurationXmlEditor.TryCreateAttributes(matcherAttributes, out var xAttributes, out var message))
		{
			return ArchitectureConfigurationEditResult.Failure(message);
		}

		if (xAttributes.Length == 0)
		{
			return ArchitectureConfigurationEditResult.Failure("A new layer needs at least one matcher attribute.");
		}

		var trimmedParentPath = parentLayerPath.Trim().Trim('/');
		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			source.Kind,
			source.Path,
			document =>
			{
				var container = ArchitectureConfigurationXmlNavigator.FindLayerInsertionContainer(document, trimmedParentPath);
				if (container is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Could not find target parent layer '" + trimmedParentPath + "'.");
				}

				if (container.Elements(ArchitectureConfigurationXmlNames.LayerElementName).Any(layer => string.Equals(layer.Attribute("name")?.Value, name.Trim(), StringComparison.Ordinal)))
				{
					return ArchitectureConfigurationEditResult.Failure("Layer '" + name.Trim() + "' already exists under " + ArchitectureConfigurationLayerPaths.FormatScopeName(trimmedParentPath) + ".");
				}

				container.Add(new XElement(
					ArchitectureConfigurationXmlNames.LayerElementName,
					new XAttribute("name", name.Trim()),
					new XElement(matcherKind, xAttributes)));
				return ArchitectureConfigurationEditResult.Success("Added layer " + name.Trim() + " under " + ArchitectureConfigurationLayerPaths.FormatScopeName(trimmedParentPath) + ".");
			});

		return result;
	}

	internal static ArchitectureConfigurationEditResult AddLayerMatcher(ArchitectureLayerEditHandle handle, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		var result = AddElementToLayer(handle, "LayerMatcher", elementKind, attributes);

		return result;
	}

	internal static ArchitectureConfigurationEditResult AddTypePolicyMatcher(ArchitectureLayerEditHandle handle, string policyKind, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		if (policyKind is not ArchitectureConfigurationXmlNames.AllowedElementName and not ArchitectureConfigurationXmlNames.ForbiddenElementName)
		{
			return ArchitectureConfigurationEditResult.Failure("Type policy kind must be Allowed or Forbidden.");
		}

		var result = AddElementToLayer(handle, policyKind, elementKind, attributes);

		return result;
	}

	internal static ArchitectureConfigurationEditResult AddElementToLayer(ArchitectureLayerEditHandle handle, string containerKind, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		if (!handle.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("This layer does not have an editable configuration origin.");
		}

		if (!ArchitectureConfigurationXmlEditor.IsSupportedElementKind(elementKind, containerKind))
		{
			return ArchitectureConfigurationEditResult.Failure("Unsupported element kind '" + elementKind + "'.");
		}

		if (!ArchitectureConfigurationXmlEditor.TryCreateAttributes(attributes, out var xAttributes, out var message))
		{
			return ArchitectureConfigurationEditResult.Failure(message);
		}

		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			handle.SourceKind,
			handle.SourcePath,
			document =>
			{
				var layer = ArchitectureConfigurationXmlNavigator.FindLayerElement(document, handle);
				if (layer is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Could not find layer '" + handle.LayerPath + "' in " + handle.SourcePath + ".");
				}

				if (containerKind == "LayerMatcher")
				{
					layer.Add(new XElement(elementKind, xAttributes));
					return ArchitectureConfigurationEditResult.Success("Added " + elementKind + " matcher to layer " + handle.LayerPath + ".");
				}

				var container = layer.Elements(containerKind).FirstOrDefault();
				if (container is null)
				{
					container = new XElement(containerKind);
					layer.Add(container);
				}

				container.Add(new XElement(elementKind, xAttributes));
				return ArchitectureConfigurationEditResult.Success("Added " + elementKind + " matcher to " + containerKind + " in layer " + handle.LayerPath + ".");
			});

		return result;
	}

}
