using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Tooling;

internal static partial class ApplicationConfigurationGenerator
{
	private static (IReadOnlyList<GeneratedLayer> Layers, Dictionary<string, GeneratedLayer> LayersByAssemblyName) InferSolutionLayers(SolutionAnalysisResult solution, CancellationToken cancellationToken)
	{
		var layers = new List<GeneratedLayer>();
		var layersByAssemblyName = new Dictionary<string, GeneratedLayer>(StringComparer.Ordinal);
		foreach (var project in solution.Projects)
		{
			if (string.IsNullOrWhiteSpace(project.AssemblyName)
			    || GetProjectTypes(project.Compilation, cancellationToken).Count == 0)
			{
				continue;
			}

			var layer = new GeneratedLayer(project.AssemblyName, $"Inferred solution layer for the {project.AssemblyName} project assembly.");
			layer.AssemblyNames.Add(project.AssemblyName);
			layers.Add(layer);
			layersByAssemblyName[project.AssemblyName] = layer;
		}

		return (layers, layersByAssemblyName);
	}

	private static IReadOnlyList<GeneratedEdge> DiscoverSolutionDependencies(SolutionAnalysisResult solution, IReadOnlyDictionary<string, GeneratedLayer> layersByAssemblyName, CancellationToken cancellationToken)
	{
		var edges = new List<GeneratedEdge>();
		var edgesByLayers = new Dictionary<(GeneratedLayer From, GeneratedLayer To), GeneratedEdge>();
		foreach (var project in solution.Projects)
		{
			cancellationToken.ThrowIfCancellationRequested();
			foreach (var observation in ProjectDependencyScanner.Scan(project.Compilation, ResolveLayerName, cancellationToken))
			{
				if (!TryResolveLayer(observation.CallerType, out var callerLayer)
				    || !TryResolveLayer(observation.DependencyType, out var dependencyLayer))
				{
					continue;
				}

				var key = (callerLayer, dependencyLayer);
				if (!edgesByLayers.TryGetValue(key, out var edge))
				{
					edge = new GeneratedEdge(callerLayer, dependencyLayer);
					edgesByLayers.Add(key, edge);
					edges.Add(edge);
				}

				edge.AddObservation(observation.CallerType, observation.Site);
			}
		}

		return edges;

		string? ResolveLayerName(INamedTypeSymbol type)
		{
			var result = TryResolveLayer(type, out var layer) ? layer.Name : null;

			return result;
		}

		bool TryResolveLayer(INamedTypeSymbol type, out GeneratedLayer layer)
		{
			var assemblyName = type.ContainingAssembly?.Name ?? string.Empty;
			var result = layersByAssemblyName.TryGetValue(assemblyName, out layer!);

			return result;
		}
	}
}
