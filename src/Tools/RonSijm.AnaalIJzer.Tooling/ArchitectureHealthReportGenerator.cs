using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.DependencyRules;
using RonSijm.AnaalIJzer.Model;
using RonSijm.AnaalIJzer.Parsing;
using RonSijm.AnaalIJzer.Diagnostics;

using RonSijm.AnaalIJzer.Conditions;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Tooling;

internal static class ArchitectureHealthReportGenerator
{
	public static ArchitectureHealthReport Generate(AnalyzerConfiguration config, string? title)
	{
		var findings = GetConfigurationFindings(config);
		return CreateReport(title, findings, null);
	}

	public static ArchitectureHealthReport Generate(ProjectAnalysisResult result, CancellationToken cancellationToken)
	{
		var findings = GetConfigurationFindings(result.Config);
		var projectFindings = InspectProject(result, cancellationToken);
		findings.AddRange(projectFindings);
		return CreateReport(result.AssemblyName ?? Path.GetFileNameWithoutExtension(result.ProjectPath), findings, result.ProjectPath);
	}

	public static ArchitectureHealthReport Generate(SolutionAnalysisResult result, CancellationToken cancellationToken)
	{
		var findings = new List<HealthFinding>();
		foreach (var project in result.Projects)
		{
			var projectName = project.AssemblyName ?? Path.GetFileNameWithoutExtension(project.ProjectPath);
			var projectFindings = GetConfigurationFindings(project.Config);
			projectFindings.AddRange(InspectProject(project, cancellationToken));
			findings.AddRange(projectFindings.Select(finding => finding with
			{
				Context = AddProjectContext(projectName, finding.Context)
			}));
		}

		return CreateReport(result.SolutionName, findings, result.SolutionPath, "Solution");
	}

	private static List<HealthFinding> GetConfigurationFindings(AnalyzerConfiguration config)
	{
		var findings = config.ConfigurationIssues
			.Where(issue => issue.Kind == ConfigurationIssueKind.InvalidConfiguration)
			.Select(issue => new HealthFinding("Error", "Configuration", issue.Message, FormatConfigLocation(issue)))
			.ToList();
		if (!config.HasLayers && findings.Count == 0)
		{
			findings.Add(new HealthFinding("Error", "Configuration", "No architectural layers were found.", "Add Architecture.anl or AssemblyMetadata(\"AnaalIJzerSettings\", ...)."));
		}

		foreach (var cycle in DependencyCycleDetector.FindConfiguredCycles(config.LayerNames, config.Graph.DependencyEdges))
		{
			findings.Add(new HealthFinding(config.EnforceAcyclic ? "Error" : "Warning", "Configured cycle", $"{string.Join(" -> ", cycle)} -> {cycle[0]}", config.EnforceAcyclic ? "enforceAcyclic is enabled" : "enforceAcyclic is disabled; the graph currently permits this cycle"));
		}

		return findings;
	}

	private static List<HealthFinding> InspectProject(ProjectAnalysisResult result, CancellationToken cancellationToken)
	{
		var findings = new List<HealthFinding>();
		var types = ApplicationConfigurationGenerator.GetProjectTypes(result.Compilation, cancellationToken);
		var matcherRules = GetMatcherRules(result.Config);
		var effectiveLayers = new Dictionary<INamedTypeSymbol, string?>(SymbolEqualityComparer.Default);
		foreach (var type in types)
		{
			var namespaceName = GetNamespace(type);
			effectiveLayers[type] = result.Config.FindLayer(type.Name, namespaceName, type)?.Layer.Name;
		}

		foreach (var type in types.Where(type => effectiveLayers[type] is null).OrderBy(GetTypeName, StringComparer.Ordinal))
		{
			findings.Add(new HealthFinding("Warning", "Unclassified type", GetTypeName(type), string.Empty));
		}

		foreach (var rule in matcherRules.Where(rule => !rule.IsException))
		{
			if (!types.Any(type => RuleMatches(rule, type)))
			{
				findings.Add(new HealthFinding("Warning", "Unmatched matcher", $"{rule.ParentKind} '{rule.ParentLabel}' / {rule.Item.Kind} '{rule.Item.Label}'", FormatRuleLocation(rule.Item)));
			}
		}

		foreach (var rule in matcherRules.Where(rule => rule.IsException))
		{
			if (!types.Any(type => RuleMatches(rule, type)))
			{
				findings.Add(new HealthFinding("Warning", "Stale exception", $"{rule.Item.Kind} '{rule.Item.Label}'", FormatRuleLocation(rule.Item)));
			}
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
				findings.Add(new HealthFinding("Warning", "Ambiguous layer match", GetTypeName(type), string.Join(", ", matchingLayers)));
			}
		}

