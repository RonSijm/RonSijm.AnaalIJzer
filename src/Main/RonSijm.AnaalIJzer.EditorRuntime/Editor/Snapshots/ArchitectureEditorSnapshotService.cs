using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.GraphModel.Model;

namespace RonSijm.AnaalIJzer.EditorRuntime.Editor.Snapshots;

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
			? Path.Combine(Path.GetDirectoryName(projectPath) ?? string.Empty, ArchitectureConfigurationDocumentLoader.InlineSettingsMetadataKey)
			: ArchitectureConfigurationDocumentLoader.InlineSettingsMetadataKey;
		var configurationSource = FindConfigurationSource(document, additionalFiles, compilation, cancellationToken);
		var config = ArchitecturalConfigParser.Parse(additionalFiles, compilation, inlineConfigPath, cancellationToken);
		if (config.HasConfigurationIssues)
		{
			var graphSnapshot = new ArchitectureGraphSnapshot(
				true,
				true,
				ImmutableArray<ArchitectureGraphLayer>.Empty,
				ImmutableArray<ArchitectureGraphRule>.Empty,
				ImmutableArray<string>.Empty,
				[..config.ConfigurationIssues.Select(issue => issue.Message)],
				configurationSource,
				exceptionReviews: ImmutableArray<ArchitectureGraphExceptionReview>.Empty);

			return new ArchitectureEditorSnapshot(
				true,
				true,
				ImmutableArray<ArchitectureLayerIndicator>.Empty,
				ImmutableArray<ArchitectureDependencySiteIndicator>.Empty,
				[..config.ConfigurationIssues.Select(issue => issue.Message)],
				graphSnapshot);
		}

		if (!config.Engine.HasLayers)
		{
			var hasConfiguration = configurationSource.CanEdit;
			var graphSnapshot = CreateEmptyGraphSnapshot(document, configurationSource, hasConfiguration);
			return new ArchitectureEditorSnapshot(
				hasConfiguration,
				false,
				ImmutableArray<ArchitectureLayerIndicator>.Empty,
				ImmutableArray<ArchitectureDependencySiteIndicator>.Empty,
				ImmutableArray<string>.Empty,
				graphSnapshot);
		}

		var layerIndicators = ImmutableArray.CreateBuilder<ArchitectureLayerIndicator>();
		var unclassifiedTypeIndicators = ImmutableArray.CreateBuilder<ArchitectureLayerIndicator>();
		var siteIndicators = ImmutableArray.CreateBuilder<ArchitectureDependencySiteIndicator>();
		var nameRuleIndicators = ImmutableArray.CreateBuilder<ArchitectureNameRuleIndicator>();
		var visibilityPolicyIndicators = ImmutableArray.CreateBuilder<ArchitectureVisibilityPolicyIndicator>();
		var apiSurfaceIndicators = ImmutableArray.CreateBuilder<ArchitectureApiSurfaceIndicator>();
		var analyzedVisibilitySymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
		var analyzedApiSurfaceSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
		var transitiveMemberCache = new ConcurrentDictionary<INamedTypeSymbol, ImmutableArray<ExposureMemberTypeReference>>(SymbolEqualityComparer.Default);
		var paletteSlots = BuildPaletteSlots(config);
		var exceptionReviews = CreateGraphExceptionReviews(config, compilation, cancellationToken);

		foreach (var typeDeclaration in syntaxRoot.DescendantNodes().OfType<TypeDeclarationSyntax>())
		{
			AddLayerIndicator(typeDeclaration, semanticModel, config, paletteSlots, exceptionReviews, layerIndicators, unclassifiedTypeIndicators, cancellationToken);
		}

		foreach (var node in syntaxRoot.DescendantNodes())
		{
			AddSiteIndicators(node, semanticModel, config, paletteSlots, siteIndicators, cancellationToken);
			AddNameRuleIndicators(node, semanticModel, config, nameRuleIndicators, cancellationToken);
			AddVisibilityPolicyIndicators(node, semanticModel, config, visibilityPolicyIndicators, analyzedVisibilitySymbols, cancellationToken);
			AddApiSurfaceIndicators(node, semanticModel, compilation, config, apiSurfaceIndicators, analyzedApiSurfaceSymbols, transitiveMemberCache, cancellationToken);
		}

		var layerIndicatorArray = layerIndicators.ToImmutable();
		var unclassifiedTypeIndicatorArray = unclassifiedTypeIndicators.ToImmutable();
		var siteIndicatorArray = siteIndicators.ToImmutable();
		var evidence = includeProjectEvidence
			? CreateProjectEvidence(compilation, config, cancellationToken)
			: ArchitectureGraphEvidence.Empty;
		var graph = CreateGraphSnapshot(config, paletteSlots, layerIndicatorArray, configurationSource, inlineConfigPath, evidence, exceptionReviews);
		var result = new ArchitectureEditorSnapshot(
			true,
			config.HasConfigurationIssues,
			layerIndicatorArray,
			siteIndicatorArray,
			[..config.ConfigurationIssues.Select(issue => issue.Message)],
			graph,
			unclassifiedTypeIndicatorArray,
			nameRuleIndicators.ToImmutable(),
			visibilityPolicyIndicators.ToImmutable(),
			apiSurfaceIndicators.ToImmutable());

		return result;
	}

}
