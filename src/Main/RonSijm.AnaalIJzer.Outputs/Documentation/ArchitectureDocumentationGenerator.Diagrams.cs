using System.Collections.Immutable;
using System.Text;
using RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model;

namespace RonSijm.AnaalIJzer.Outputs.Documentation;

internal static partial class ArchitectureDocumentationMarkdownBuilder
{
	private static void AppendDependencyDiagrams(StringBuilder sb, AnalyzerConfig config)
	{
		sb.AppendLine("## Dependency Flow");
		sb.AppendLine();

		var explicitEdges = config.Graph.DependencyEdges.Where(edge => edge.IsExplicit).ToImmutableArray();
		var components = GetConnectedComponents(config.Layers, explicitEdges);
		if (components.Length == 0)
		{
			sb.AppendLine("No layers are configured.");
			sb.AppendLine();
		}
		else
		{
			var chainNumber = 1;
			foreach (var component in components)
			{
				var rootNames = component.Select(node => node.Definition.Name).ToImmutableHashSet(StringComparer.Ordinal);
				var componentEdges = explicitEdges.Where(edge => rootNames.Contains(GetRootName(edge.From)) && rootNames.Contains(GetRootName(edge.To))).ToImmutableArray();
				var title = string.Join(", ", component.Select(node => GetLocalName(node.Definition.Name)));
				sb.AppendLine(components.Length == 1 ? "### Layer Flow" : $"### Dependency Chain {chainNumber++}: {title}");
				sb.AppendLine();
				sb.AppendLine(componentEdges.Length == 0
					? "This boundary has no explicit named dependency chain. It may still participate through wildcard rules."
					: "These boundaries form one connected dependency chain in the configured rules.");
				sb.AppendLine();
				AppendMermaidDiagram(sb, component, componentEdges);
				AppendLayerTable(sb, config, FlattenLayerNames(component));
				AppendEdgeTable(sb, config, componentEdges);
			}
		}

		AppendWildcardDependencyRules(sb, config);
	}

	private static void AppendWildcardDependencyRules(StringBuilder sb, AnalyzerConfig config)
	{
		var wildcardEdges = config.Graph.DependencyEdges.Where(edge => !edge.IsExplicit).ToImmutableArray();
		if (wildcardEdges.Length == 0)
		{
			return;
		}

		sb.AppendLine("### Universal Rules");
		sb.AppendLine();
		sb.AppendLine("These rules expand unconditionally across every layer and sub-layer. " +
		              "The `all layers` node represents any configured layer — a rule from or to `all layers` " +
		              "is automatically satisfied for every boundary in the architecture, including nested sub-layers.");
		sb.AppendLine();
		var wildcardLayerNames = new HashSet<string>(StringComparer.Ordinal);
		foreach (var edge in wildcardEdges)
		{
			if (edge.From != "*")
			{
				wildcardLayerNames.Add(GetRootName(edge.From));
			}

			if (edge.To != "*")
			{
				wildcardLayerNames.Add(GetRootName(edge.To));
			}
		}

		var relevantLayers = config.Layers.Where(l => wildcardLayerNames.Contains(l.Definition.Name)).ToImmutableArray();
		AppendMermaidDiagram(sb, relevantLayers, wildcardEdges);
		AppendEdgeTable(sb, config, wildcardEdges);
	}
}
