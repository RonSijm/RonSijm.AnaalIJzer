using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Configuration;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.DependencyRules;
using RonSijm.AnaalIJzer.Graph;
using RonSijm.AnaalIJzer.Indicators;
using RonSijm.AnaalIJzer.Parsing;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static ArchitectureDependencyGraphSnapshot CreateGraphSnapshot(ProjectAnalyzerConfig config, ImmutableDictionary<string, int> paletteSlots, ImmutableArray<ArchitectureLayerIndicator> layerIndicators, ArchitectureConfigurationSource configurationSource, string inlineConfigPath, ArchitectureDependencyGraphEvidence evidence)
	{
		var activeLayerPaths = layerIndicators.Select(indicator => indicator.LayerPath).Distinct(StringComparer.Ordinal).ToImmutableArray();
		var layers = ImmutableArray.CreateBuilder<ArchitectureDependencyGraphLayer>();
		foreach (var layer in config.Layers)
		{
			AddGraphLayer(config, layer, paletteSlots, activeLayerPaths, configurationSource, inlineConfigPath, layers);
		}

		var rules = config.Graph.DependencyEdges
			.Select(edge => new ArchitectureDependencyGraphRule(
				edge.From,
				edge.To,
				edge.ScopePath,
				edge.IsBlocked ? "BlockedDependency" : "AllowedDependency",
				string.IsNullOrWhiteSpace(edge.SiteFilter.ToDisplayText()) ? "all sites" : edge.SiteFilter.ToDisplayText(),
				edge.AppliesToDescendants,
				edge.From == "*" || edge.To == "*",
				RuleTouchesActiveLayer(edge, activeLayerPaths),
				edge.ConfiguredFrom,
				edge.ConfiguredTo,
				GetEditableRulePath(edge, configurationSource, inlineConfigPath),
				GetEditableRuleSourceKind(edge, configurationSource, inlineConfigPath),
				edge.XmlLineNumber,
				edge.XmlLinePosition,
				ArchitectureDependencySites.All.Where(edge.SiteFilter.AllowedSites.Contains).ToImmutableArray(),
				ArchitectureDependencySites.All.Where(edge.SiteFilter.BlockedSites.Contains).ToImmutableArray(),
				FindDependencyRuleDescription(config, edge)))
			.ToImmutableArray();

		var result = new ArchitectureDependencyGraphSnapshot(
			true,
			config.HasConfigurationIssues,
			layers.ToImmutable(),
			rules,
			activeLayerPaths,
			config.ConfigurationIssues.Select(issue => issue.Message).ToImmutableArray(),
			configurationSource,
			evidence);

		return result;
	}

	private static ArchitectureConfigurationSource FindConfigurationSource(Document document, ImmutableArray<AdditionalText> additionalFiles, Compilation compilation, CancellationToken cancellationToken)
	{
		var configFile = ArchitecturalConfigParser.FindConfigFile(additionalFiles);
		if (configFile is not null)
		{
			var result = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, configFile.Path);

			return result;
		}

		var inlinePath = FindInlineSettingsPath(document, compilation, cancellationToken);
		if (!string.IsNullOrWhiteSpace(inlinePath))
		{
			var result = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, inlinePath);

			return result;
		}

		return ArchitectureConfigurationSource.None;
	}

	private static string FindInlineSettingsPath(Document document, Compilation compilation, CancellationToken cancellationToken)
	{
		foreach (var syntaxTree in compilation.SyntaxTrees)
		{
			var root = syntaxTree.GetRoot(cancellationToken);
			if (ContainsInlineSettingsMetadata(root))
			{
				var result = string.IsNullOrWhiteSpace(syntaxTree.FilePath) ? document.FilePath ?? string.Empty : syntaxTree.FilePath;

				return result;
			}
		}

		return string.Empty;
	}

	private static bool ContainsInlineSettingsMetadata(SyntaxNode root)
	{
		foreach (var attribute in root.DescendantNodes().OfType<AttributeSyntax>())
		{
			if (!IsAssemblyLevelAttribute(attribute) || !IsAssemblyMetadataName(attribute.Name.ToString()))
			{
				continue;
			}

			var firstArgument = attribute.ArgumentList?.Arguments.FirstOrDefault();
			if (firstArgument?.Expression is LiteralExpressionSyntax literal
			    && string.Equals(literal.Token.ValueText, ArchitecturalConfigParser.InlineSettingsMetadataKey, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsAssemblyLevelAttribute(AttributeSyntax attribute)
	{
		var result = attribute.Parent is AttributeListSyntax { Target.Identifier.ValueText: "assembly" };

		return result;
	}

	private static bool IsAssemblyMetadataName(string name)
	{
		var result = name.EndsWith("AssemblyMetadata", StringComparison.Ordinal)
		             || name.EndsWith("AssemblyMetadataAttribute", StringComparison.Ordinal);

		return result;
	}

	private static string GetEditableRulePath(DependencyEdge edge, ArchitectureConfigurationSource configurationSource, string inlineConfigPath)
	{
		if (configurationSource.Kind == ArchitectureConfigurationSourceKind.InlineAssemblyMetadata
		    && string.Equals(edge.XmlPath, inlineConfigPath, StringComparison.OrdinalIgnoreCase))
		{
			return configurationSource.Path;
		}

		return edge.XmlPath;
	}

	private static ArchitectureConfigurationSourceKind GetEditableRuleSourceKind(DependencyEdge edge, ArchitectureConfigurationSource configurationSource, string inlineConfigPath)
	{
		if (configurationSource.Kind == ArchitectureConfigurationSourceKind.InlineAssemblyMetadata
		    && string.Equals(edge.XmlPath, inlineConfigPath, StringComparison.OrdinalIgnoreCase))
		{
			return ArchitectureConfigurationSourceKind.InlineAssemblyMetadata;
		}

		return string.IsNullOrWhiteSpace(edge.XmlPath) ? ArchitectureConfigurationSourceKind.None : ArchitectureConfigurationSourceKind.XmlFile;
	}

	private static string GetEditableElementPath(string sourcePath, ArchitectureConfigurationSource configurationSource, string inlineConfigPath)
	{
		if (configurationSource.Kind == ArchitectureConfigurationSourceKind.InlineAssemblyMetadata
		    && string.Equals(sourcePath, inlineConfigPath, StringComparison.OrdinalIgnoreCase))
		{
			return configurationSource.Path;
		}

		return sourcePath;
	}

	private static ArchitectureConfigurationSourceKind GetEditableElementSourceKind(string sourcePath, ArchitectureConfigurationSource configurationSource, string inlineConfigPath)
	{
		if (configurationSource.Kind == ArchitectureConfigurationSourceKind.InlineAssemblyMetadata
		    && string.Equals(sourcePath, inlineConfigPath, StringComparison.OrdinalIgnoreCase))
		{
			return ArchitectureConfigurationSourceKind.InlineAssemblyMetadata;
		}

		return string.IsNullOrWhiteSpace(sourcePath) ? ArchitectureConfigurationSourceKind.None : ArchitectureConfigurationSourceKind.XmlFile;
	}

	private static void AddGraphLayer(ProjectAnalyzerConfig config, LayerNode layer, ImmutableDictionary<string, int> paletteSlots, ImmutableArray<string> activeLayerPaths, ArchitectureConfigurationSource configurationSource, string inlineConfigPath, ImmutableArray<ArchitectureDependencyGraphLayer>.Builder layers)
	{
		var layerPath = layer.Definition.Name;
		var slashIndex = layerPath.LastIndexOf('/');
		var displayName = slashIndex >= 0 ? layerPath.Substring(slashIndex + 1) : layerPath;
		var paletteSlot = paletteSlots.TryGetValue(layerPath, out var slot) ? slot : 1;
		var documentationItem = config.Documentation.Items.FirstOrDefault(item => item.Kind == "Layer" && item.LayerPath == layerPath);
		var sourcePath = documentationItem.Kind == "Layer" ? documentationItem.SourcePath : string.Empty;
		layers.Add(new ArchitectureDependencyGraphLayer(
			layerPath,
			displayName,
			FindLayerDescription(config, layerPath),
			layerPath.Count(character => character == '/'),
			paletteSlot,
			activeLayerPaths.Any(activeLayerPath => PathsOverlap(layerPath, activeLayerPath)),
			GetEditableElementPath(sourcePath, configurationSource, inlineConfigPath),
			GetEditableElementSourceKind(sourcePath, configurationSource, inlineConfigPath),
			documentationItem.Kind == "Layer" ? documentationItem.XmlLineNumber : 0));

		foreach (var child in layer.Children)
		{
			AddGraphLayer(config, child, paletteSlots, activeLayerPaths, configurationSource, inlineConfigPath, layers);
		}
	}

	private static bool RuleTouchesActiveLayer(DependencyEdge edge, ImmutableArray<string> activeLayerPaths)
	{
		var result = activeLayerPaths.Any(activeLayerPath => EndpointTouchesLayer(edge.From, activeLayerPath) || EndpointTouchesLayer(edge.To, activeLayerPath));

		return result;
	}

	private static bool EndpointTouchesLayer(string endpoint, string activeLayerPath)
	{
		var result = endpoint == "*" || PathsOverlap(endpoint, activeLayerPath);

		return result;
	}

	private static bool PathsOverlap(string left, string right)
	{
		var result = left == right
		             || left.StartsWith(right + "/", StringComparison.Ordinal)
		             || right.StartsWith(left + "/", StringComparison.Ordinal);

		return result;
	}
}
