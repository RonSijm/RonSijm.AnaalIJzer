using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Graphing.ViewModels;

namespace RonSijm.AnaalIJzer.Graphing.Building;

public static partial class ArchitectureGraphViewModelBuilder
{
	private const double NodeColumnWidth = 220;
	private const double NodeRowHeight = 120;
	private const double NodeStartX = 36;
	private const double NodeStartY = 44;
	private const double NodeVisualWidth = 170;
	private const double NodeVisualHeight = 72;
	private const double BoundaryPaddingX = 28;
	private const double BoundaryPaddingTop = 36;
	private const double BoundaryPaddingBottom = 24;
	private const double BlockRowGap = 48;
	private const double BlockHorizontalGap = 32;

	public static ImmutableArray<ArchitectureGraphGroupViewModel> Build(ArchitectureGraphSnapshot snapshot, ArchitectureGraphFocusMode focusMode, bool includeEvidence = false)
	{
		if (!snapshot.HasConfiguration || snapshot.HasConfigurationIssues)
		{
			return ImmutableArray<ArchitectureGraphGroupViewModel>.Empty;
		}

		var groups = ImmutableArray.CreateBuilder<ArchitectureGraphGroupViewModel>();
		var layerGroups = BuildConcreteGroups(snapshot, focusMode, includeEvidence);
		groups.AddRange(layerGroups);
		var wildcardRules = snapshot.Rules.Where(rule => rule.IsWildcard).ToImmutableArray();
		if (wildcardRules.Length > 0)
		{
			var wildcardActive = wildcardRules.Any(rule => rule.IsActive);
			var wildcardDiagram = BuildWildcardDiagram(snapshot, wildcardRules);
			groups.Add(new ArchitectureGraphGroupViewModel(
				"Wildcard and global rules",
				wildcardActive,
				focusMode == ArchitectureGraphFocusMode.HighlightCurrent && wildcardActive,
				ImmutableArray<string>.Empty,
				wildcardRules.Select(FormatRule).ToImmutableArray(),
				wildcardDiagram.Nodes,
				wildcardDiagram.Edges,
				snapshot.ConfigurationSource));
		}

		var builtGroups = groups.ToImmutable();
		if (focusMode != ArchitectureGraphFocusMode.FilterToCurrent || !builtGroups.Any(group => group.IsActive))
		{
			return builtGroups;
		}

		var result = builtGroups.Where(group => group.IsActive).ToImmutableArray();

		return result;
	}

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
			var componentDiagram = BuildConcreteDiagram(componentLayers, componentRules, includeEvidence ? snapshot.Evidence : ArchitectureGraphEvidence.Empty, componentEvidence);
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
