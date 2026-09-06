using System.Collections.Immutable;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Root;

internal static class ArchitectureRootCompositionEditor
{
	internal static ArchitectureConfigurationDocumentOperationResult AddGlobalTypePolicyMatcher(ArchitectureConfigurationSource source, string policyKind, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		if (policyKind is not ArchitectureConfigurationXmlNames.AllowedElementName and not ArchitectureConfigurationXmlNames.ForbiddenElementName)
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("Type policy kind must be Allowed or Forbidden.");
		}

		if (!source.CanEdit)
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("This configuration source is not editable.");
		}

		if (!ArchitectureConfigurationXmlEditor.IsSupportedElementKind(elementKind, policyKind))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("Unsupported element kind '" + elementKind + "'.");
		}

		if (!ArchitectureConfigurationXmlEditor.TryCreateAttributes(attributes, out var xAttributes, out var message))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure(message);
		}

		var result = ArchitectureConfigurationEditExecution.EditConfiguration(
			source.Kind,
			source.Path,
			document =>
			{
				if (document.Root is null)
				{
					return ArchitectureConfigurationDocumentOperationResult.Failure("Architecture configuration has no root element.");
				}

				var container = document.Root.Elements(policyKind).FirstOrDefault();
				if (container is null)
				{
					container = new XElement(policyKind);
					document.Root.Add(container);
				}

				container.Add(new XElement(elementKind, xAttributes));

				return ArchitectureConfigurationDocumentOperationResult.Success("Added global " + policyKind + " " + elementKind + " matcher.");
			});

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddInclude(ArchitectureConfigurationSource source, string path)
	{
		if (!source.CanEdit)
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("This configuration source is not editable.");
		}

		if (string.IsNullOrWhiteSpace(path))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("Include path may not be empty.");
		}

		var result = ArchitectureConfigurationEditExecution.EditConfiguration(
			source.Kind,
			source.Path,
			document =>
			{
				if (document.Root is null)
				{
					return ArchitectureConfigurationDocumentOperationResult.Failure("Architecture configuration has no root element.");
				}

				document.Root.Add(new XElement(ArchitectureConfigurationXmlNames.IncludeElementName, new XAttribute("path", path.Trim())));

				return ArchitectureConfigurationDocumentOperationResult.Success("Added Include " + path.Trim() + ".");
			});

		return result;
	}

	internal static ArchitectureConfigurationDocumentOperationResult AddOperationContracts(ArchitectureConfigurationSource source, ImmutableDictionary<string, string> attributes, string childXml)
	{
		if (!source.CanEdit)
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("This configuration source is not editable.");
		}

		if (attributes.Keys.Any(key => key is not "description" and not "comment"))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure("Operations supports only description and comment attributes.");
		}

		if (!TryReadOperationContractChildren(childXml, out var children, out var message))
		{
			return ArchitectureConfigurationDocumentOperationResult.Failure(message);
		}

		var result = ArchitectureConfigurationEditExecution.EditConfiguration(
			source.Kind,
			source.Path,
			document =>
			{
				if (document.Root is null)
				{
					return ArchitectureConfigurationDocumentOperationResult.Failure("Architecture configuration has no root element.");
				}

				document.Root.Add(new XElement(ArchitectureConfigurationXmlNames.OperationsElementName, attributes.Select(attribute => new XAttribute(attribute.Key, attribute.Value)), children));

				return ArchitectureConfigurationDocumentOperationResult.Success("Added explicit operation contracts.");
			});

		return result;
	}

	private static bool TryReadOperationContractChildren(string childXml, out IEnumerable<XNode> children, out string message)
	{
		try
		{
			var wrapper = XElement.Parse("<Root>" + childXml + "</Root>", LoadOptions.PreserveWhitespace);
			var elements = wrapper.Elements().ToArray();
			if (elements.Length == 0 || elements.Any(element => element.Name.LocalName != "Operation"))
			{
				children = [];
				message = "Operations requires one or more Operation children.";

				return false;
			}

			children = elements;
			message = string.Empty;

			return true;
		}
		catch (System.Xml.XmlException exception)
		{
			children = [];
			message = "Operation contract XML is invalid: " + exception.Message;

			return false;
		}
	}
}
