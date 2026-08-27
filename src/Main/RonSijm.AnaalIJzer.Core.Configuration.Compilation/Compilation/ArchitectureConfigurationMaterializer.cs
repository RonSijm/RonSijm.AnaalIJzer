using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Configuration.Compilation.Parsing;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.Exceptions;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Core.Matchers;
using RonSijm.AnaalIJzer.Core.PolicyEvaluation.Engine.DependencyRules;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Compilation;

internal static class ArchitectureConfigurationMaterializer
{
	internal static ArchitectureConfigurationMaterializationResult Materialize(
		ImmutableArray<ArchitectureConfigurationElementInput> elements,
		ArchitectureConfigurationRootSettings rootSettings,
		string configPath,
		ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var exceptionDefinitions = ImmutableArray.CreateBuilder<ArchitectureExceptionDefinition>();
		var exceptionReviews = ImmutableArray.CreateBuilder<ArchitectureExceptionReview>();

		var forbiddenTypeNames = new Dictionary<string, MatcherRule>(StringComparer.Ordinal);
		var forbiddenMatchers = new List<(PatternMatcher Matcher, MatcherRule Rule)>();
		var layerNames = new List<string>();
		var layerNodesByPath = new Dictionary<string, LayerNode>(StringComparer.Ordinal);
		var layerRequiredRecognizedDependencySites = ImmutableDictionary.CreateBuilder<string, ImmutableHashSet<string>>(StringComparer.Ordinal);
		var forbiddenPatterns = new List<ArchitectureForbiddenPattern>();
		var rootLayerElements = elements.Where(item => item.Element.Name.LocalName == "Layer").ToArray();
		var declaredLayerPaths = ArchitecturalConfigParser.CollectDeclaredLayerPaths(rootLayerElements);
		var roots = ArchitecturalConfigParser.ParseLayerCollection(rootLayerElements, string.Empty, layerNames, layerNodesByPath, declaredLayerPaths, layerRequiredRecognizedDependencySites, rootSettings.ExceptionPolicy, exceptionDefinitions, exceptionReviews, issues);
		var allowedTypeMatchers = ArchitecturalConfigParser.ParseTypePolicyMatchers(elements.Where(item => item.Element.Name.LocalName == "Allowed"), LayerDefinition.Normal("global", null), false, rootSettings.ExceptionPolicy, exceptionDefinitions, exceptionReviews);

		foreach (var forbiddenInput in elements.Where(elementInput => elementInput.Element.Name.LocalName == "Forbidden"))
		{
			MaterializeForbiddenTypePolicies(
				forbiddenInput,
				rootSettings.ExceptionPolicy,
				forbiddenTypeNames,
				forbiddenMatchers,
				forbiddenPatterns,
				exceptionDefinitions,
				exceptionReviews);
		}

		var edgeBuilder = ImmutableArray.CreateBuilder<DependencyEdge>();
		ArchitecturalConfigParser.ParseDependencyEdges(elements.Where(item => item.Element.Name.LocalName is "AllowedDependency" or "BlockedDependency"), string.Empty, layerNodesByPath, edgeBuilder, issues);
		foreach (var rootLayer in rootLayerElements)
		{
			ArchitecturalConfigParser.ParseNestedDependencyEdges(rootLayer.Element, rootLayer.Path, rootLayer.Element.Attribute("name")?.Value ?? string.Empty, layerNodesByPath, edgeBuilder, issues);
		}

		var layerCatalog = new CompiledLayerCatalog(
			roots,
			layerNodesByPath.ToImmutableDictionary(StringComparer.Ordinal),
			forbiddenTypeNames.ToImmutableDictionary(StringComparer.Ordinal),
			[..forbiddenMatchers],
			allowedTypeMatchers);
		var dependencyEdges = edgeBuilder.ToImmutable();
		if (rootSettings.EnforceAcyclic)
		{
			AddConfiguredCycleIssues(layerNames, dependencyEdges, configPath, issues);
		}

		var result = new ArchitectureConfigurationMaterializationResult(
			layerCatalog,
			dependencyEdges,
			layerRequiredRecognizedDependencySites.ToImmutable(),
			exceptionDefinitions.ToImmutable(),
			exceptionReviews.ToImmutable(),
			[..layerNames],
			[..forbiddenPatterns],
			ArchitecturalConfigParser.ParseProjectArchitecture(elements, configPath, issues));

		return result;
	}

	private static void MaterializeForbiddenTypePolicies(
		ArchitectureConfigurationElementInput forbiddenInput,
		ArchitectureExceptionPolicy exceptionPolicy,
		Dictionary<string, MatcherRule> forbiddenTypeNames,
		List<(PatternMatcher Matcher, MatcherRule Rule)> forbiddenMatchers,
		List<ArchitectureForbiddenPattern> forbiddenPatterns,
		ImmutableArray<ArchitectureExceptionDefinition>.Builder exceptionDefinitions,
		ImmutableArray<ArchitectureExceptionReview>.Builder exceptionReviews)
	{
		var forbiddenContainer = forbiddenInput.Element;
		var xmlPath = forbiddenInput.Path;

		foreach (var classElement in forbiddenContainer.Elements("Class"))
		{
			var comment = classElement.Attribute("comment")?.Value;
			var fixSuffix = classElement.Element("Fix")?.Attribute("Rename")?.Value;
			var forbiddenName = ArchitectureConfigurationMatcherReader.GetPrimaryMatcherValue(classElement) ?? "Forbidden";
			var layerDefinition = LayerDefinition.Forbidden(forbiddenName, comment, fixSuffix);
			ArchitecturalConfigParser.ParseClassElement(classElement, layerDefinition, xmlPath, forbiddenTypeNames, forbiddenMatchers, exceptionPolicy, exceptionDefinitions, exceptionReviews);
			forbiddenPatterns.Add(new ArchitectureForbiddenPattern(forbiddenName, comment));
		}

		foreach (var namespaceElement in forbiddenContainer.Elements("Namespace"))
		{
			var comment = namespaceElement.Attribute("comment")?.Value;
			var forbiddenName = ArchitectureConfigurationMatcherReader.GetPrimaryMatcherValue(namespaceElement) ?? "ForbiddenNamespace";
			var layerDefinition = LayerDefinition.Forbidden(forbiddenName, comment, null);
			ArchitecturalConfigParser.ParseNamespaceElement(namespaceElement, layerDefinition, xmlPath, forbiddenMatchers, exceptionPolicy, exceptionDefinitions, exceptionReviews);
			forbiddenPatterns.Add(new ArchitectureForbiddenPattern(forbiddenName, comment));
		}
	}

	private static void AddConfiguredCycleIssues(
		IReadOnlyList<string> layerNames,
		ImmutableArray<DependencyEdge> dependencyEdges,
		string configPath,
		ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		foreach (var cycle in DependencyCycleDetector.FindConfiguredCycles(layerNames, dependencyEdges))
		{
			var firstEdge = dependencyEdges.FirstOrDefault(edge => edge.IsAllowed && edge.From == cycle[0] && edge.To == cycle[1]);
			var message = $"Configured allowed-dependency cycle: {string.Join(" -> ", cycle)} -> {cycle[0]}.";
			issues.Add(new ConfigurationIssue(ConfigurationIssueKind.CyclicDependencyGraph, message, firstEdge.XmlPath ?? configPath, firstEdge.XmlLineNumber, firstEdge.XmlLinePosition));
		}
	}
}
