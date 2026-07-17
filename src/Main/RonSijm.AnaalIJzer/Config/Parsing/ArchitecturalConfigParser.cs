using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.DependencyRules;
using RonSijm.AnaalIJzer.Model;
using AnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Parsing;

/// <summary>
///     Parses an <c>Architecture.anl</c> additional file or inline
///     <c>AssemblyMetadata("AnaalIJzerSettings", ...)</c> value into an <see cref="AnalyzerConfig" />.
/// </summary>
internal static partial class ArchitecturalConfigParser
{
	internal const string ConfigFileName = "Architecture.anl";
	internal const string InlineSettingsMetadataKey = "AnaalIJzerSettings";
	private static readonly Lazy<XmlSchemaSet> ConfigurationSchemas = new(CreateConfigurationSchemas);

	internal static AnalyzerConfig Parse(ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken)
	{
		var result = Parse(additionalFiles, null, cancellationToken);

		return result;
	}

	internal static AnalyzerConfig Parse(ImmutableArray<AdditionalText> additionalFiles, Compilation? compilation, CancellationToken cancellationToken)
	{
		var result = Parse(additionalFiles, compilation, null, cancellationToken);

		return result;
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

	internal static AdditionalText? FindConfigFile(ImmutableArray<AdditionalText> additionalFiles)
	{
		var result = additionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path).Equals(ConfigFileName, StringComparison.OrdinalIgnoreCase));

		return result;
	}

	private static (string? Content, string Path) ReadConfigXml(ImmutableArray<AdditionalText> additionalFiles, Compilation? compilation, string? inlineConfigPath, CancellationToken cancellationToken)
	{
		var configFile = FindConfigFile(additionalFiles);
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

	private static bool IsAssemblyMetadataAttribute(INamedTypeSymbol? attributeClass)
	{
		var result = attributeClass is not null
		             && string.Equals(attributeClass.Name, "AssemblyMetadataAttribute", StringComparison.Ordinal)
		             && string.Equals(attributeClass.ContainingNamespace?.ToDisplayString(), "System.Reflection", StringComparison.Ordinal);

		return result;
	}

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

			var requiredRecognizedDependencySites = ParseRequiredRecognizedDependencySites(documents, issues);
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
			var layerRequiredRecognizedDependencySites = ImmutableDictionary.CreateBuilder<string, ImmutableHashSet<string>>(StringComparer.Ordinal);
			var forbiddenPatterns = new List<(string Name, string? Comment)>();
			var rootLayerElements = elements.Where(item => item.Element.Name.LocalName == "Layer").ToArray();
			var roots = ParseLayerCollection(rootLayerElements, string.Empty, layerNames, layerNodesByPath, layerRequiredRecognizedDependencySites, issues);
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
			return new AnalyzerConfig(registry, graph, outputConfig, requiredRecognizedDependencySites, layerRequiredRecognizedDependencySites.ToImmutable(), enforceAcyclic, roots, [..layerNames], [..forbiddenPatterns], documentation, issues.ToImmutable());
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

}
