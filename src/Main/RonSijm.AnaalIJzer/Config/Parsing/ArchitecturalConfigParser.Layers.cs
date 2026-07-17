using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.DependencyRules;

namespace RonSijm.AnaalIJzer.Parsing;

internal static partial class ArchitecturalConfigParser
{
	private static ImmutableArray<LayerNode> ParseLayerCollection(IEnumerable<(XElement Element, string Path)> layerElements, string parentPath, List<string> layerNames, Dictionary<string, LayerNode> nodesByPath, ImmutableDictionary<string, ImmutableHashSet<string>>.Builder layerRequiredRecognizedDependencySites, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var nodes = ImmutableArray.CreateBuilder<LayerNode>();
		var seenNames = new HashSet<string>(StringComparer.Ordinal);
		var exactAssignments = new Dictionary<string, string>(StringComparer.Ordinal);

		foreach (var (layerEl, xmlPath) in layerElements)
		{
			var configuredName = layerEl.Attribute("name")?.Value;
			if (string.IsNullOrWhiteSpace(configuredName))
			{
				continue;
			}
			var localName = configuredName!;

			if (localName.Contains('/'))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Layer name '{localName}' may not contain '/'.", layerEl, xmlPath);
				continue;
			}

			if (!seenNames.Add(localName))
			{
				var scopeDescription = string.IsNullOrEmpty(parentPath) ? "the root" : $"layer '{parentPath}'";
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Layer '{localName}' is declared more than once in {scopeDescription}.", layerEl, xmlPath);
				continue;
			}

			foreach (var classEl in layerEl.Elements("Class"))
			{
				var exactName = classEl.Attribute("typeName")?.Value ?? classEl.Attribute("exactName")?.Value;
				if (exactName is null)
				{
					continue;
				}

				if (exactAssignments.TryGetValue(exactName, out var existingLayer))
				{
					AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Exact type '{exactName}' is assigned more than once (layers '{existingLayer}' and '{localName}').", classEl, xmlPath);
				}
				else
				{
					exactAssignments.Add(exactName, localName);
				}
			}

			var canonicalPath = string.IsNullOrEmpty(parentPath) ? localName : parentPath + "/" + localName;
			if (ParseRequiredRecognizedDependencySitesAttribute(layerEl, xmlPath, $"Layer '{canonicalPath}'", issues) is { } layerSites)
			{
				layerRequiredRecognizedDependencySites[canonicalPath] = layerSites;
			}

			var definition = LayerDefinition.Normal(canonicalPath, null);
			var matchers = ImmutableArray.CreateBuilder<(PatternMatcher Matcher, MatcherRule Rule)>();
			foreach (var matcherElement in layerEl.Elements().Where(element => element.Name.LocalName is "Class" or "Namespace" or "Assembly"))
			{
				var target = matcherElement.Name.LocalName switch
				{
					"Namespace" => MatchTarget.Namespace,
					"Assembly" => MatchTarget.Assembly,
					_ => MatchTarget.TypeName
				};
				if (TryReadMatcher(matcherElement, target, out var matcher))
				{
					matchers.Add((matcher, CreateRule(matcherElement, definition, ParseExceptions(matcherElement), xmlPath)));
				}
			}

			layerNames.Add(canonicalPath);
			var children = ParseLayerCollection(layerEl.Elements("Layer").Select(element => (element, xmlPath)), canonicalPath, layerNames, nodesByPath, layerRequiredRecognizedDependencySites, issues);
			var allowedTypeMatchers = ParseTypePolicyMatchers(layerEl.Elements("Allowed").Select(element => (element, xmlPath)), definition, false);
			var forbiddenTypeMatchers = ParseTypePolicyMatchers(layerEl.Elements("Forbidden").Select(element => (element, xmlPath)), definition, true);
			if (matchers.Count == 0 && children.Length == 0)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Layer '{canonicalPath}' does not contain a matcher or nested layer.", layerEl, xmlPath);
			}

			var node = new LayerNode(definition, matchers.ToImmutable(), children, allowedTypeMatchers, forbiddenTypeMatchers);
			nodes.Add(node);
			nodesByPath[canonicalPath] = node;
		}

		return nodes.ToImmutable();
	}

	private static void ParseNestedDependencyEdges(XElement layerElement, string xmlPath, string scopePath, IReadOnlyDictionary<string, LayerNode> nodesByPath, ImmutableArray<DependencyEdge>.Builder edges, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		if (string.IsNullOrEmpty(scopePath) || !nodesByPath.ContainsKey(scopePath))
		{
			return;
		}

		ParseDependencyEdges(layerElement.Elements().Where(element => element.Name.LocalName is "AllowedDependency" or "BlockedDependency").Select(element => (element, xmlPath)), scopePath, nodesByPath, edges, issues);
		foreach (var child in layerElement.Elements("Layer"))
		{
			var childName = child.Attribute("name")?.Value;
			if (!string.IsNullOrWhiteSpace(childName))
			{
				ParseNestedDependencyEdges(child, xmlPath, scopePath + "/" + childName, nodesByPath, edges, issues);
			}
		}
	}

	private static void ParseDependencyEdges(IEnumerable<(XElement Element, string Path)> edgeElements, string scopePath, IReadOnlyDictionary<string, LayerNode> nodesByPath, ImmutableArray<DependencyEdge>.Builder edges, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		foreach (var (element, xmlPath) in edgeElements)
		{
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
