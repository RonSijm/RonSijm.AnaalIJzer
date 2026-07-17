using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.DependencyRules;
using RonSijm.AnaalIJzer.Indicators;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static ImmutableDictionary<string, int> BuildPaletteSlots(ProjectAnalyzerConfig config)
	{
		var slots = ImmutableDictionary.CreateBuilder<string, int>(StringComparer.Ordinal);
		var index = 0;
		foreach (var layerName in config.LayerNames)
		{
			if (!slots.ContainsKey(layerName))
			{
				slots.Add(layerName, index++ % PaletteSlotCount + 1);
			}
		}

		return slots.ToImmutable();
	}

	private static void AddLayerIndicator(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableDictionary<string, int> paletteSlots, ImmutableArray<ArchitectureLayerIndicator>.Builder indicators, ImmutableArray<ArchitectureLayerIndicator>.Builder unclassifiedIndicators, CancellationToken cancellationToken)
	{
		if (semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) is not ITypeSymbol typeSymbol)
		{
			return;
		}

		var match = config.FindLayer(typeSymbol.Name, typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty, typeSymbol);
		if (match is null)
		{
			unclassifiedIndicators.Add(new ArchitectureLayerIndicator(
				typeDeclaration.Span,
				typeDeclaration.Identifier.Span,
				typeSymbol.Name,
				"not in layer",
				ImmutableArray<string>.Empty,
				"This type is not assigned to any configured AnaalIJzer layer.",
				0,
				false));

			return;
		}

		if (match.Value.Layer.IsForbidden)
		{
			return;
		}

		var layerPath = match.Value.Layer.Name;
		var ancestry = match.Value.Layers.Select(layer => layer.Name).ToImmutableArray();
		var paletteSlot = paletteSlots.TryGetValue(layerPath, out var slot) ? slot : 1;
		var layersThatCanCallThisLayer = GetLayersThatCanCall(config, layerPath);
		var layersThisLayerCanCall = GetLayersThisLayerCanCall(config, layerPath);
		var linearCallChain = GetLinearCallChain(config, layerPath);
		indicators.Add(new ArchitectureLayerIndicator(
			typeDeclaration.Span,
			typeDeclaration.Identifier.Span,
			typeSymbol.Name,
			layerPath,
			ancestry,
			FindLayerDescription(config, layerPath),
			paletteSlot,
			true,
			layersThatCanCallThisLayer,
			layersThisLayerCanCall,
			linearCallChain));
	}

	private static ImmutableArray<string> GetLayersThatCanCall(ProjectAnalyzerConfig config, string layerPath)
	{
		var result = config.Graph.DependencyEdges
			.Where(edge => edge.IsAllowed && EndpointTouchesLayer(edge.To, layerPath))
			.Select(edge => FormatLayerEndpoint(edge.From))
			.Distinct(StringComparer.Ordinal)
			.ToImmutableArray();

		return result;
	}

	private static ImmutableArray<string> GetLayersThisLayerCanCall(ProjectAnalyzerConfig config, string layerPath)
	{
		var result = config.Graph.DependencyEdges
			.Where(edge => edge.IsAllowed && EndpointTouchesLayer(edge.From, layerPath))
			.Select(edge => FormatLayerEndpoint(edge.To))
			.Distinct(StringComparer.Ordinal)
			.ToImmutableArray();

		return result;
	}

	private static string FormatLayerEndpoint(string endpoint)
	{
		var result = endpoint == "*" ? "* (any layer)" : endpoint;

		return result;
	}

	private static ImmutableArray<string> GetLinearCallChain(ProjectAnalyzerConfig config, string layerPath)
	{
		var edges = config.Graph.DependencyEdges
			.Where(edge => edge.IsAllowed && IsDirectLayerEndpoint(edge.From) && IsDirectLayerEndpoint(edge.To))
			.Select(edge => (edge.From, edge.To))
			.Distinct()
			.ToArray();
		if (edges.Length == 0)
		{
			return ImmutableArray<string>.Empty;
		}

		var nodes = edges.Select(edge => edge.From).Concat(edges.Select(edge => edge.To)).Distinct(StringComparer.Ordinal).ToImmutableHashSet(StringComparer.Ordinal);
		if (!nodes.Contains(layerPath))
		{
			return ImmutableArray<string>.Empty;
		}

		var outgoing = BuildEdgeLookup(edges, edge => edge.From, edge => edge.To);
		var incoming = BuildEdgeLookup(edges, edge => edge.To, edge => edge.From);
		var component = GetConnectedComponent(layerPath, nodes, outgoing, incoming);
		if (component.Any(node => GetLookupValues(outgoing, node).Length > 1 || GetLookupValues(incoming, node).Length > 1))
		{
			return ImmutableArray<string>.Empty;
		}

		var starts = component.Where(node => GetLookupValues(incoming, node).Length == 0).ToArray();
		if (starts.Length != 1)
		{
			return ImmutableArray<string>.Empty;
		}

		var chain = ImmutableArray.CreateBuilder<string>();
		var seen = new HashSet<string>(StringComparer.Ordinal);
		var current = starts[0];
		while (seen.Add(current))
		{
			chain.Add(current);
			var next = GetLookupValues(outgoing, current);
			if (next.Length == 0)
			{
				break;
			}

			current = next[0];
		}

		var result = chain.Count == component.Count && chain.Count > 1 && chain.Contains(layerPath, StringComparer.Ordinal)
			? chain.ToImmutable()
			: ImmutableArray<string>.Empty;

		return result;
	}

	private static bool IsDirectLayerEndpoint(string endpoint)
	{
		var result = !string.IsNullOrWhiteSpace(endpoint) && endpoint != "*";

		return result;
	}

	private static ImmutableDictionary<string, ImmutableArray<string>> BuildEdgeLookup(
		IEnumerable<(string From, string To)> edges,
		Func<(string From, string To), string> keySelector,
		Func<(string From, string To), string> valueSelector)
	{
		var groups = edges
			.GroupBy(keySelector, StringComparer.Ordinal)
			.ToImmutableDictionary(
				group => group.Key,
				group => group.Select(valueSelector).Distinct(StringComparer.Ordinal).ToImmutableArray(),
				StringComparer.Ordinal);

		return groups;
	}

	private static ImmutableArray<string> GetLookupValues(ImmutableDictionary<string, ImmutableArray<string>> lookup, string key)
	{
		var result = lookup.TryGetValue(key, out var values) ? values : ImmutableArray<string>.Empty;

		return result;
	}

	private static ImmutableHashSet<string> GetConnectedComponent(
		string layerPath,
		ImmutableHashSet<string> nodes,
		ImmutableDictionary<string, ImmutableArray<string>> outgoing,
		ImmutableDictionary<string, ImmutableArray<string>> incoming)
	{
		var component = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
		var stack = new Stack<string>();
		stack.Push(layerPath);
		while (stack.Count > 0)
		{
			var current = stack.Pop();
			if (!nodes.Contains(current) || !component.Add(current))
			{
				continue;
			}

			foreach (var next in GetLookupValues(outgoing, current).Concat(GetLookupValues(incoming, current)))
			{
				stack.Push(next);
			}
		}

		var result = component.ToImmutable();

		return result;
	}

	private static string? FindLayerDescription(ProjectAnalyzerConfig config, string layerPath)
	{
		var result = config.Documentation.Items.FirstOrDefault(item => item.Kind == "Layer" && item.LayerPath == layerPath).Description;

		return result;
	}

	private static string? FindDependencyRuleDescription(ProjectAnalyzerConfig config, DependencyEdge edge)
	{
		var kind = edge.IsBlocked ? "BlockedDependency" : "AllowedDependency";
		var result = config.Documentation.Items
			.FirstOrDefault(item => item.Kind == kind
			                        && string.Equals(item.SourcePath, edge.XmlPath, StringComparison.OrdinalIgnoreCase)
			                        && item.XmlLineNumber == edge.XmlLineNumber)
			.Description;

		return result;
	}
}
