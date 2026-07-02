using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Diagnostics;
using RonSijm.AnaalIJzer.Matching;

namespace RonSijm.AnaalIJzer.Config;

/// <summary>
///     Parses an <c>ArchitecturalLevels.xml</c> additional file or inline
///     <c>AssemblyMetadata("AnaalIJzerSettings", ...)</c> value into an <see cref="AnalyzerConfig" />.
/// </summary>
internal static class ArchitecturalConfigParser
{
	internal const string ConfigFileName = "ArchitecturalLevels.xml";
	internal const string InlineSettingsMetadataKey = "AnaalIJzerSettings";
	private static readonly Lazy<XmlSchemaSet> ConfigurationSchemas = new(CreateConfigurationSchemas);

	internal static AnalyzerConfig Parse(ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken) =>
		Parse(additionalFiles, null, cancellationToken);

	internal static AnalyzerConfig Parse(ImmutableArray<AdditionalText> additionalFiles, Compilation? compilation, CancellationToken cancellationToken)
	{
		return Parse(additionalFiles, compilation, null, cancellationToken);
	}

	internal static AnalyzerConfig Parse(ImmutableArray<AdditionalText> additionalFiles, Compilation? compilation, string? inlineConfigPath, CancellationToken cancellationToken)
	{
		var (content, path) = ReadConfigXml(additionalFiles, compilation, inlineConfigPath, cancellationToken);
		if (content is null || string.IsNullOrWhiteSpace(content))
		{
			return AnalyzerConfig.Empty;
		}

		return ParseXml(content, path, additionalFiles, cancellationToken);
	}

	internal static AnalyzerConfig ParseFile(AdditionalText configFile, ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken)
	{
		var content = configFile.GetText(cancellationToken)?.ToString();
		return string.IsNullOrWhiteSpace(content) ? AnalyzerConfig.Empty : ParseXml(content!, configFile.Path, additionalFiles, cancellationToken);
	}

	private static (string? Content, string Path) ReadConfigXml(ImmutableArray<AdditionalText> additionalFiles, Compilation? compilation, string? inlineConfigPath, CancellationToken cancellationToken)
	{
		var configFile = additionalFiles.FirstOrDefault(f => Path.GetFileName(f.Path).Equals(ConfigFileName, StringComparison.OrdinalIgnoreCase));
		if (configFile is null)
		{
			return ReadInlineConfigXml(compilation, inlineConfigPath);
		}

		return (configFile.GetText(cancellationToken)?.ToString(), configFile.Path);
	}

	private static (string? Content, string Path) ReadInlineConfigXml(Compilation? compilation, string? inlineConfigPath)
	{
		if (compilation is null)
		{
			return (null, inlineConfigPath ?? InlineSettingsMetadataKey);
		}

		foreach (var attribute in compilation.Assembly.GetAttributes())
		{
			if (!IsAssemblyMetadataAttribute(attribute.AttributeClass))
			{
				continue;
			}

			if (attribute.ConstructorArguments.Length >= 2
			    && string.Equals(attribute.ConstructorArguments[0].Value as string, InlineSettingsMetadataKey, StringComparison.Ordinal)
			    && attribute.ConstructorArguments[1].Value is string xml)
			{
				return (xml, inlineConfigPath ?? InlineSettingsMetadataKey);
			}
		}

		return (null, inlineConfigPath ?? InlineSettingsMetadataKey);
	}

	private static bool IsAssemblyMetadataAttribute(INamedTypeSymbol? attributeClass) =>
		attributeClass is not null
		&& string.Equals(attributeClass.Name, "AssemblyMetadataAttribute", StringComparison.Ordinal)
		&& string.Equals(attributeClass.ContainingNamespace?.ToDisplayString(), "System.Reflection", StringComparison.Ordinal);

