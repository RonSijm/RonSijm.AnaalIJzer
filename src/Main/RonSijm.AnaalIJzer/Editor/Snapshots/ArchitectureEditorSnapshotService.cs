using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Graph;
using RonSijm.AnaalIJzer.Indicators;
using RonSijm.AnaalIJzer.Parsing;

namespace RonSijm.AnaalIJzer.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private const int PaletteSlotCount = 16;

	public static async Task<ArchitectureEditorSnapshot> CreateSnapshotAsync(Document document, CancellationToken cancellationToken = default)
	{
		var additionalFiles = document.Project.AnalyzerOptions.AdditionalFiles;
		var result = await CreateSnapshotAsync(document, additionalFiles, false, cancellationToken);

		return result;
	}

	public static async Task<ArchitectureEditorSnapshot> CreateSnapshotAsync(Document document, ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken = default)
	{
		var result = await CreateSnapshotAsync(document, additionalFiles, false, cancellationToken);

		return result;
	}

	public static async Task<ArchitectureEditorSnapshot> CreateSnapshotAsync(Document document, ImmutableArray<AdditionalText> additionalFiles, bool includeProjectEvidence, CancellationToken cancellationToken = default)
	{
		var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
		if (syntaxRoot is null || IsGenerated(document, syntaxRoot, cancellationToken))
		{
			return ArchitectureEditorSnapshot.Empty;
		}

		var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
		var compilation = await document.Project.GetCompilationAsync(cancellationToken);
		if (semanticModel is null || compilation is null)
		{
			return ArchitectureEditorSnapshot.Empty;
		}

		var inlineConfigPath = document.Project.FilePath is { } projectPath
			? Path.Combine(Path.GetDirectoryName(projectPath) ?? string.Empty, ArchitecturalConfigParser.InlineSettingsMetadataKey)
			: ArchitecturalConfigParser.InlineSettingsMetadataKey;
		var configurationSource = FindConfigurationSource(document, additionalFiles, compilation, cancellationToken);
		var config = ArchitecturalConfigParser.Parse(additionalFiles, compilation, inlineConfigPath, cancellationToken);
		if (config.HasConfigurationIssues)
		{
			var graphSnapshot = new ArchitectureDependencyGraphSnapshot(
				true,
				true,
				ImmutableArray<ArchitectureDependencyGraphLayer>.Empty,
				ImmutableArray<ArchitectureDependencyGraphRule>.Empty,
				ImmutableArray<string>.Empty,
				config.ConfigurationIssues.Select(issue => issue.Message).ToImmutableArray(),
				configurationSource);

			return new ArchitectureEditorSnapshot(
				true,
				true,
				ImmutableArray<ArchitectureLayerIndicator>.Empty,
				ImmutableArray<ArchitectureDependencySiteIndicator>.Empty,
				config.ConfigurationIssues.Select(issue => issue.Message).ToImmutableArray(),
				graphSnapshot);
		}

		if (!config.HasLayers)
		{
			return new ArchitectureEditorSnapshot(
				config.HasConfigurationIssues,
				config.HasConfigurationIssues,
				ImmutableArray<ArchitectureLayerIndicator>.Empty,
				ImmutableArray<ArchitectureDependencySiteIndicator>.Empty,
				config.ConfigurationIssues.Select(issue => issue.Message).ToImmutableArray());
		}

		var layerIndicators = ImmutableArray.CreateBuilder<ArchitectureLayerIndicator>();
		var unclassifiedTypeIndicators = ImmutableArray.CreateBuilder<ArchitectureLayerIndicator>();
		var siteIndicators = ImmutableArray.CreateBuilder<ArchitectureDependencySiteIndicator>();
		var paletteSlots = BuildPaletteSlots(config);

		foreach (var typeDeclaration in syntaxRoot.DescendantNodes().OfType<TypeDeclarationSyntax>())
		{
			AddLayerIndicator(typeDeclaration, semanticModel, config, paletteSlots, layerIndicators, unclassifiedTypeIndicators, cancellationToken);
		}

		foreach (var node in syntaxRoot.DescendantNodes())
		{
			AddSiteIndicators(node, semanticModel, config, paletteSlots, siteIndicators, cancellationToken);
		}

		var layerIndicatorArray = layerIndicators.ToImmutable();
		var unclassifiedTypeIndicatorArray = unclassifiedTypeIndicators.ToImmutable();
		var siteIndicatorArray = siteIndicators.ToImmutable();
		var evidence = includeProjectEvidence
			? CreateProjectEvidence(compilation, config, cancellationToken)
			: ArchitectureDependencyGraphEvidence.Empty;
		var graph = CreateGraphSnapshot(config, paletteSlots, layerIndicatorArray, configurationSource, inlineConfigPath, evidence);
		var result = new ArchitectureEditorSnapshot(
			true,
			config.HasConfigurationIssues,
			layerIndicatorArray,
			siteIndicatorArray,
			config.ConfigurationIssues.Select(issue => issue.Message).ToImmutableArray(),
			graph,
			unclassifiedTypeIndicatorArray);

		return result;
	}

}
