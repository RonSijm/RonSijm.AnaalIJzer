using System.Collections.Immutable;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Layers;

internal static class ArchitectureLayerStructureEditor
{
	internal static ArchitectureConfigurationDocumentOperationResult SetLayerName(ArchitectureLayerEditHandle handle, string name)
	{
		if (string.IsNullOrWhiteSpace(name) || name.Contains("/"))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("Layer names must be non-empty and may not contain '/'.");
		}

		var trimmedName = name.Trim();
		var result = ArchitectureLayerMutationExecutor.EditLayer(
			handle,
			(_, layer) =>
			{
				layer.SetAttributeValue("name", trimmedName);
				var editResult = ArchitectureConfigurationDocumentOperationResult.Success("Renamed layer " + handle.LayerPath + " to " + trimmedName + ".");

				return editResult;
			});

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult RemoveLayer(ArchitectureLayerEditHandle handle)
	{
		var result = ArchitectureLayerMutationExecutor.EditLayer(
			handle,
			(_, layer) =>
			{
				layer.Remove();
				var editResult = ArchitectureConfigurationDocumentOperationResult.Success("Removed layer " + handle.LayerPath + ".");

				return editResult;
			});

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult MoveLayer(ArchitectureLayerEditHandle handle, string newParentPath)
	{
		var trimmedParentPath = newParentPath.Trim().Trim('/');
		if (string.Equals(trimmedParentPath, handle.LayerPath, StringComparison.Ordinal)
		    || trimmedParentPath.StartsWith(handle.LayerPath + "/", StringComparison.Ordinal))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("A layer cannot be moved inside itself or one of its descendants.");
		}

		var result = ArchitectureLayerMutationExecutor.EditLayer(
			handle,
			(document, layer) =>
			{
				var newParent = ArchitectureConfigurationXmlNavigator.FindLayerInsertionContainer(document, trimmedParentPath);
				if (newParent is null)
				{
					return ArchitectureConfigurationDocumentOperationResult.Failure("Could not find target parent layer '" + trimmedParentPath + "'.");
				}

				layer.Remove();
				newParent.Add(layer);
				var editResult = ArchitectureConfigurationDocumentOperationResult.Success("Moved layer " + handle.LayerPath + " to " + ArchitectureConfigurationLayerPaths.FormatScopeName(trimmedParentPath) + ".");

				return editResult;
			});

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddLayer(ArchitectureConfigurationSource source, string parentLayerPath, string name, string matcherKind, ImmutableDictionary<string, string> matcherAttributes)
	{
		if (!source.CanEdit)
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("This configuration source is not editable.");
		}

		if (string.IsNullOrWhiteSpace(name) || name.Contains("/"))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("Layer names must be non-empty and may not contain '/'.");
		}

		if (!ArchitectureConfigurationXmlEditor.IsSupportedElementKind(matcherKind, "LayerMatcher"))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("Unsupported matcher kind '" + matcherKind + "'.");
		}

		if (!ArchitectureConfigurationXmlEditor.TryCreateAttributes(matcherAttributes, out var xAttributes, out var message))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure(message);
		}

		if (xAttributes.Length == 0)
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("A new layer needs at least one matcher attribute.");
		}

		var trimmedParentPath = parentLayerPath.Trim().Trim('/');
		var trimmedName = name.Trim();
		var result = ArchitectureConfigurationEditExecution.EditConfiguration(
			source.Kind,
			source.Path,
			document =>
			{
				var container = ArchitectureConfigurationXmlNavigator.FindLayerInsertionContainer(document, trimmedParentPath);
				if (container is null)
				{
					return ArchitectureConfigurationDocumentOperationResult.Failure("Could not find target parent layer '" + trimmedParentPath + "'.");
				}

				if (container.Elements(ArchitectureConfigurationXmlNames.LayerElementName).Any(layer => string.Equals(layer.Attribute("name")?.Value, trimmedName, StringComparison.Ordinal)))
				{
					return ArchitectureConfigurationDocumentOperationResult.Failure("Layer '" + trimmedName + "' already exists under " + ArchitectureConfigurationLayerPaths.FormatScopeName(trimmedParentPath) + ".");
				}

				container.Add(CreateLayerElement(trimmedName, matcherKind, xAttributes));
				var editResult = ArchitectureConfigurationDocumentOperationResult.Success("Added layer " + trimmedName + " under " + ArchitectureConfigurationLayerPaths.FormatScopeName(trimmedParentPath) + ".");

				return editResult;
			});

		return result;
	}

	private static XElement CreateLayerElement(string name, string matcherKind, ImmutableArray<XAttribute> xAttributes)
	{
		var result = new XElement(
			ArchitectureConfigurationXmlNames.LayerElementName,
			new XAttribute("name", name),
			new XElement(matcherKind, xAttributes));

		return result;
	}
}
