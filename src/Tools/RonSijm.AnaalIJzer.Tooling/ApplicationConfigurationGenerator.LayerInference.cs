using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Diagnostics;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Tooling;

internal static partial class ApplicationConfigurationGenerator
{
	private static (IReadOnlyList<GeneratedLayer> Layers, Dictionary<INamedTypeSymbol, GeneratedLayer> TypeLayers) InferLayers(IReadOnlyList<INamedTypeSymbol> types, string? assemblyName)
	{
		var baseNamespace = FindBaseNamespace(types, assemblyName);
		var layers = new List<GeneratedLayer>();
		var layersByName = new Dictionary<string, GeneratedLayer>(StringComparer.Ordinal);
		var typeLayers = new Dictionary<INamedTypeSymbol, GeneratedLayer>(SymbolEqualityComparer.Default);

		foreach (var type in types)
		{
			var namespaceName = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();
			var hasNamespaceLayer = TryGetNamespaceLayer(namespaceName, baseNamespace, out var layerName, out var namespacePrefix);
			if (!hasNamespaceLayer)
			{
				layerName = GetTypeLayerName(type.Name);
			}

			if (!layersByName.TryGetValue(layerName, out var layer))
			{
				layer = new GeneratedLayer(layerName, $"Inferred {layerName} layer from the project's namespaces and type names.");
				layersByName.Add(layerName, layer);
				layers.Add(layer);
			}

			if (hasNamespaceLayer)
			{
				if (!layer.NamespacePrefixes.Contains(namespacePrefix, StringComparer.Ordinal))
				{
					layer.NamespacePrefixes.Add(namespacePrefix);
				}
			}
			else
			{
				layer.ExactTypes.Add(type);
			}

			typeLayers[type.OriginalDefinition] = layer;
		}

		return (layers, typeLayers);
	}

	private static string FindBaseNamespace(IReadOnlyList<INamedTypeSymbol> types, string? assemblyName)
	{
		var namespaces = types
			.Select(type => type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString())
			.Where(value => value.Length > 0)
			.Distinct(StringComparer.Ordinal)
			.ToArray();
		if (namespaces.Length == 0)
		{
			return string.Empty;
		}

		if (!string.IsNullOrWhiteSpace(assemblyName)
		    && namespaces.All(value => value == assemblyName || value.StartsWith(assemblyName + ".", StringComparison.Ordinal)))
		{
			return assemblyName;
		}

		if (namespaces.Length == 1)
		{
			var separator = namespaces[0].LastIndexOf('.');
			return separator < 0 ? string.Empty : namespaces[0][..separator];
		}

		var segments = namespaces.Select(value => value.Split('.')).ToArray();
		var commonLength = 0;
		while (segments.All(parts => parts.Length > commonLength && parts[commonLength] == segments[0][commonLength]))
		{
			commonLength++;
		}

		return string.Join(".", segments[0].Take(commonLength));
	}

	private static bool TryGetNamespaceLayer(string namespaceName, string baseNamespace, out string layerName, out string namespacePrefix)
	{
		var remainder = namespaceName;
		if (baseNamespace.Length > 0)
		{
			if (namespaceName == baseNamespace)
			{
				layerName = string.Empty;
				namespacePrefix = string.Empty;
				return false;
			}

			if (namespaceName.StartsWith(baseNamespace + ".", StringComparison.Ordinal))
			{
				remainder = namespaceName[(baseNamespace.Length + 1)..];
			}
		}

		if (remainder.Length == 0)
		{
			layerName = string.Empty;
			namespacePrefix = string.Empty;
			return false;
		}

		layerName = remainder.Split('.')[0];
		namespacePrefix = baseNamespace.Length > 0 ? baseNamespace + "." + layerName : layerName;
		return true;
	}

	private static string GetTypeLayerName(string typeName)
	{
		foreach (var suffix in CommonTypeSuffixes)
		{
			if (typeName.EndsWith(suffix, StringComparison.Ordinal))
			{
				return suffix;
			}
		}

		return "Core";
	}
}
