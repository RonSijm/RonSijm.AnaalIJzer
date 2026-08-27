using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;

internal static partial class ArchitectureConfigurationXmlNavigator
{
	private static IEnumerable<XElement> GetConfigurationElementCandidates(XDocument document, ArchitectureConfigurationElementEditHandle handle)
	{
		var containerRoot = GetConfigurationElementContainerRoot(document, handle);
		if (containerRoot is null)
		{
			return [];
		}

		if (handle.ContainerKind == ArchitectureConfigurationXmlNames.IncludeElementName)
		{
			var result = containerRoot
				.Elements(ArchitectureConfigurationXmlNames.IncludeElementName)
				.Where(element => string.Equals(element.Name.LocalName, handle.ElementKind, StringComparison.Ordinal));

			return result;
		}

		if (handle.ContainerKind == "LayerMatcher")
		{
			var result = containerRoot
				.Elements()
				.Where(element => string.Equals(element.Name.LocalName, handle.ElementKind, StringComparison.Ordinal) && ArchitectureConfigurationXmlEditor.IsMatcherElement(element));

			return result;
		}

		if (handle.ContainerKind == ArchitectureConfigurationXmlNames.ExceptionsElementName)
		{
			var result = GetExceptionElementCandidates(containerRoot, string.IsNullOrWhiteSpace(handle.LayerPath))
				.Where(element => string.Equals(element.Name.LocalName, handle.ElementKind, StringComparison.Ordinal));

			return result;
		}

		if (handle.ContainerKind is ArchitectureConfigurationXmlNames.AllowedElementName or ArchitectureConfigurationXmlNames.ForbiddenElementName)
		{
			var result = containerRoot
				.Elements(handle.ContainerKind)
				.SelectMany(container => container.Elements())
				.Where(element => string.Equals(element.Name.LocalName, handle.ElementKind, StringComparison.Ordinal));

			return result;
		}

		if (handle.ContainerKind == ArchitectureConfigurationXmlNames.NameRulesElementName)
		{
			var result = containerRoot
				.Elements(ArchitectureConfigurationXmlNames.NameRulesElementName)
				.SelectMany(container => container.Elements())
				.Where(element => string.Equals(element.Name.LocalName, handle.ElementKind, StringComparison.Ordinal));

			return result;
		}

		if (handle.ContainerKind == ArchitectureConfigurationXmlNames.VisibilityPolicyElementName)
		{
			var result = containerRoot
				.Elements(ArchitectureConfigurationXmlNames.VisibilityPolicyElementName)
				.Where(element => string.Equals(element.Name.LocalName, handle.ElementKind, StringComparison.Ordinal));

			return result;
		}

		if (handle.ContainerKind == ArchitectureConfigurationXmlNames.InheritancePolicyElementName)
		{
			var result = containerRoot
				.Elements(ArchitectureConfigurationXmlNames.InheritancePolicyElementName)
				.Where(element => string.Equals(element.Name.LocalName, handle.ElementKind, StringComparison.Ordinal));

			return result;
		}

		if (handle.ContainerKind == ArchitectureConfigurationXmlNames.ApiSurfaceElementName)
		{
			var result = containerRoot
				.Elements(ArchitectureConfigurationXmlNames.ApiSurfaceElementName)
				.Where(element => string.Equals(element.Name.LocalName, handle.ElementKind, StringComparison.Ordinal));

			return result;
		}

		return [];
	}

	private static XElement? GetConfigurationElementContainerRoot(XDocument document, ArchitectureConfigurationElementEditHandle handle)
	{
		if (string.IsNullOrWhiteSpace(handle.LayerPath))
		{
			return document.Root;
		}

		var layerHandle = new ArchitectureLayerEditHandle(handle.SourceKind, handle.SourcePath, 0, handle.LayerPath, string.Empty, GetParentPath(handle.LayerPath), null);
		var result = FindLayerElement(document, layerHandle);

		return result;
	}
}