		string? ResolveLayer(INamedTypeSymbol type)
        {
            return result.Config.FindLayer(type.Name, GetNamespace(type), type)?.Layer.Name;
        }

        var observations = ProjectDependencyScanner.Scan(result.Compilation, ResolveLayer, cancellationToken);
		foreach (var edge in result.Config.Graph.DependencyEdges.Where(edge => edge.IsAllowed))
		{
			var used = observations.Any(observation =>
				result.Config.Graph.Matches(edge, observation.CallerLayer, observation.DependencyLayer)
				&& edge.AllowsSite(observation.Site)
				&& result.Config.Graph.EvaluateDependency(observation.CallerLayer, observation.DependencyLayer, observation.Site).IsAllowed);
			if (!used)
			{
				findings.Add(new HealthFinding("Warning", "Unused allowed edge", $"{edge.From} -> {edge.To}", edge.SiteFilter.HasFilter ? edge.SiteFilter.ToDisplayText() : "all sites"));
			}
		}

		var observedCycles = DependencyCycleDetector.FindCycles(
			result.Config.LayerNames,
			observations.Select(observation => (observation.CallerLayer, observation.DependencyLayer)).Distinct());
		foreach (var cycle in observedCycles)
		{
			findings.Add(new HealthFinding("Warning", "Observed dependency cycle", $"{string.Join(" -> ", cycle)} -> {cycle[0]}", "based on current project code"));
		}

		foreach (var diagnostic in result.AnalyzerDiagnostics.Where(diagnostic => diagnostic.Id is not ArchitecturalDiagnosticIds.InvalidConfiguration and not ArchitecturalDiagnosticIds.CyclicDependencyGraph))
		{
			findings.Add(new HealthFinding("Error", diagnostic.Id, diagnostic.GetMessage(), FormatDiagnosticLocation(diagnostic, result.ProjectDirectory)));
		}

