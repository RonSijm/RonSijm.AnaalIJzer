using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Model;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.Core.Observations;
using RonSijm.AnaalIJzer.GraphModel.Model;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.EditorRuntime.Editor.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static ArchitectureGraphEvidence CreateProjectEvidence(Compilation compilation, ProjectAnalyzerConfig config, CancellationToken cancellationToken)
	{
		var types = ImmutableArray.CreateBuilder<ArchitectureGraphTypeEvidence>();
		var dependencies = ImmutableArray.CreateBuilder<ArchitectureGraphDependencyEvidence>();
		var seenTypes = new HashSet<string>(StringComparer.Ordinal);
		var seenDependencies = new HashSet<string>(StringComparer.Ordinal);
		var observations = ProjectDependencyScanner.Scan(compilation, type => ResolveObservedLayer(config, type), cancellationToken);

		foreach (var typeSymbol in CompilationTypeCollector.GetProjectTypes(compilation, cancellationToken))
		{
			AddProjectTypeEvidence(typeSymbol, config, types, seenTypes);
		}

		foreach (var observation in observations)
		{
			AddProjectDependencyEvidence(observation, config, dependencies, seenDependencies);
		}

		var analyzedApiSurfaceSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
		var transitiveMemberCache = new ConcurrentDictionary<INamedTypeSymbol, ImmutableArray<ExposureMemberTypeReference>>(SymbolEqualityComparer.Default);

		foreach (var syntaxTree in compilation.SyntaxTrees)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (GeneratedCodeDetector.IsGenerated(syntaxTree, cancellationToken))
			{
				continue;
			}

			var semanticModel = compilation.GetSemanticModel(syntaxTree);
			var root = syntaxTree.GetRoot(cancellationToken);
			foreach (var node in root.DescendantNodes())
			{
				AddProjectApiSurfaceEvidence(node, semanticModel, compilation, config, dependencies, seenDependencies, analyzedApiSurfaceSymbols, transitiveMemberCache, cancellationToken);
			}
		}

		var result = new ArchitectureGraphEvidence(types.ToImmutable(), dependencies.ToImmutable());

		return result;
	}

	private static string? ResolveObservedLayer(ProjectAnalyzerConfig config, INamedTypeSymbol type)
	{
		var layerName = config.Engine.FindLayer(type.Name, type.ContainingNamespace?.ToDisplayString() ?? string.Empty, type)?.Layer.Name;

		return layerName;
	}

	private static void AddProjectApiSurfaceEvidence(
		SyntaxNode node,
		SemanticModel semanticModel,
		Compilation compilation,
		ProjectAnalyzerConfig config,
		ImmutableArray<ArchitectureGraphDependencyEvidence>.Builder dependencies,
		HashSet<string> seenDependencies,
		HashSet<ISymbol> analyzedApiSurfaceSymbols,
		ConcurrentDictionary<INamedTypeSymbol, ImmutableArray<ExposureMemberTypeReference>> transitiveMemberCache,
		CancellationToken cancellationToken)
	{
		var indicators = ImmutableArray.CreateBuilder<ArchitectureApiSurfaceIndicator>();
		AddApiSurfaceIndicators(node, semanticModel, compilation, config, indicators, analyzedApiSurfaceSymbols, transitiveMemberCache, cancellationToken);
		if (indicators.Count == 0)
		{
			return;
		}

		var sourceText = node.SyntaxTree.GetText(cancellationToken);
		foreach (var indicator in indicators)
		{
			if (string.IsNullOrWhiteSpace(indicator.CallerLayerPath) || string.IsNullOrWhiteSpace(indicator.ExposedLayerPath))
			{
				continue;
			}

			var spanStart = indicator.Span.Start;
			if (spanStart < 0)
			{
				spanStart = 0;
			}
			else if (spanStart > sourceText.Length)
			{
				spanStart = sourceText.Length;
			}
			var lineNumber = sourceText.Lines.GetLineFromPosition(spanStart).LineNumber + 1;
			var evidence = new ArchitectureGraphDependencyEvidence(
				indicator.CallerLayerPath,
				indicator.ExposedLayerPath,
				indicator.CallerTypeName,
				indicator.ExposedTypeName,
				indicator.Site,
				GetApiSurfaceEvidenceStatus(indicator.DiagnosticId),
				indicator.DiagnosticId,
				indicator.Reason,
				node.SyntaxTree.FilePath ?? string.Empty,
				lineNumber,
				indicator.ExposurePath,
				indicator.ExposureDepth);
			var key = evidence.CallerLayerPath
			          + "|"
			          + evidence.DependencyLayerPath
			          + "|"
			          + evidence.CallerTypeName
			          + "|"
			          + evidence.DependencyTypeName
			          + "|"
			          + evidence.Site
			          + "|"
			          + evidence.FilePath
			          + "|"
			          + evidence.LineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture)
			          + "|"
			          + evidence.DiagnosticId
			          + "|"
			          + evidence.ExposurePath;
			if (seenDependencies.Add(key))
			{
				dependencies.Add(evidence);
			}
		}
	}

	private static void AddProjectTypeEvidence(INamedTypeSymbol typeSymbol, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureGraphTypeEvidence>.Builder types, HashSet<string> seenTypes)
	{
		var match = config.Engine.FindLayer(typeSymbol.Name, typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty, typeSymbol);
		if (match is null || match.Value.Layer.IsForbidden)
		{
			return;
		}

		var location = typeSymbol.Locations.FirstOrDefault(current => current.IsInSource);
		if (location is null)
		{
			return;
		}

		var filePath = GetLocationPath(location);
		var lineNumber = GetLineNumber(location);
		var fullTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
		var key = match.Value.Layer.Name + "|" + fullTypeName + "|" + filePath + "|" + lineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
		if (!seenTypes.Add(key))
		{
			return;
		}

		types.Add(new ArchitectureGraphTypeEvidence(
			match.Value.Layer.Name,
			typeSymbol.Name,
			fullTypeName,
			filePath,
			lineNumber));
	}
}
