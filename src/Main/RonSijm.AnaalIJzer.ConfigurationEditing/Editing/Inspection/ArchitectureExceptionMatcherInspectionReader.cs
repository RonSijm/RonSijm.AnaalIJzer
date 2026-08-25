using System.Collections.Immutable;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Xml;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing.Inspection;

internal static class ArchitectureExceptionMatcherInspectionReader
{
	internal static ImmutableArray<ArchitectureConfigurationElementDetails> GetExceptionMatchersForLayer(XElement layerElement, ArchitectureLayerEditHandle handle)
	{
		var owners = layerElement.Elements()
			.Where(ArchitectureConfigurationXmlEditor.IsMatcherElement)
			.Concat(layerElement.Elements(ArchitectureConfigurationXmlNames.AllowedElementName).SelectMany(container => container.Elements().Where(ArchitectureConfigurationXmlEditor.IsPolicyMatcherElement)))
			.Concat(layerElement.Elements(ArchitectureConfigurationXmlNames.ForbiddenElementName).SelectMany(container => container.Elements().Where(ArchitectureConfigurationXmlEditor.IsPolicyMatcherElement)));
		var result = GetExceptionMatchers(owners, handle);

		return result;
	}

	internal static ImmutableArray<ArchitectureConfigurationElementDetails> GetExceptionMatchersForRoot(XElement rootElement, ArchitectureLayerEditHandle handle)
	{
		var owners = rootElement.Elements(ArchitectureConfigurationXmlNames.AllowedElementName)
			.SelectMany(container => container.Elements().Where(ArchitectureConfigurationXmlEditor.IsPolicyMatcherElement))
			.Concat(rootElement.Elements(ArchitectureConfigurationXmlNames.ForbiddenElementName).SelectMany(container => container.Elements().Where(ArchitectureConfigurationXmlEditor.IsPolicyMatcherElement)));
		var result = GetExceptionMatchers(owners, handle);

		return result;
	}

	private static ImmutableArray<ArchitectureConfigurationElementDetails> GetExceptionMatchers(IEnumerable<XElement> ownerElements, ArchitectureLayerEditHandle handle)
	{
		var builder = ImmutableArray.CreateBuilder<ArchitectureConfigurationElementDetails>();
		foreach (var ownerElement in ownerElements)
		{
			AddExceptionMatchers(builder, ownerElement, handle);
		}

		var result = builder.ToImmutable();

		return result;
	}

	private static void AddExceptionMatchers(ImmutableArray<ArchitectureConfigurationElementDetails>.Builder builder, XElement ownerElement, ArchitectureLayerEditHandle handle)
	{
		var exceptions = ownerElement.Element(ArchitectureConfigurationXmlNames.ExceptionsElementName);
		if (exceptions is null)
		{
			return;
		}

		foreach (var matcher in EnumerateExceptionMatchers(exceptions))
		{
			builder.Add(ArchitectureConfigurationXmlEditor.CreateElementDetails(matcher, handle, ArchitectureConfigurationXmlNames.ExceptionsElementName));
		}
	}

	private static IEnumerable<XElement> EnumerateExceptionMatchers(XElement exceptionsContainer)
	{
		foreach (var matcher in exceptionsContainer.Elements().Where(ArchitectureConfigurationXmlEditor.IsMatcherElement))
		{
			yield return matcher;

			var nestedExceptions = matcher.Element(ArchitectureConfigurationXmlNames.ExceptionsElementName);
			if (nestedExceptions is null)
			{
				continue;
			}

			foreach (var nestedMatcher in EnumerateExceptionMatchers(nestedExceptions))
			{
				yield return nestedMatcher;
			}
		}
	}
}
