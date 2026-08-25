using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.Contracts;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Engine.EntryPoints;
using RonSijm.AnaalIJzer.Exceptions;
using RonSijm.AnaalIJzer.SourceLocations;
using RonSijm.AnaalIJzer.Engine.LayerModel;

namespace RonSijm.AnaalIJzer.Config.Parsing;

public static partial class ArchitecturalConfigParser
{
	internal static ImmutableArray<LayerNode> ParseLayerCollection(IEnumerable<ArchitectureConfigurationElementInput> layerElements, string parentPath, List<string> layerNames, Dictionary<string, LayerNode> nodesByPath, ISet<string> declaredLayerPaths, ImmutableDictionary<string, ImmutableHashSet<string>>.Builder layerRequiredRecognizedDependencySites, ArchitectureExceptionPolicy exceptionPolicy, ImmutableArray<ArchitectureExceptionDefinition>.Builder exceptionDefinitions, ImmutableArray<ArchitectureExceptionReview>.Builder exceptionReviews, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var nodes = ImmutableArray.CreateBuilder<LayerNode>();
		var seenNames = new HashSet<string>(StringComparer.Ordinal);
		var exactAssignments = new Dictionary<string, string>(StringComparer.Ordinal);

		foreach (var layerInput in layerElements)
		{
			var layerEl = layerInput.Element;
			var xmlPath = layerInput.Path;
			var isInlineConfiguration = layerInput.IsInlineConfiguration;
			var configuredName = layerEl.Attribute("name")?.Value;
			if (string.IsNullOrWhiteSpace(configuredName))
			{
				continue;
			}
			var localName = configuredName!;

			if (localName.Contains('/'))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Layer name '{localName}' may not contain '/'.", layerEl, xmlPath);
				continue;
			}

			if (!seenNames.Add(localName))
			{
				var scopeDescription = string.IsNullOrEmpty(parentPath) ? "the root" : $"layer '{parentPath}'";
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Layer '{localName}' is declared more than once in {scopeDescription}.", layerEl, xmlPath);
				continue;
			}

			foreach (var classEl in layerEl.Elements("Class"))
			{
				var exactName = classEl.Attribute("typeName")?.Value ?? classEl.Attribute("exactName")?.Value;
				if (exactName is null)
				{
					continue;
				}

				if (exactAssignments.TryGetValue(exactName, out var existingLayer))
				{
					AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Exact type '{exactName}' is assigned more than once (layers '{existingLayer}' and '{localName}').", classEl, xmlPath);
				}
				else
				{
					exactAssignments.Add(exactName, localName);
				}
			}

			var validLocalName = localName!;
			var canonicalPath = string.IsNullOrEmpty(parentPath) ? validLocalName : parentPath + "/" + validLocalName;
			if (ParseRequiredRecognizedDependencySitesAttribute(layerEl, xmlPath, $"Layer '{canonicalPath}'", issues) is { } layerSites)
			{
				layerRequiredRecognizedDependencySites[canonicalPath] = layerSites;
			}

			var definition = LayerDefinition.Normal(canonicalPath, null);
			var matchers = ImmutableArray.CreateBuilder<(PatternMatcher Matcher, MatcherRule Rule)>();
			foreach (var matcherElement in layerEl.Elements().Where(element => element.Name.LocalName is "Class" or "Namespace" or "Assembly"))
			{
				var target = matcherElement.Name.LocalName switch
				{
					"Namespace" => MatchTarget.Namespace,
					"Assembly" => MatchTarget.Assembly,
					_ => MatchTarget.TypeName
				};
				if (ArchitectureConfigurationMatcherReader.TryReadMatcher(matcherElement, target, out var matcher))
				{
					matchers.Add((matcher, CreateRule(matcherElement, definition, ParseExceptions(matcherElement, xmlPath, canonicalPath, exceptionPolicy, exceptionDefinitions, exceptionReviews), xmlPath)));
				}
			}

			layerNames.Add(canonicalPath);
			var children = ParseLayerCollection(layerEl.Elements("Layer").Select(element => new ArchitectureConfigurationElementInput(element, xmlPath, isInlineConfiguration)), canonicalPath, layerNames, nodesByPath, declaredLayerPaths, layerRequiredRecognizedDependencySites, exceptionPolicy, exceptionDefinitions, exceptionReviews, issues);
			var allowedTypeMatchers = ParseTypePolicyMatchers(layerEl.Elements("Allowed").Select(element => new ArchitectureConfigurationElementInput(element, xmlPath, isInlineConfiguration)), definition, false, exceptionPolicy, exceptionDefinitions, exceptionReviews);
			var forbiddenTypeMatchers = ParseTypePolicyMatchers(layerEl.Elements("Forbidden").Select(element => new ArchitectureConfigurationElementInput(element, xmlPath, isInlineConfiguration)), definition, true, exceptionPolicy, exceptionDefinitions, exceptionReviews);
			var nameRules = ParseNameRules(layerEl.Elements("NameRules"), canonicalPath, xmlPath, issues);
			var contractPolicies = ParseContractPolicies(layerEl.Elements("ContractPolicy"), canonicalPath, xmlPath, issues);
			var inheritancePolicies = ParseInheritancePolicies(layerEl.Elements("InheritancePolicy"), canonicalPath, xmlPath, issues);
			var visibilityPolicies = ParseVisibilityPolicies(layerEl.Elements("VisibilityPolicy"), canonicalPath, xmlPath, issues);
			var apiSurfacePolicies = ParseApiSurfacePolicies(layerEl.Elements("ApiSurface"), canonicalPath, xmlPath, declaredLayerPaths, issues);
			var entryPointPolicies = ParseBoundaryEntryPointPolicies(layerEl.Elements("EntryPoints"), canonicalPath, xmlPath, nodesByPath, exceptionPolicy, exceptionDefinitions, exceptionReviews, issues);
			var sourceLocationPolicies = ParseSourceLocationPolicies(layerEl.Elements("SourceLocations"), canonicalPath, xmlPath, isInlineConfiguration, issues);
			if (matchers.Count == 0 && children.Length == 0)
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Layer '{canonicalPath}' does not contain a matcher or nested layer.", layerEl, xmlPath);
			}

			var node = new LayerNode(definition, matchers.ToImmutable(), children, allowedTypeMatchers, forbiddenTypeMatchers, nameRules, contractPolicies, inheritancePolicies, visibilityPolicies, apiSurfacePolicies, entryPointPolicies, sourceLocationPolicies);
			nodes.Add(node);
			nodesByPath[canonicalPath] = node;
		}

		return nodes.ToImmutable();
	}
}