		return findings;
	}

	private static ArchitectureHealthReport CreateReport(string? title, IReadOnlyList<HealthFinding> findings, string? inputPath, string inputPathLabel = "Project")
	{
		var errors = findings.Count(finding => finding.Severity == "Error");
		var warnings = findings.Count - errors;
		var sb = new StringBuilder();
		sb.AppendLine("# Architecture Health");
		sb.AppendLine();
		if (!string.IsNullOrWhiteSpace(title))
		{
			sb.AppendLine($"**Input**: `{Escape(title)}`");
		}
		if (!string.IsNullOrWhiteSpace(inputPath))
		{
			sb.AppendLine($"**{inputPathLabel}**: `{Escape(inputPath)}`");
		}
		sb.AppendLine($"**Findings**: {errors} error(s), {warnings} warning(s)");
		sb.AppendLine();

		if (findings.Count == 0)
		{
			sb.AppendLine("No configuration, classification, dependency-graph, or rule-usage problems were found.");
			return new ArchitectureHealthReport(sb.ToString(), 0);
		}

		sb.AppendLine("| Severity | Category | Finding | Context |");
		sb.AppendLine("|----------|----------|---------|---------|");
		foreach (var finding in findings.OrderByDescending(finding => finding.Severity == "Error").ThenBy(finding => finding.Category, StringComparer.Ordinal).ThenBy(finding => finding.Message, StringComparer.Ordinal))
		{
			sb.AppendLine($"| {EscapeTable(finding.Severity)} | {EscapeTable(finding.Category)} | {EscapeTable(finding.Message)} | {EscapeTable(finding.Context)} |");
		}
		return new ArchitectureHealthReport(sb.ToString(), findings.Count);
	}

	private static string AddProjectContext(string projectName, string context)
	{
		var result = string.IsNullOrWhiteSpace(context)
			? projectName
			: $"{projectName} - {context}";

		return result;
	}

	private static IReadOnlyList<HealthMatcherRule> GetMatcherRules(AnalyzerConfiguration config)
	{
		var rules = new List<HealthMatcherRule>();
		var ancestors = new List<ArchitectureDocumentationItem>();
		foreach (var item in config.Documentation.Items)
		{
			while (ancestors.Count > item.Depth)
			{
				ancestors.RemoveAt(ancestors.Count - 1);
			}

			if (item.Kind is "Class" or "Namespace" or "Assembly" && TryCreateMatcher(item, out var matcher))
			{
				var parent = ancestors.LastOrDefault(ancestor => ancestor.Kind is "Layer" or "Allowed" or "Forbidden");
				var isException = ancestors.Any(ancestor => ancestor.Kind == "Exceptions");
				rules.Add(new HealthMatcherRule(item, parent.Kind, parent.Label, matcher, isException));
			}

			if (ancestors.Count == item.Depth)
			{
				ancestors.Add(item);
			}
			else
			{
				ancestors[item.Depth] = item;
			}
		}
		return rules;
	}

	private static bool TryCreateMatcher(ArchitectureDocumentationItem item, out PatternMatcher matcher)
	{
		var target = item.Kind switch
		{
			"Namespace" => MatchTarget.Namespace,
			"Assembly" => MatchTarget.Assembly,
			_ => MatchTarget.TypeName
		};
		var candidates = new (string Attribute, MatchKind Kind)[]
		{
			("typeName", MatchKind.Equals),
			("exactName", MatchKind.Equals),
			("exactFullName", MatchKind.EqualsFullName),
			("inherits", MatchKind.Inherits),
			("implements", MatchKind.Implements),
			("withAttribute", MatchKind.HasAttribute),
			("withAccessModifier", MatchKind.HasAccessModifier),
			("typeKind", MatchKind.HasTypeKind),
			("endsWith", MatchKind.EndsWith),
			("startsWith", MatchKind.StartsWith),
			("contains", MatchKind.Contains),
			("regex", MatchKind.Regex)
		};
		var conditions = ImmutableArray.CreateBuilder<MatchCondition>();
		foreach (var candidate in candidates)
		{
			if (item.GetAttribute(candidate.Attribute) is { } value)
			{
				conditions.Add(new MatchCondition(candidate.Kind, value));
			}
		}

		matcher = new PatternMatcher(target, conditions.ToImmutable());
		return conditions.Count > 0;
	}

	private static bool RuleMatches(HealthMatcherRule rule, INamedTypeSymbol type)
	{
		var result = rule.Matcher.TryMatch(type.Name, GetNamespace(type), type) is not null;

		return result;
	}

    private static bool HasDivergentPaths(IReadOnlyList<string> paths)
	{
		for (var left = 0; left < paths.Count; left++)
		{
			for (var right = left + 1; right < paths.Count; right++)
			{
				if (!IsAncestor(paths[left], paths[right]) && !IsAncestor(paths[right], paths[left]))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static bool IsAncestor(string ancestor, string descendant)
	{
		var result = descendant == ancestor || descendant.StartsWith(ancestor + "/", StringComparison.Ordinal);

		return result;
	}

	private static string GetNamespace(INamedTypeSymbol type)
	{
		var result = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();

		return result;
	}

	private static string GetTypeName(INamedTypeSymbol type)
	{
		var result = type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

		return result;
	}

	private static string FormatConfigLocation(ConfigurationIssue issue)
	{
		var result = issue.LineNumber > 0 ? $"{issue.Path}:{issue.LineNumber}" : issue.Path;

		return result;
	}

	private static string FormatRuleLocation(ArchitectureDocumentationItem item)
	{
		var result = item.XmlLineNumber > 0 ? $"{item.SourcePath}:{item.XmlLineNumber}" : item.SourcePath;

		return result;
	}

    private static string FormatDiagnosticLocation(Diagnostic diagnostic, string projectDirectory)
	{
		if (!diagnostic.Location.IsInSource)
		{
			return string.Empty;
		}

		var lineSpan = diagnostic.Location.GetLineSpan();
		var path = string.IsNullOrWhiteSpace(lineSpan.Path) ? string.Empty : Path.GetRelativePath(projectDirectory, lineSpan.Path);
		return $"{path}:{lineSpan.StartLinePosition.Line + 1}";
	}

	private static string Escape(string text)
	{
		var result = text.Replace("\r", " ").Replace("\n", " ").Replace("`", "\\`");

		return result;
	}

	private static string EscapeTable(string text)
	{
		var result = Escape(text).Replace("|", "\\|");

		return result;
	}

    private sealed record HealthFinding(string Severity, string Category, string Message, string Context);
	private sealed record HealthMatcherRule(ArchitectureDocumentationItem Item, string ParentKind, string ParentLabel, PatternMatcher Matcher, bool IsException);
}

internal sealed record ArchitectureHealthReport(string Markdown, int FindingCount);