	private static AnalyzerConfig ParseXml(string content, string configPath, ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken)
	{
		var issues = ImmutableArray.CreateBuilder<ConfigurationIssue>();
		try
		{
			var additionalFileLookup = BuildAdditionalFileLookup(additionalFiles);
			var documents = new List<(XElement Root, string Path)>();
			var elements = new List<(XElement Element, string Path)>();
			var documentationItems = ImmutableArray.CreateBuilder<ArchitectureDocumentationItem>();
			CollectConfig(content, configPath, additionalFileLookup, cancellationToken, documents, elements, documentationItems, issues, new HashSet<string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase));

			if (documents.Count == 0)
			{
				return issues.Count == 0 ? AnalyzerConfig.Empty : CreateInvalidConfig(issues.ToImmutable());
			}

			var strict = documents.Any(d => IsEnabled(d.Root, "strict"));
			var enforceAcyclic = documents.Any(d => IsEnabled(d.Root, "enforceAcyclic"));

			var enableReport = TryFindEnabledDocument(documents, "enableReport", out var reportRoot, out var reportConfigPath);

			var reportPath = ResolveRelativePath(
				reportRoot?.Attribute("reportPath")?.Value ?? "architectural-violations.md", reportConfigPath ?? configPath);

			var enableDocumentation = TryFindEnabledDocument(documents, "enableDocumentation", out var documentationRoot, out var documentationConfigPath);

			var documentationPath = ResolveRelativePath(
				documentationRoot?.Attribute("documentationPath")?.Value ?? "architecture-documentation.md",
				documentationConfigPath ?? configPath);

			var forbiddenTypeNames = new Dictionary<string, MatcherRule>(StringComparer.Ordinal);
			var forbiddenMatchers = new List<(PatternMatcher Matcher, MatcherRule Rule)>();
			var layerNames = new List<string>();
			var layerNodesByPath = new Dictionary<string, LayerNode>(StringComparer.Ordinal);
			var forbiddenPatterns = new List<(string Name, string? Comment)>();
			var rootLayerElements = elements.Where(item => item.Element.Name.LocalName == "Layer").ToArray();
			var roots = ParseLayerCollection(rootLayerElements, string.Empty, layerNames, layerNodesByPath, issues);
			var allowedTypeMatchers = ParseTypePolicyMatchers(elements.Where(item => item.Element.Name.LocalName == "Allowed"), LayerDefinition.Normal("global", null), false);

			foreach (var (forbiddenContainer, xmlPath) in elements.Where(e => e.Element.Name.LocalName == "Forbidden"))
			{
				foreach (var classEl in forbiddenContainer.Elements("Class"))
				{
					var comment = classEl.Attribute("comment")?.Value;
					var fixSuffix = classEl.Element("Fix")?.Attribute("Rename")?.Value;
					var forbiddenName = GetForbiddenDisplayName(classEl) ?? "Forbidden";
					var def = LayerDefinition.Forbidden(forbiddenName, comment, fixSuffix);
					ParseClassElement(classEl, def, xmlPath, forbiddenTypeNames, forbiddenMatchers);
					forbiddenPatterns.Add((forbiddenName, comment));
				}

				foreach (var nsEl in forbiddenContainer.Elements("Namespace"))
				{
					var comment = nsEl.Attribute("comment")?.Value;
					var forbiddenName = GetForbiddenDisplayName(nsEl) ?? "ForbiddenNamespace";
					var def = LayerDefinition.Forbidden(forbiddenName, comment, null);
					ParseNamespaceElement(nsEl, def, xmlPath, forbiddenMatchers);
					forbiddenPatterns.Add((forbiddenName, comment));
				}
			}

			var edgeBuilder = ImmutableArray.CreateBuilder<DependencyEdge>();
			ParseDependencyEdges(elements.Where(item => item.Element.Name.LocalName is "AllowedDependency" or "BlockedDependency"), string.Empty, layerNodesByPath, edgeBuilder, issues);
			foreach (var rootLayer in rootLayerElements)
			{
				ParseNestedDependencyEdges(rootLayer.Element, rootLayer.Path, rootLayer.Element.Attribute("name")?.Value ?? string.Empty, layerNodesByPath, edgeBuilder, issues);
			}

			var registry = new LayerRegistry(roots, layerNodesByPath, forbiddenTypeNames, [..forbiddenMatchers], allowedTypeMatchers);
			var edges = edgeBuilder.ToImmutable();
			var graph = new DependencyGraph(edges);
			if (enforceAcyclic)
			{
				foreach (var cycle in DependencyCycleDetector.FindConfiguredCycles(layerNames, edges))
				{
					var firstEdge = edges.FirstOrDefault(edge => edge.IsAllowed && edge.From == cycle[0] && edge.To == cycle[1]);
					var message = $"Configured allowed-dependency cycle: {string.Join(" -> ", cycle)} -> {cycle[0]}.";
					issues.Add(new ConfigurationIssue(ConfigurationIssueKind.CyclicDependencyGraph, message, firstEdge.XmlPath ?? configPath, firstEdge.XmlLineNumber, firstEdge.XmlLinePosition));
				}
			}
			var outputConfig = new OutputConfig(enableReport, reportPath, enableDocumentation, documentationPath);
			var documentation = new ArchitectureDocumentation(documents.Select(document => document.Root.Attribute("description")?.Value).FirstOrDefault(description => !string.IsNullOrWhiteSpace(description)), documentationItems.ToImmutable());
			return new AnalyzerConfig(registry, graph, outputConfig, strict, enforceAcyclic, roots, [..layerNames], [..forbiddenPatterns], documentation, issues.ToImmutable());
		}
		catch (XmlException ex)
		{
			return AnalyzerConfig.Invalid(new ConfigurationIssue(ConfigurationIssueKind.InvalidConfiguration, $"Invalid architecture XML: {ex.Message}", configPath, ex.LineNumber, ex.LinePosition));
		}
		catch (Exception ex)
		{
			return AnalyzerConfig.Invalid(new ConfigurationIssue(ConfigurationIssueKind.InvalidConfiguration, $"Could not read architecture configuration: {ex.Message}", configPath, 0, 0));
		}
	}

	private static ImmutableArray<LayerNode> ParseLayerCollection(IEnumerable<(XElement Element, string Path)> layerElements, string parentPath, List<string> layerNames, Dictionary<string, LayerNode> nodesByPath, ImmutableArray<ConfigurationIssue>.Builder issues)
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
			var children = ParseLayerCollection(layerEl.Elements("Layer").Select(element => (element, xmlPath)), canonicalPath, layerNames, nodesByPath, issues);
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
			edges.Add(new DependencyEdge(scopePath, from, to, configuredFrom, configuredTo, siteFilter, element.Name.LocalName == "BlockedDependency" ? DependencyRuleKind.Blocked : DependencyRuleKind.Allowed, xmlPath, line.HasLineInfo() ? line.LineNumber : 0, line.HasLineInfo() ? line.LinePosition : 0));
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

	private static string ResolveRelativePath(string path, string configFilePath)
	{
		if (Path.IsPathRooted(path))
		{
			return path;
		}

		var configDir = Path.GetDirectoryName(configFilePath);
		return configDir is null ? path : Path.Combine(configDir, path);
	}

	private static Dictionary<string, AdditionalText> BuildAdditionalFileLookup(ImmutableArray<AdditionalText> additionalFiles)
	{
		var lookup = new Dictionary<string, AdditionalText>(StringComparer.OrdinalIgnoreCase);
		foreach (var file in additionalFiles)
		{
			lookup[NormalizePath(file.Path)] = file;

			var fileName = Path.GetFileName(file.Path);
			if (!string.IsNullOrEmpty(fileName) && !lookup.ContainsKey(fileName))
			{
				lookup[fileName] = file;
			}
		}

		return lookup;
	}

	private static bool TryFindIncludedFile(IReadOnlyDictionary<string, AdditionalText> additionalFileLookup, string resolvedPath, string includePath, bool allowFileNameFallback, out AdditionalText includeFile)
	{
		if (additionalFileLookup.TryGetValue(NormalizePath(resolvedPath), out includeFile!))
		{
			return true;
		}

		if (!allowFileNameFallback)
		{
			return false;
		}

		if (additionalFileLookup.TryGetValue(includePath, out includeFile!))
		{
			return true;
		}

		var fileName = Path.GetFileName(includePath);
		return !string.IsNullOrEmpty(fileName) && additionalFileLookup.TryGetValue(fileName, out includeFile!);
	}

	private static void CollectConfig(string content, string configPath, IReadOnlyDictionary<string, AdditionalText> additionalFileLookup, CancellationToken cancellationToken, List<(XElement Root, string Path)> documents, List<(XElement Element, string Path)> elements, ImmutableArray<ArchitectureDocumentationItem>.Builder documentationItems, ImmutableArray<ConfigurationIssue>.Builder issues, HashSet<string> activePaths, HashSet<string> visitedPaths)
	{
		var normalizedPath = NormalizePath(configPath);
		if (!activePaths.Add(normalizedPath))
		{
			return;
		}

		if (!visitedPaths.Add(normalizedPath))
		{
			activePaths.Remove(normalizedPath);
			return;
		}

		// SetLineInfo lets us recover the originating XML element location for each
		// rule, which the "Add to exceptions" code fix uses to find the matcher
		// element that owns the <Exceptions> block.
		var doc = XDocument.Parse(content, LoadOptions.SetLineInfo);
		ValidateDocument(doc, configPath, issues);
		if (doc.Root is null)
		{
			activePaths.Remove(normalizedPath);
			return;
		}

		documents.Add((doc.Root, configPath));

		foreach (var child in doc.Root.Elements())
		{
			AddDocumentationItems(child, configPath, 0, string.Empty, documentationItems);

			if (child.Name.LocalName != "Include")
			{
				elements.Add((child, configPath));
				continue;
			}

			if (child.Attribute("path")?.Value is not { } includePath || string.IsNullOrWhiteSpace(includePath))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Include requires a non-empty path.", child, configPath);
				continue;
			}

			var resolvedPath = ResolveRelativePath(includePath, configPath);
			var allowFileNameFallback = string.Equals(configPath, InlineSettingsMetadataKey, StringComparison.Ordinal)
			                            || string.IsNullOrEmpty(Path.GetDirectoryName(configPath));
			if (!TryFindIncludedFile(additionalFileLookup, resolvedPath, includePath, allowFileNameFallback, out var includeFile))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Included architecture configuration was not provided as an AdditionalFile: {includePath}.", child, configPath);
				continue;
			}

			var includeText = includeFile.GetText(cancellationToken);
			var includeContent = includeText?.ToString();
			if (string.IsNullOrWhiteSpace(includeContent))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Included architecture configuration is empty: {includePath}.", child, configPath);
				continue;
			}

			CollectConfig(includeContent!, includeFile.Path, additionalFileLookup, cancellationToken, documents, elements, documentationItems, issues, activePaths, visitedPaths);
		}

		activePaths.Remove(normalizedPath);
	}

	private static void AddDocumentationItems(XElement el, string configPath, int depth, string parentLayerPath, ImmutableArray<ArchitectureDocumentationItem>.Builder documentationItems)
	{
		var layerPath = parentLayerPath;
		if (el.Name.LocalName == "Layer" && el.Attribute("name")?.Value is { } layerName)
		{
			layerPath = string.IsNullOrEmpty(parentLayerPath) ? layerName : parentLayerPath + "/" + layerName;
		}

		documentationItems.Add(CreateDocumentationItem(el, configPath, depth, layerPath));

		foreach (var child in el.Elements())
		{
			AddDocumentationItems(child, configPath, depth + 1, layerPath, documentationItems);
		}
	}

	private static ArchitectureDocumentationItem CreateDocumentationItem(XElement el, string configPath, int depth, string layerPath)
	{
		var attributes = el.Attributes()
			.Where(attribute => !attribute.IsNamespaceDeclaration
			                    && !string.Equals(attribute.Name.LocalName, "description", StringComparison.Ordinal)
			                    && !string.Equals(attribute.Name.LocalName, "comment", StringComparison.Ordinal))
			.Select(attribute => new ArchitectureDocumentationAttribute(attribute.Name.LocalName, attribute.Value))
			.ToImmutableArray();

		var line = (IXmlLineInfo)el;
		return new ArchitectureDocumentationItem(el.Name.LocalName, GetDocumentationLabel(el), el.Attribute("description")?.Value, el.Attribute("comment")?.Value, attributes, depth, layerPath, configPath, line.HasLineInfo() ? line.LineNumber : 0);
	}

	private static string GetDocumentationLabel(XElement el)
	{
		return el.Name.LocalName switch
		{
			"Include" => el.Attribute("path")?.Value ?? "Include",
			"Layer" => el.Attribute("name")?.Value ?? "Layer",
			"Class" => "Class " + (GetMatcherDisplayName(el) ?? "(no matcher)"),
			"Namespace" => "Namespace " + (GetMatcherDisplayName(el) ?? "(no matcher)"),
			"Assembly" => "Assembly " + (GetMatcherDisplayName(el) ?? "(no matcher)"),
			"AllowedDependency" => $"{el.Attribute("from")?.Value ?? "?"} -> {el.Attribute("to")?.Value ?? "?"}",
			"BlockedDependency" => $"{el.Attribute("from")?.Value ?? "?"} -x-> {el.Attribute("to")?.Value ?? "?"}",
			"Fix" => "Fix " + (el.Attribute("Rename")?.Value ?? string.Empty),
			_ => el.Name.LocalName
		};
	}

	private static string? GetMatcherDisplayName(XElement el)
	{
		var attribute = el.Attributes()
			.FirstOrDefault(a => a.Name.LocalName is "typeName" or "exactName" or "exactFullName" or "inherits" or "implements" or "withAttribute" or "withAccessModifier" or "endsWith" or "startsWith" or "contains" or "regex");

		return attribute is null ? null : $"{attribute.Name.LocalName}=\"{attribute.Value}\"";
	}

	private static string NormalizePath(string path)
	{
		try
		{
			return Path.GetFullPath(path);
		}
		catch
		{
			return path;
		}
	}

	private static bool IsEnabled(XElement root, string attributeName) =>
		string.Equals(root.Attribute(attributeName)?.Value, "true", StringComparison.OrdinalIgnoreCase);

	private static bool TryReadSiteFilter(XElement el, out DependencySiteFilter siteFilter, out string error)
	{
		var allowedSitesText = el.Attribute("allowedSites")?.Value;
		var blockedSitesText = el.Attribute("blockedSites")?.Value;

		if (allowedSitesText is not null && blockedSitesText is not null)
		{
			siteFilter = DependencySiteFilter.All;
			error = $"{el.Name.LocalName} may use allowedSites or blockedSites, but not both.";
			return false;
		}

		if (allowedSitesText is not null)
		{
			if (!TryParseSiteList(allowedSitesText, out var allowedSites))
			{
				siteFilter = DependencySiteFilter.All;
				error = $"{el.Name.LocalName} contains an empty or unknown allowedSites value.";
				return false;
			}

			siteFilter = new DependencySiteFilter(allowedSites, ImmutableHashSet<string>.Empty);
			error = string.Empty;
			return true;
		}

		if (blockedSitesText is not null)
		{
			if (!TryParseSiteList(blockedSitesText, out var blockedSites))
			{
				siteFilter = DependencySiteFilter.All;
				error = $"{el.Name.LocalName} contains an empty or unknown blockedSites value.";
				return false;
			}

			siteFilter = new DependencySiteFilter(ImmutableHashSet<string>.Empty, blockedSites);
			error = string.Empty;
			return true;
		}

		siteFilter = DependencySiteFilter.All;
		error = string.Empty;
		return true;
	}

	private static bool TryParseSiteList(string value, out ImmutableHashSet<string> sites)
	{
		var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);

		foreach (var rawToken in value.Split(','))
		{
			var token = rawToken.Trim();
			if (token.Length == 0)
			{
				continue;
			}

			if (!DependencySites.TryNormalize(token, out var normalized))
			{
				sites = ImmutableHashSet<string>.Empty;
				return false;
			}

			builder.Add(normalized);
		}

		sites = builder.ToImmutable();
		return sites.Count > 0;
	}

	private static bool TryFindEnabledDocument(List<(XElement Root, string Path)> documents, string attributeName, out XElement? root, out string? path)
	{
		foreach (var document in documents)
		{
			if (!IsEnabled(document.Root, attributeName))
			{
				continue;
			}

			root = document.Root;
			path = document.Path;
			return true;
		}

		root = null;
		path = null;
		return false;
	}

	private static void ParseClassElement(XElement classEl, LayerDefinition def, string xmlPath, Dictionary<string, MatcherRule> typeNameLayers, List<(PatternMatcher, MatcherRule)> matchers)
	{
		var exceptions = ParseExceptions(classEl);
		var rule = CreateRule(classEl, def, exceptions, xmlPath);

		// Exact-name attributes (typeName / exactName) go into the fast-path dictionary.
		var exactName = classEl.Attribute("typeName")?.Value ?? classEl.Attribute("exactName")?.Value;
		if (exactName is not null)
		{
			typeNameLayers[exactName] = rule;
			return;
		}

		if (TryReadMatcher(classEl, MatchTarget.TypeName, out var matcher))
		{
			matchers.Add((matcher, rule));
		}
	}

	private static void ParseNamespaceElement(XElement nsEl, LayerDefinition def, string xmlPath, List<(PatternMatcher, MatcherRule)> matchers)
	{
		var exceptions = ParseExceptions(nsEl);
		var rule = CreateRule(nsEl, def, exceptions, xmlPath);

		if (TryReadMatcher(nsEl, MatchTarget.Namespace, out var matcher))
		{
			matchers.Add((matcher, rule));
		}
	}

	private static void ParseAssemblyElement(XElement assemblyEl, LayerDefinition def, string xmlPath, List<(PatternMatcher, MatcherRule)> matchers)
	{
		var exceptions = ParseExceptions(assemblyEl);
		var rule = CreateRule(assemblyEl, def, exceptions, xmlPath);

		if (TryReadMatcher(assemblyEl, MatchTarget.Assembly, out var matcher))
		{
			matchers.Add((matcher, rule));
		}
	}

	private static ImmutableArray<(PatternMatcher Matcher, MatcherRule Rule)> ParseTypePolicyMatchers(IEnumerable<(XElement Element, string Path)> containers, LayerDefinition scope, bool forbidden)
	{
		var matchers = ImmutableArray.CreateBuilder<(PatternMatcher Matcher, MatcherRule Rule)>();
		foreach (var (container, xmlPath) in containers)
		{
			foreach (var element in container.Elements().Where(element => element.Name.LocalName is "Class" or "Namespace"))
			{
				var target = element.Name.LocalName == "Namespace" ? MatchTarget.Namespace : MatchTarget.TypeName;
				if (!TryReadMatcher(element, target, out var matcher))
				{
					continue;
				}

				var definition = scope;
				if (forbidden)
				{
					var displayName = GetForbiddenDisplayName(element) ?? "Forbidden";
					definition = LayerDefinition.Forbidden(displayName, element.Attribute("comment")?.Value, element.Element("Fix")?.Attribute("Rename")?.Value);
				}

				matchers.Add((matcher, CreateRule(element, definition, ParseExceptions(element), xmlPath)));
			}
		}

		return matchers.ToImmutable();
	}

	private static MatcherRule CreateRule(XElement el, LayerDefinition def, ImmutableArray<ExceptionMatcher> exceptions, string xmlPath)
	{
		var line = (IXmlLineInfo)el;
		var hasInfo = line.HasLineInfo();
		return new MatcherRule(def, exceptions, hasInfo ? line.LineNumber : 0, hasInfo ? line.LinePosition : 0, xmlPath);
	}

	/// <summary>
	///     Parses any <c>&lt;Exceptions&gt;</c> child of <paramref name="ruleEl" /> into a tree.
	///     Nested exceptions flip the previous exception level again, with the deepest matching
	///     level winning.
	/// </summary>
	private static ImmutableArray<ExceptionMatcher> ParseExceptions(XElement ruleEl)
	{
		var exceptionsContainer = ruleEl.Element("Exceptions");
		if (exceptionsContainer is null)
		{
			return ImmutableArray<ExceptionMatcher>.Empty;
		}

		var builder = ImmutableArray.CreateBuilder<ExceptionMatcher>();

		foreach (var exEl in exceptionsContainer.Elements("Class"))
		{
			if (TryReadMatcher(exEl, MatchTarget.TypeName, out var m))
			{
				builder.Add(new ExceptionMatcher(m, ParseExceptions(exEl)));
			}
		}

		foreach (var exEl in exceptionsContainer.Elements("Namespace"))
		{
			if (TryReadMatcher(exEl, MatchTarget.Namespace, out var m))
			{
				builder.Add(new ExceptionMatcher(m, ParseExceptions(exEl)));
			}
		}

		return builder.ToImmutable();
	}

	/// <summary>
	///     Resolves the (single) matcher attribute on <paramref name="el" /> using the standard
	///     precedence order. Semantic matchers (<c>inherits</c>, <c>implements</c>,
	///     <c>withAttribute</c>, <c>withAccessModifier</c>) and <c>exactFullName</c> are only
	///     meaningful on <see cref="MatchTarget.TypeName" /> targets; on <c>&lt;Namespace&gt;</c>
	///     they silently fall through, matching the legacy behaviour of <c>typeName</c>.
	/// </summary>
	private static bool TryReadMatcher(XElement el, MatchTarget target, out PatternMatcher matcher)
	{
		// typeName / exactName on <Class>: synonyms for an exact-name match.
		if (target == MatchTarget.TypeName)
		{
			if (el.Attribute("typeName")?.Value is { } typeName)
			{
				matcher = new PatternMatcher(target, MatchKind.Equals, typeName);
				return true;
			}

			if (el.Attribute("exactName")?.Value is { } exactName)
			{
				matcher = new PatternMatcher(target, MatchKind.Equals, exactName);
				return true;
			}

			if (el.Attribute("exactFullName")?.Value is { } exactFullName)
			{
				matcher = new PatternMatcher(target, MatchKind.EqualsFullName, exactFullName);
				return true;
			}

			if (el.Attribute("inherits")?.Value is { } inherits)
			{
				matcher = new PatternMatcher(target, MatchKind.Inherits, inherits);
				return true;
			}

			if (el.Attribute("implements")?.Value is { } implements)
			{
				matcher = new PatternMatcher(target, MatchKind.Implements, implements);
				return true;
			}

			if (el.Attribute("withAttribute")?.Value is { } withAttribute)
			{
				matcher = new PatternMatcher(target, MatchKind.HasAttribute, withAttribute);
				return true;
			}

			if (el.Attribute("withAccessModifier")?.Value is { } withAccessModifier)
			{
				matcher = new PatternMatcher(target, MatchKind.HasAccessModifier, withAccessModifier);
				return true;
			}
		}
		else if (el.Attribute("exactName")?.Value is { } exactValue)
		{
			matcher = new PatternMatcher(target, MatchKind.Equals, exactValue);
			return true;
		}

		if (el.Attribute("endsWith")?.Value is { } endsWith)
		{
			matcher = new PatternMatcher(target, MatchKind.EndsWith, endsWith);
			return true;
		}

		if (el.Attribute("startsWith")?.Value is { } startsWith)
		{
			matcher = new PatternMatcher(target, MatchKind.StartsWith, startsWith);
			return true;
		}

		if (el.Attribute("contains")?.Value is { } contains)
		{
			matcher = new PatternMatcher(target, MatchKind.Contains, contains);
			return true;
		}

		if (el.Attribute("regex")?.Value is { } regex)
		{
			matcher = new PatternMatcher(target, MatchKind.Regex, regex);
			return true;
		}

		matcher = default;
		return false;
	}

	/// <summary>
	///     Best-effort display label for a forbidden rule, used in diagnostics, reports, and the
	///     architecture documentation. Falls back to a generic placeholder when no matcher attribute is
	///     present (the rule will then never match anything anyway).
	/// </summary>
	private static string? GetForbiddenDisplayName(XElement el) =>
		el.Attribute("typeName")?.Value
		?? el.Attribute("exactName")?.Value
		?? el.Attribute("exactFullName")?.Value
		?? el.Attribute("inherits")?.Value
		?? el.Attribute("implements")?.Value
		?? el.Attribute("withAttribute")?.Value
		?? el.Attribute("withAccessModifier")?.Value
		?? el.Attribute("endsWith")?.Value
		?? el.Attribute("startsWith")?.Value
		?? el.Attribute("contains")?.Value
		?? el.Attribute("regex")?.Value;

	private static AnalyzerConfig CreateInvalidConfig(ImmutableArray<ConfigurationIssue> issues)
	{
		var config = AnalyzerConfig.Invalid(issues[0]);
		if (issues.Length == 1)
		{
			return config;
		}

		return new AnalyzerConfig(new LayerRegistry(ImmutableArray<LayerNode>.Empty, ImmutableDictionary<string, LayerNode>.Empty, ImmutableDictionary<string, MatcherRule>.Empty, ImmutableArray<(PatternMatcher, MatcherRule)>.Empty, ImmutableArray<(PatternMatcher, MatcherRule)>.Empty), new DependencyGraph(ImmutableArray<DependencyEdge>.Empty), new OutputConfig(false, string.Empty, false, string.Empty), false, false, ImmutableArray<LayerNode>.Empty, ImmutableArray<string>.Empty, ImmutableArray<(string, string?)>.Empty, ArchitectureDocumentation.Empty, issues);
	}

	private static void ValidateDocument(XDocument document, string configPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		document.Validate(ConfigurationSchemas.Value, (_, args) =>
		{
			var exception = args.Exception;
			issues.Add(new ConfigurationIssue(ConfigurationIssueKind.InvalidConfiguration, $"Architecture XML schema validation failed: {args.Message}", configPath, exception?.LineNumber ?? 0, exception?.LinePosition ?? 0));
		}, true);

		foreach (var element in document.Descendants().Where(element => element.Name.LocalName is "Class" or "Namespace" or "Assembly"))
		{
			ValidateMatcherElement(element, configPath, issues);
		}
	}

	private static void ValidateMatcherElement(XElement element, string configPath, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var matcherNames = new[]
		{
			"typeName",
			"exactName",
			"exactFullName",
			"inherits",
			"implements",
			"withAttribute",
			"withAccessModifier",
			"endsWith",
			"startsWith",
			"contains",
			"regex"
		};
		var configuredMatchers = element.Attributes().Where(attribute => matcherNames.Contains(attribute.Name.LocalName, StringComparer.Ordinal)).ToArray();
		if (configuredMatchers.Length == 0)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} requires exactly one matcher attribute.", element, configPath);
			return;
		}

		if (configuredMatchers.Length > 1)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} has multiple matcher attributes; configure exactly one.", element, configPath);
		}

		if (element.Name.LocalName is "Namespace" or "Assembly"
		    && configuredMatchers.Any(attribute => attribute.Name.LocalName is "typeName" or "exactFullName" or "inherits" or "implements" or "withAttribute" or "withAccessModifier"))
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{element.Name.LocalName} supports exactName, endsWith, startsWith, contains, or regex matchers.", element, configPath);
		}

		var regex = element.Attribute("regex")?.Value;
		if (regex is null)
		{
			return;
		}

		try
		{
			_ = new Regex(regex, RegexOptions.CultureInvariant);
		}
		catch (ArgumentException ex)
		{
			AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Invalid regular expression '{regex}': {ex.Message}", element, configPath);
		}
	}

	private static void AddIssue(ImmutableArray<ConfigurationIssue>.Builder issues, ConfigurationIssueKind kind, string message, XElement element, string path)
	{
		var line = (IXmlLineInfo)element;
		issues.Add(new ConfigurationIssue(kind, message, path, line.HasLineInfo() ? line.LineNumber : 0, line.HasLineInfo() ? line.LinePosition : 0));
	}

	private static XmlSchemaSet CreateConfigurationSchemas()
	{
		var assembly = typeof(ArchitecturalConfigParser).GetTypeInfo().Assembly;
		using var stream = assembly.GetManifestResourceStream("RonSijm.AnaalIJzer.AnaalIJzer.xsd")
			?? throw new InvalidOperationException("Embedded AnaalIJzer.xsd schema was not found.");
		using var reader = XmlReader.Create(stream);
		var schemas = new XmlSchemaSet();
		schemas.Add(string.Empty, reader);
		schemas.Compile();
		return schemas;
	}
}
