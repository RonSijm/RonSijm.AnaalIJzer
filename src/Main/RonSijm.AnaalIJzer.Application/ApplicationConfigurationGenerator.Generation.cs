using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ApplicationConfigurationGenerator
{
	public static string Generate(Compilation compilation, string schemaFileName, ConfigurationGenerationOptions options, CancellationToken cancellationToken)
	{
		var types = CompilationTypeCollector.GetProjectTypes(compilation, cancellationToken);
		if (types.Count == 0)
		{
			throw new ApplicationOperationException("The project does not contain any source-defined types to classify.");
		}

		var (layers, typeLayers) = InferLayers(types, compilation.AssemblyName);
		var observedEdges = DiscoverDependencies(compilation, typeLayers, cancellationToken);
		var edges = SelectEdges(observedEdges, options);
		var root = new XElement(
			"ArchitecturalLevels",
			new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName),
			new XAttribute(Xsi + "noNamespaceSchemaLocation", schemaFileName),
			new XAttribute("description", GetRootDescription(compilation.AssemblyName, options)));

		foreach (var layer in layers)
		{
			var layerElement = new XElement("Layer", new XAttribute("name", layer.Name), new XAttribute("description", layer.Description));
			foreach (var assemblyName in layer.AssemblyNames)
			{
				var matcher = new XElement("Assembly", new XAttribute("exactName", assemblyName), new XAttribute("description", $"Types compiled into {assemblyName}."));
				AddExceptions(matcher, layer.ExceptionTypes.Where(type => string.Equals(type.ContainingAssembly?.Name, assemblyName, StringComparison.Ordinal)));
				layerElement.Add(matcher);
			}

			foreach (var namespacePrefix in layer.NamespacePrefixes)
			{
				var pattern = "^" + Regex.Escape(namespacePrefix) + @"(?:\.|$)";
				var matcher = new XElement("Namespace", new XAttribute("regex", pattern), new XAttribute("description", $"Types in {namespacePrefix} and its child namespaces."));
				AddExceptions(matcher, layer.ExceptionTypes.Where(type => IsInNamespace(type, namespacePrefix)));
				layerElement.Add(matcher);
			}

			foreach (var type in DistinctTypes(layer.ExactTypes))
			{
				var attributeName = type.ContainingType is null ? "exactFullName" : "exactName";
				var matchValue = type.ContainingType is null ? GetAnalyzerFullName(type) : type.Name;
				var matcher = new XElement("Class", new XAttribute(attributeName, matchValue), new XAttribute("description", $"Exact match for {type.ToDisplayString()}."));
				if (layer.ExceptionTypes.Contains(type.OriginalDefinition, SymbolEqualityComparer.Default))
				{
					AddExceptions(matcher, [type]);
				}

				layerElement.Add(matcher);
			}

			root.Add(layerElement);
		}

		foreach (var edge in edges)
		{
			var sites = string.Join(", ", DependencySites.All.Where(edge.Sites.Contains));
			root.Add(new XElement(
				"AllowedDependency",
				new XAttribute("from", edge.From.Name),
				new XAttribute("to", edge.To.Name),
				new XAttribute("allowedSites", sites),
				new XAttribute("description", GetEdgeDescription(edge, sites))));
		}

		var result = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine + root + Environment.NewLine;

		return result;
	}

	public static string Generate(SolutionAnalysisResult solution, string schemaFileName, ConfigurationGenerationOptions options, CancellationToken cancellationToken)
	{
		var (layers, layersByAssemblyName) = InferSolutionLayers(solution, cancellationToken);
		if (layers.Count == 0)
		{
			throw new ApplicationOperationException("The solution does not contain any C# projects with source-defined types to classify.");
		}

		var observedEdges = DiscoverSolutionDependencies(solution, layersByAssemblyName, cancellationToken);
		var edges = SelectEdges(observedEdges, options);
		var root = new XElement(
			"ArchitecturalLevels",
			new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName),
			new XAttribute(Xsi + "noNamespaceSchemaLocation", schemaFileName),
			new XAttribute("description", GetSolutionRootDescription(solution.SolutionName, options)));

		foreach (var layer in layers)
		{
			var layerElement = new XElement("Layer", new XAttribute("name", layer.Name), new XAttribute("description", layer.Description));
			foreach (var assemblyName in layer.AssemblyNames)
			{
				var matcher = new XElement("Assembly", new XAttribute("exactName", assemblyName), new XAttribute("description", $"Types compiled into {assemblyName}."));
				AddExceptions(matcher, layer.ExceptionTypes.Where(type => string.Equals(type.ContainingAssembly?.Name, assemblyName, StringComparison.Ordinal)));
				layerElement.Add(matcher);
			}

			root.Add(layerElement);
		}

		foreach (var edge in edges)
		{
			var sites = string.Join(", ", DependencySites.All.Where(edge.Sites.Contains));
			root.Add(new XElement(
				"AllowedDependency",
				new XAttribute("from", edge.From.Name),
				new XAttribute("to", edge.To.Name),
				new XAttribute("allowedSites", sites),
				new XAttribute("description", GetEdgeDescription(edge, sites))));
		}

		var result = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine + root + Environment.NewLine;

		return result;
	}
}

