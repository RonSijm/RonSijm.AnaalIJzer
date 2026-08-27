using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.LayerModel;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;

public static partial class ArchitecturalConfigParser
{
	internal static void ParseNestedDependencyEdges(XElement layerElement, string xmlPath, string scopePath, IReadOnlyDictionary<string, LayerNode> nodesByPath, ImmutableArray<DependencyEdge>.Builder edges, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		if (string.IsNullOrEmpty(scopePath) || !nodesByPath.ContainsKey(scopePath))
		{
			return;
		}

		ParseDependencyEdges(layerElement.Elements().Where(element => element.Name.LocalName is "AllowedDependency" or "BlockedDependency").Select(element => new ArchitectureConfigurationElementInput(element, xmlPath, false)), scopePath, nodesByPath, edges, issues);
		foreach (var child in layerElement.Elements("Layer"))
		{
			var childName = child.Attribute("name")?.Value;
			if (!string.IsNullOrWhiteSpace(childName))
			{
				ParseNestedDependencyEdges(child, xmlPath, scopePath + "/" + childName, nodesByPath, edges, issues);
			}
		}
	}

	internal static void ParseDependencyEdges(IEnumerable<ArchitectureConfigurationElementInput> edgeElements, string scopePath, IReadOnlyDictionary<string, LayerNode> nodesByPath, ImmutableArray<DependencyEdge>.Builder edges, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		foreach (var edgeInput in edgeElements)
		{
			var element = edgeInput.Element;
			var xmlPath = edgeInput.Path;
			var configuredFrom = element.Attribute("from")?.Value;
			var configuredTo = element.Attribute("to")?.Value;
			if (configuredFrom is null || configuredTo is null)
			{
				continue;
			}

			if (!TryReadSiteFilter(element, out var siteFilter, out var siteFilterError))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, siteFilterError, element, xmlPath);
				continue;
			}

			if (!TryReadBooleanAttribute(element, "appliesToDescendants", out var appliesToDescendants))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} contains an invalid appliesToDescendants value. Use true, false, 1, or 0.", element, xmlPath);
				continue;
			}

			if (!TryResolveLayerReference(configuredFrom, scopePath, nodesByPath, out var from, out var fromError))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} source {fromError}", element, xmlPath);
				continue;
			}

			if (!TryResolveLayerReference(configuredTo, scopePath, nodesByPath, out var to, out var toError))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} target {toError}", element, xmlPath);
				continue;
			}

			var line = (IXmlLineInfo)element;
			edges.Add(new DependencyEdge(scopePath, from, to, configuredFrom, configuredTo, siteFilter, appliesToDescendants, element.Name.LocalName == "BlockedDependency" ? DependencyRuleKind.Blocked : DependencyRuleKind.Allowed, xmlPath, line.HasLineInfo() ? line.LineNumber : 0, line.HasLineInfo() ? line.LinePosition : 0));
		}
	}

	private static bool TryResolveLayerReference(string reference, string scopePath, IReadOnlyDictionary<string, LayerNode> nodesByPath, out string resolved, out string error)
	{
		if (reference == "*")
		{
			resolved = reference;
			error = string.Empty;
			return true;
		}

		if (reference.StartsWith("/", StringComparison.Ordinal))
		{
			resolved = reference.TrimStart('/');
		}
		else if (reference.Contains('/'))
		{
			resolved = string.Empty;
			error = $"layer path '{reference}' must start with '/'.";
			return false;
		}
		else
		{
			resolved = string.IsNullOrEmpty(scopePath) ? reference : scopePath + "/" + reference;
		}

		if (resolved.Length == 0 || !nodesByPath.ContainsKey(resolved))
		{
			error = $"references unknown layer '{reference}'.";
			return false;
		}

		error = string.Empty;
		return true;
	}
}

