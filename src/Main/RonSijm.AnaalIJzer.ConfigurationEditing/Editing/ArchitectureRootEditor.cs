using System.Collections.Immutable;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

internal static class ArchitectureRootEditor
{
	internal static ArchitectureConfigurationEditResult SetRootSettings(
		ArchitectureConfigurationSource source,
		string? description,
		string? requireRecognizedDependencies,
		bool enforceAcyclic,
		bool enableReport,
		string? reportPath,
		bool enableDocumentation,
		string? documentationPath)
	{
		if (!source.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("This configuration source is not editable.");
		}

		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			source.Kind,
			source.Path,
			document =>
			{
				if (document.Root is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Architecture configuration has no root element.");
				}

				ArchitectureConfigurationXmlEditor.SetOptionalAttribute(document.Root, ArchitectureConfigurationXmlNames.DescriptionAttributeName, description);
				ArchitectureConfigurationXmlEditor.SetOptionalAttribute(document.Root, ArchitectureConfigurationXmlNames.RequireRecognizedDependenciesAttributeName, requireRecognizedDependencies);
				ArchitectureConfigurationXmlEditor.SetOptionalBooleanAttribute(document.Root, "enforceAcyclic", enforceAcyclic);
				ArchitectureConfigurationXmlEditor.SetOptionalBooleanAttribute(document.Root, "enableReport", enableReport);
				ArchitectureConfigurationXmlEditor.SetOptionalAttribute(document.Root, "reportPath", reportPath);
				ArchitectureConfigurationXmlEditor.SetOptionalBooleanAttribute(document.Root, "enableDocumentation", enableDocumentation);
				ArchitectureConfigurationXmlEditor.SetOptionalAttribute(document.Root, "documentationPath", documentationPath);
				return ArchitectureConfigurationEditResult.Success("Updated root architecture settings.");
			});

		return result;
	}

	internal static ArchitectureConfigurationEditResult AddGlobalTypePolicyMatcher(ArchitectureConfigurationSource source, string policyKind, string elementKind, ImmutableDictionary<string, string> attributes)
	{
		if (policyKind is not ArchitectureConfigurationXmlNames.AllowedElementName and not ArchitectureConfigurationXmlNames.ForbiddenElementName)
		{
			return ArchitectureConfigurationEditResult.Failure("Type policy kind must be Allowed or Forbidden.");
		}

		if (!source.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("This configuration source is not editable.");
		}

		if (!ArchitectureConfigurationXmlEditor.IsSupportedElementKind(elementKind, policyKind))
		{
			return ArchitectureConfigurationEditResult.Failure("Unsupported element kind '" + elementKind + "'.");
		}

		if (!ArchitectureConfigurationXmlEditor.TryCreateAttributes(attributes, out var xAttributes, out var message))
		{
			return ArchitectureConfigurationEditResult.Failure(message);
		}

		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			source.Kind,
			source.Path,
			document =>
			{
				if (document.Root is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Architecture configuration has no root element.");
				}

				var container = document.Root.Elements(policyKind).FirstOrDefault();
				if (container is null)
				{
					container = new XElement(policyKind);
					document.Root.Add(container);
				}

				container.Add(new XElement(elementKind, xAttributes));
				return ArchitectureConfigurationEditResult.Success("Added global " + policyKind + " " + elementKind + " matcher.");
			});

		return result;
	}

	internal static ArchitectureConfigurationEditResult AddInclude(ArchitectureConfigurationSource source, string path)
	{
		if (!source.CanEdit)
		{
			return ArchitectureConfigurationEditResult.Failure("This configuration source is not editable.");
		}

		if (string.IsNullOrWhiteSpace(path))
		{
			return ArchitectureConfigurationEditResult.Failure("Include path may not be empty.");
		}

		var result = ArchitectureConfigurationDocumentStore.EditConfiguration(
			source.Kind,
			source.Path,
			document =>
			{
				if (document.Root is null)
				{
					return ArchitectureConfigurationEditResult.Failure("Architecture configuration has no root element.");
				}

				document.Root.Add(new XElement(ArchitectureConfigurationXmlNames.IncludeElementName, new XAttribute("path", path.Trim())));
				return ArchitectureConfigurationEditResult.Success("Added Include " + path.Trim() + ".");
			});

		return result;
	}
}
