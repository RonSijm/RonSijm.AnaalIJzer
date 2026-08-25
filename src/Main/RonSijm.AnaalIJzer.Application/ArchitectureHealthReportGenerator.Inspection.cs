using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer;
using RonSijm.AnaalIJzer.Exceptions;
using RonSijm.AnaalIJzer.Findings;
using RonSijm.AnaalIJzer.Model;
using RonSijm.AnaalIJzer.ObservedDependencies;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ArchitectureHealthReportGenerator
{
	private static List<ArchitectureFinding> InspectProject(ProjectAnalysisResult result, CancellationToken cancellationToken)
	{
		var findings = InspectProjects([result], cancellationToken);

		return findings;
	}

	private static List<ArchitectureFinding> InspectProjects(IReadOnlyList<ProjectAnalysisResult> projects, CancellationToken cancellationToken)
	{
		var findings = new List<ArchitectureFinding>();
		var config = projects[0].Config;
		var types = GetDistinctProjectTypes(projects, cancellationToken);
		var matcherRules = GetMatcherRules(config);
		var effectiveLayers = new Dictionary<INamedTypeSymbol, string?>(SymbolEqualityComparer.Default);
		foreach (var type in types)
		{
			var namespaceName = GetNamespace(type);
			effectiveLayers[type] = config.Engine.FindLayer(type.Name, namespaceName, type)?.Layer.Name;
		}

		foreach (var type in types.Where(type => effectiveLayers[type] is null).OrderBy(GetTypeName, StringComparer.Ordinal))
		{
			findings.Add(new ArchitectureFinding(ArchitectureFindingSeverity.Warning, ArchitectureFindingCodes.UnclassifiedType, GetTypeName(type), GetProjectContext(type)));
		}

		foreach (var rule in matcherRules.Where(rule => !rule.IsException))
		{
			if (!types.Any(type => RuleMatches(rule, type)))
			{
				findings.Add(new ArchitectureFinding(ArchitectureFindingSeverity.Warning, ArchitectureFindingCodes.UnmatchedMatcher, $"{rule.ParentKind} '{rule.ParentLabel}' / {rule.Item.Kind} '{rule.Item.Label}'", FormatRuleLocation(rule.Item)));
			}
		}

		foreach (var definition in config.ExceptionDefinitions)
		{
			if (!definition.IsActive)
			{
				continue;
			}

			if (types.Any(type => definition.Matcher.TryMatch(type.Name, GetNamespace(type), type) is not null))
			{
				continue;
			}

			findings.Add(new ArchitectureFinding(
				ArchitectureFindingSeverity.Warning,
				ArchitecturalDiagnosticIds.ExceptionReview,
				ArchitectureExceptionEvaluator.CreateStaleMessage(definition, projects.Count > 1 ? "solution" : "project"),
				FormatExceptionLocation(definition.XmlPath, definition.XmlLineNumber),
				ArchitectureExceptionStatus.Stale.ToString(),
				ArchitectureExceptionStatus.Stale.ToString()));
		}

		foreach (var type in types)
		{
			var matchingLayers = matcherRules
				.Where(rule => !rule.IsException && rule.ParentKind == "Layer" && RuleMatches(rule, type))
				.Select(rule => rule.Item.LayerPath)
				.Distinct(StringComparer.Ordinal)
				.OrderBy(layer => layer, StringComparer.Ordinal)
				.ToArray();
			if (HasDivergentPaths(matchingLayers))
			{
				findings.Add(new ArchitectureFinding(ArchitectureFindingSeverity.Warning, ArchitectureFindingCodes.AmbiguousLayerMatch, GetTypeName(type), string.Join(", ", matchingLayers)));
			}
		}

		string? ResolveLayer(INamedTypeSymbol type)
		{
			var result = config.Engine.FindLayer(type.Name, GetNamespace(type), type)?.Layer.Name;

			return result;
		}

		var observations = projects
			.SelectMany(project => ProjectDependencyScanner.Scan(project.Compilation, ResolveLayer, cancellationToken))
			.ToArray();
		foreach (var edge in config.Graph.DependencyEdges.Where(edge => edge.IsAllowed))
		{
			var used = observations.Any(observation =>
				config.Graph.Matches(edge, observation.CallerLayer, observation.DependencyLayer)
				&& edge.AllowsSite(observation.Site)
				&& config.Graph.EvaluateDependency(observation.CallerLayer, observation.DependencyLayer, observation.Site).IsAllowed);
			if (!used)
			{
				findings.Add(new ArchitectureFinding(ArchitectureFindingSeverity.Warning, ArchitectureFindingCodes.UnusedAllowedEdge, $"{edge.From} -> {edge.To}", edge.SiteFilter.HasFilter ? edge.SiteFilter.ToDisplayText() : "all sites"));
			}
		}

		var observedCycles = ObservedDependencyCycleEvaluator.FindCycles(
			config.LayerNames,
			observations.Select(observation => observation.ToObservedDependency(observation.CallerType.ContainingAssembly?.Name)),
			projects.Count > 1 ? "Solution" : "Project");
		foreach (var cycle in observedCycles)
		{
			var severity = config.EnforceObservedAcyclic ? ArchitectureFindingSeverity.Error : ArchitectureFindingSeverity.Warning;
			var category = config.EnforceObservedAcyclic ? ArchitecturalDiagnosticIds.ObservedDependencyCycle : "Observed dependency cycle";
			var context = string.Join(
				"; ",
				cycle.RepresentativeEdges.Select(edge =>
				{
					var location = FormatObservedEdgeLocation(edge, projects);
					var result = $"{edge.CallerLayer} -> {edge.DependencyLayer} at {edge.Site}" + (string.IsNullOrWhiteSpace(location) ? string.Empty : $" ({location})");

					return result;
				}));
			findings.Add(new ArchitectureFinding(severity, category, cycle.GetDisplayPath(), string.IsNullOrWhiteSpace(context) ? "based on current project code" : context));
		}

		foreach (var project in projects)
		{
			var projectName = project.AssemblyName ?? Path.GetFileNameWithoutExtension(project.ProjectPath);
			foreach (var diagnostic in project.AnalyzerDiagnostics.Where(diagnostic => diagnostic.Id is not ArchitecturalDiagnosticIds.InvalidConfiguration and not ArchitecturalDiagnosticIds.CyclicDependencyGraph))
			{
				var context = AddProjectContext(projectName, FormatDiagnosticLocation(diagnostic, project.ProjectDirectory));
				findings.Add(ArchitectureFindingFactory.FromDiagnostic(diagnostic, context));
			}
		}

		return findings;
	}
}

