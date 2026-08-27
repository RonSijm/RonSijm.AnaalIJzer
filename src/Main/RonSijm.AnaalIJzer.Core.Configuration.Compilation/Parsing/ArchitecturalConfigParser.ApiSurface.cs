using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.ApiSurface.Engine.Policies;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
	private static ImmutableArray<ApiSurfacePolicy> ParseApiSurfacePolicies(IEnumerable<XElement> policyElements, string ownerLayerPath, string xmlPath, ISet<string> declaredLayerPaths, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var policies = ImmutableArray.CreateBuilder<ApiSurfacePolicy>();
		foreach (var element in policyElements)
		{
			if (!TryReadBooleanAttribute(element, "requireRecognizedTypes", out var requireRecognizedTypes))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "ApiSurface contains an invalid requireRecognizedTypes value. Use true, false, 1, or 0.", element, xmlPath);
				continue;
			}

			var allowedLayers = ParseApiSurfaceLayerRules(element.Elements("AllowedLayer"), ownerLayerPath, xmlPath, declaredLayerPaths, issues);
			var blockedLayers = ParseApiSurfaceLayerRules(element.Elements("BlockedLayer"), ownerLayerPath, xmlPath, declaredLayerPaths, issues);
			if (allowedLayers.Length == 0 && blockedLayers.Length == 0)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "ApiSurface requires at least one valid AllowedLayer or BlockedLayer.", element, xmlPath);
				continue;
			}

			var transitiveExposure = ParseTransitiveExposure(element, xmlPath, issues);
			var line = (IXmlLineInfo)element;
			policies.Add(new ApiSurfacePolicy(
				ownerLayerPath,
				requireRecognizedTypes,
				allowedLayers,
				blockedLayers,
				transitiveExposure,
				element.Attribute("description")?.Value,
				xmlPath,
				line.HasLineInfo() ? line.LineNumber : 0,
				line.HasLineInfo() ? line.LinePosition : 0));
		}

		return policies.ToImmutable();
	}

	private static ImmutableArray<ApiSurfaceLayerRule> ParseApiSurfaceLayerRules(IEnumerable<XElement> ruleElements, string ownerLayerPath, string xmlPath, ISet<string> declaredLayerPaths, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var rules = ImmutableArray.CreateBuilder<ApiSurfaceLayerRule>();
		foreach (var element in ruleElements)
		{
			var configuredPath = element.Attribute("path")?.Value;
			if (string.IsNullOrWhiteSpace(configuredPath))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} requires a path.", element, xmlPath);
				continue;
			}

			if (!TryResolveApiSurfaceLayerReference(configuredPath!, ownerLayerPath, declaredLayerPaths, out var resolvedPath, out var pathError))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} {pathError}", element, xmlPath);
				continue;
			}

			if (!TryReadSiteFilter(element, out var siteFilter, out var siteFilterError))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, siteFilterError, element, xmlPath);
				continue;
			}

			var line = (IXmlLineInfo)element;
			rules.Add(new ApiSurfaceLayerRule(
				resolvedPath,
				configuredPath!,
				siteFilter,
				element.Attribute("description")?.Value,
				xmlPath,
				line.HasLineInfo() ? line.LineNumber : 0,
				line.HasLineInfo() ? line.LinePosition : 0));
		}

		return rules.ToImmutable();
	}

	private static TransitiveExposureOptions? ParseTransitiveExposure(XElement policyElement, string xmlPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var elements = policyElement.Elements("TransitiveExposure").ToArray();
		if (elements.Length == 0)
		{
			return null;
		}

		if (elements.Length > 1)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "ApiSurface may contain at most one TransitiveExposure element.", elements[1], xmlPath);
			return null;
		}

		var element = elements[0];
		var value = element.Attribute("maxDepth")?.Value;
		var maxDepth = TransitiveExposureOptions.DefaultMaxDepth;
		if (value is not null
		    && (!int.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out maxDepth)
		        || maxDepth < 1
		        || maxDepth > TransitiveExposureOptions.MaximumMaxDepth))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"TransitiveExposure maxDepth must be an integer from 1 through {TransitiveExposureOptions.MaximumMaxDepth}.", element, xmlPath);
			return null;
		}

		var line = (IXmlLineInfo)element;
		var result = new TransitiveExposureOptions(
			maxDepth,
			element.Attribute("description")?.Value,
			xmlPath,
			line.HasLineInfo() ? line.LineNumber : 0,
			line.HasLineInfo() ? line.LinePosition : 0);

		return result;
	}

	private static bool TryResolveApiSurfaceLayerReference(string reference, string ownerLayerPath, ISet<string> declaredLayerPaths, out string resolvedPath, out string error)
	{
		if (reference.StartsWith("/", StringComparison.Ordinal))
		{
			resolvedPath = reference.TrimStart('/');
		}
		else if (reference.Contains('/'))
		{
			resolvedPath = string.Empty;
			error = $"layer path '{reference}' must start with '/'.";
			return false;
		}
		else
		{
			var parentPath = ownerLayerPath.Contains('/')
				? ownerLayerPath.Substring(0, ownerLayerPath.LastIndexOf('/'))
				: string.Empty;
			resolvedPath = string.IsNullOrEmpty(parentPath) ? reference : parentPath + "/" + reference;
		}

		if (resolvedPath.Length == 0 || !declaredLayerPaths.Contains(resolvedPath))
		{
			error = $"references unknown layer '{reference}'.";
			return false;
		}

		error = string.Empty;
		return true;
	}
}

