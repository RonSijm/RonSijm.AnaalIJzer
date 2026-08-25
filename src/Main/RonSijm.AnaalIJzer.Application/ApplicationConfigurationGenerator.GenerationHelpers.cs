using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ApplicationConfigurationGenerator
{
	private static string GetRootDescription(string? assemblyName, ConfigurationGenerationOptions options)
	{
		var result = options.Strategy switch
		{
			ConfigurationGenerationStrategy.Conventions => $"Generated convention architecture for {assemblyName ?? "the project"} using minimum confidence {options.MinimumConfidence * 100:F0}% and minimum support {options.MinimumSupport}. Minority dependencies are grandfathered as layer matcher exceptions.",
			ConfigurationGenerationStrategy.Helpful => $"Generated helpful baseline architecture for {assemblyName ?? "the project"}. Review inferred layers and observed dependency edges before tightening it.",
			_ => $"Generated snapshot architecture for {assemblyName ?? "the project"}. Review inferred layers and observed dependency edges before adopting it."
		};

		return result;
	}

	private static string GetSolutionRootDescription(string solutionName, ConfigurationGenerationOptions options)
	{
		var result = options.Strategy switch
		{
			ConfigurationGenerationStrategy.Conventions => $"Generated convention architecture for solution {solutionName} using minimum confidence {options.MinimumConfidence * 100:F0}% and minimum support {options.MinimumSupport}. Minority dependencies are grandfathered as layer matcher exceptions.",
			ConfigurationGenerationStrategy.Helpful => $"Generated helpful solution baseline for {solutionName}. Each C# project is represented as an assembly layer, with observed inter-project dependency sites allowed.",
			_ => $"Generated solution snapshot for {solutionName}. Each C# project is represented as an assembly layer, with observed dependency sites allowed."
		};

		return result;
	}

	private static string GetEdgeDescription(GeneratedEdge edge, string sites)
	{
		var result = edge.Disposition switch
		{
			EdgeDisposition.Convention =>
				$"Inferred convention: {edge.CallerCount} of {edge.ActiveCallerCount} active {edge.From.Name} callers ({(double)edge.CallerCount / edge.ActiveCallerCount * 100:F0}%) depend on {edge.To.Name}. Observed sites: {sites}.",
			EdgeDisposition.AmbiguousSnapshot =>
				$"Observed {edge.CallerCount} of {edge.ActiveCallerCount} active {edge.From.Name} callers depending on {edge.To.Name} at: {sites}. Preserved because this layer had no dependency edge with enough confidence and support to establish a convention.",
			_ => $"Observed {edge.CallerCount} {edge.From.Name} caller(s) depending on {edge.To.Name} at: {sites}."
		};

		return result;
	}

	private static void AddExceptions(XElement matcher, IEnumerable<INamedTypeSymbol> exceptionTypes)
	{
		var types = DistinctTypes(exceptionTypes).OrderBy(GetAnalyzerFullName, StringComparer.Ordinal).ToArray();
		if (types.Length == 0)
		{
			return;
		}

		var exceptions = new XElement("Exceptions", new XAttribute("description", "Generated exceptions for callers that do not follow an inferred dependency convention."));
		foreach (var type in types)
		{
			exceptions.Add(new XElement(
				"Class",
				new XAttribute("exactFullName", GetAnalyzerFullName(type)),
				new XAttribute("description", $"Grandfathers the observed dependencies of {type.ToDisplayString()}.")));
		}

		matcher.Add(exceptions);
	}

	private static bool IsInNamespace(INamedTypeSymbol type, string namespacePrefix)
	{
		var namespaceName = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();
		var result = namespaceName == namespacePrefix || namespaceName.StartsWith(namespacePrefix + ".", StringComparison.Ordinal);

		return result;
	}
}

