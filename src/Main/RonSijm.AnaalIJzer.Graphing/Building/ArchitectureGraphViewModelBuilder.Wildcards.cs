using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Graphing.ViewModels;

namespace RonSijm.AnaalIJzer.Graphing.Building;

public static partial class ArchitectureGraphViewModelBuilder
{
	private static GraphDiagram BuildWildcardDiagram(ArchitectureGraphSnapshot snapshot, ImmutableArray<ArchitectureGraphRule> rules)
	{
		var nodes = new Dictionary<string, ArchitectureGraphNodeViewModel>(StringComparer.Ordinal);
		var layerByPath = snapshot.Layers.ToDictionary(layer => layer.Path, StringComparer.Ordinal);
		foreach (var rule in rules)
		{
			EnsureWildcardNode(nodes, layerByPath, rule.From, true);
			EnsureWildcardNode(nodes, layerByPath, rule.To, false);
		}

		var orderedNodes = nodes.Values
			.OrderBy(node => node.X)
			.ThenBy(node => node.Y)
			.ToImmutableArray();
		var edges = rules
			.Select(CreateEdge)
			.ToImmutableArray();
		var result = new GraphDiagram(orderedNodes, edges, ImmutableArray<ArchitectureGraphBoundaryViewModel>.Empty);

		return result;
	}

	private static void EnsureWildcardNode(Dictionary<string, ArchitectureGraphNodeViewModel> nodes, Dictionary<string, ArchitectureGraphLayer> layerByPath, string path, bool isSource)
	{
		if (nodes.ContainsKey(path))
		{
			return;
		}

		if (path == "*")
		{
			nodes.Add(path, new ArchitectureGraphNodeViewModel(path, "*", "Any configured layer.", 0, 0, true, isSource ? NodeStartX : NodeStartX + NodeColumnWidth, NodeStartY));
			return;
		}

		if (layerByPath.TryGetValue(path, out var layer))
		{
			nodes.Add(path, new ArchitectureGraphNodeViewModel(layer.Path, layer.DisplayName, layer.Description, layer.Depth, layer.PaletteSlot, layer.IsActive, isSource ? NodeStartX : NodeStartX + NodeColumnWidth, NodeStartY + nodes.Count * 80));
			return;
		}

		nodes.Add(path, new ArchitectureGraphNodeViewModel(path, path, null, 0, 0, false, isSource ? NodeStartX : NodeStartX + NodeColumnWidth, NodeStartY + nodes.Count * 80));
	}

	private static ArchitectureGraphEdgeViewModel CreateEdge(ArchitectureGraphRule rule)
	{
		var result = new ArchitectureGraphEdgeViewModel(
			rule.From,
			rule.To,
			rule.Kind,
			rule.SiteText,
			rule.AppliesToDescendants,
			rule.IsActive,
			rule.ScopePath,
			rule.ConfiguredFrom,
			rule.ConfiguredTo,
			rule.SourcePath,
			rule.SourceKind,
			rule.XmlLineNumber,
			rule.XmlLinePosition,
			rule.AllowedSites,
			rule.BlockedSites,
			rule.Description);

		return result;
	}

	private static ImmutableArray<ArchitectureGraphEdgeViewModel> CreateEvidenceEdges(ImmutableArray<ArchitectureGraphDependencyEvidence> dependencies)
	{
		var result = dependencies
			.GroupBy(dependency => new { dependency.CallerLayerPath, dependency.DependencyLayerPath })
			.Where(group => group.Any(dependency => dependency.IsViolation))
			.Select(group =>
			{
				var observedUsageCount = group.Count();
				var violationCount = group.Count(dependency => dependency.IsViolation);
				return new ArchitectureGraphEdgeViewModel(
					group.Key.CallerLayerPath,
					group.Key.DependencyLayerPath,
					"CodeEvidence",
					violationCount + " violation" + (violationCount == 1 ? string.Empty : "s") + " in " + observedUsageCount + " observed use" + (observedUsageCount == 1 ? string.Empty : "s"),
					false,
					true,
					description: string.Join(Environment.NewLine, group
						.Where(dependency => dependency.IsViolation)
						.OrderBy(dependency => dependency.FilePath, StringComparer.OrdinalIgnoreCase)
						.ThenBy(dependency => dependency.LineNumber)
						.Take(8)
						.Select(dependency => dependency.CallerTypeName + " -> " + dependency.DependencyTypeName + " (" + dependency.Site + "): " + dependency.Reason)),
					isEvidence: true,
					observedUsageCount: observedUsageCount,
					violationCount: violationCount);
			})
			.OrderBy(edge => edge.From, StringComparer.Ordinal)
			.ThenBy(edge => edge.To, StringComparer.Ordinal)
			.ToImmutableArray();

		return result;
	}

	private static HashSet<string> CollectComponent(string start, Dictionary<string, HashSet<string>> adjacency, HashSet<string> visited)
	{
		var component = new HashSet<string>(StringComparer.Ordinal);
		var stack = new Stack<string>();
		stack.Push(start);
		while (stack.Count > 0)
		{
			var current = stack.Pop();
			component.Add(current);
			foreach (var next in adjacency[current])
			{
				if (visited.Add(next))
				{
					stack.Push(next);
				}
			}
		}

		return component;
	}

	private static string GetParentPath(string path)
	{
		var slashIndex = path.LastIndexOf('/');
		var result = slashIndex <= 0 ? string.Empty : path.Substring(0, slashIndex);

		return result;
	}

	private static bool IsChildOf(string candidatePath, string parentPath)
	{
		var result = candidatePath.StartsWith(parentPath + "/", StringComparison.Ordinal);

		return result;
	}

	private static bool IsContainmentRelationship(string firstPath, string secondPath)
	{
		var result = IsChildOf(firstPath, secondPath) || IsChildOf(secondPath, firstPath);

		return result;
	}

	private static string FormatGraphName(ImmutableArray<ArchitectureGraphLayer> layers)
	{
		var minimumDepth = layers.Min(layer => layer.Depth);
		var names = layers.Where(layer => layer.Depth == minimumDepth).Select(layer => layer.DisplayName);
		var result = string.Join(", ", names);

		return result;
	}

	private static string FormatLayer(ArchitectureGraphLayer layer)
	{
		var indent = new string(' ', layer.Depth * 2);
		var active = layer.IsActive ? "  [current]" : string.Empty;
		var description = string.IsNullOrWhiteSpace(layer.Description) ? string.Empty : " - " + layer.Description;
		var result = indent + layer.DisplayName + " (" + layer.Path + ")" + active + description;

		return result;
	}

	private static string FormatRule(ArchitectureGraphRule rule)
	{
		var cascade = rule.AppliesToDescendants ? ", cascades to descendants" : string.Empty;
		var scope = string.IsNullOrWhiteSpace(rule.ScopePath) ? "root" : rule.ScopePath;
		var arrow = rule.Kind == "BlockedDependency" ? " -x-> " : " -> ";
		var result = rule.Kind + " [" + scope + "]: " + rule.From + arrow + rule.To + " (" + rule.SiteText + cascade + ")";

		return result;
	}
}
