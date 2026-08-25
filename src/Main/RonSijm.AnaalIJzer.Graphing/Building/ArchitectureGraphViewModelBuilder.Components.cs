using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Graphing.ViewModels;

namespace RonSijm.AnaalIJzer.Graphing.Building;

internal static partial class ArchitectureGraphViewModelBuilder
{
	private static ImmutableArray<ArchitectureGraphGroupViewModel> BuildConcreteGroups(ArchitectureGraphSnapshot snapshot, ArchitectureGraphFocusMode focusMode, bool includeEvidence)
	{
		var layersByPath = snapshot.Layers.ToDictionary(layer => layer.Path, StringComparer.Ordinal);
		var adjacency = snapshot.Layers.ToDictionary(layer => layer.Path, _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
		foreach (var layer in snapshot.Layers)
		{
			var parentPath = GetParentPath(layer.Path);
			if (parentPath.Length == 0 || !layersByPath.ContainsKey(parentPath))
			{
				continue;
			}

			adjacency[parentPath].Add(layer.Path);
			adjacency[layer.Path].Add(parentPath);
		}

		foreach (var rule in snapshot.Rules.Where(rule => !rule.IsWildcard && layersByPath.ContainsKey(rule.From) && layersByPath.ContainsKey(rule.To)))
		{
			adjacency[rule.From].Add(rule.To);
			adjacency[rule.To].Add(rule.From);
		}

		if (includeEvidence)
		{
			foreach (var dependency in snapshot.Evidence.Dependencies.Where(dependency => dependency.IsViolation && layersByPath.ContainsKey(dependency.CallerLayerPath) && layersByPath.ContainsKey(dependency.DependencyLayerPath)))
			{
				adjacency[dependency.CallerLayerPath].Add(dependency.DependencyLayerPath);
				adjacency[dependency.DependencyLayerPath].Add(dependency.CallerLayerPath);
			}
		}

		var visited = new HashSet<string>(StringComparer.Ordinal);
		var groups = ImmutableArray.CreateBuilder<ArchitectureGraphGroupViewModel>();
		foreach (var layer in snapshot.Layers)
		{
			if (!visited.Add(layer.Path))
			{
				continue;
			}

			var component = CollectComponent(layer.Path, adjacency, visited);
			var componentLayers = snapshot.Layers.Where(item => component.Contains(item.Path)).ToImmutableArray();
			var componentRules = snapshot.Rules
				.Where(rule => !rule.IsWildcard && component.Contains(rule.From) && component.Contains(rule.To))
				.ToImmutableArray();
			var componentEvidence = includeEvidence
				? snapshot.Evidence.Dependencies.Where(dependency => component.Contains(dependency.CallerLayerPath) && component.Contains(dependency.DependencyLayerPath)).ToImmutableArray()
				: ImmutableArray<ArchitectureGraphDependencyEvidence>.Empty;
			var componentDiagram = BuildConcreteDiagram(componentLayers, componentRules, snapshot.ExceptionReviews, includeEvidence ? snapshot.Evidence : ArchitectureGraphEvidence.Empty, componentEvidence);
			var active = componentLayers.Any(item => item.IsActive);
			var index = groups.Count + 1;
			groups.Add(new ArchitectureGraphGroupViewModel(
				"Graph " + index + ": " + FormatGraphName(componentLayers),
				active,
				focusMode == ArchitectureGraphFocusMode.HighlightCurrent && active,
				componentLayers.Select(FormatLayer).ToImmutableArray(),
				componentRules.Select(FormatRule).ToImmutableArray(),
				componentDiagram.Nodes,
				componentDiagram.Edges,
				snapshot.ConfigurationSource,
				componentDiagram.Boundaries));
		}

		return groups.ToImmutable();
	}
}
