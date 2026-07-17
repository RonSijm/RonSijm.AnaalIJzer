using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Model;
using RonSijm.AnaalIJzer.Diagnostics;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Tooling;

internal static class ArchitectureCodeEvidenceGenerator
{
	public static string Append(string documentation, Compilation compilation, AnalyzerConfiguration config, ImmutableArray<Diagnostic> diagnostics, string projectDirectory, CancellationToken cancellationToken)
	{
		var types = ApplicationConfigurationGenerator.GetProjectTypes(compilation, cancellationToken);
		var matches = GetMatches(types, config);
		string? ResolveLayer(INamedTypeSymbol type)
		{
			var namespaceName = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();
			return config.FindLayer(type.Name, namespaceName, type)?.Layer.Name;
		}
		var dependencies = ProjectDependencyScanner.Scan(compilation, ResolveLayer, cancellationToken);
		var sb = new StringBuilder(documentation.TrimEnd());
		sb.AppendLine();
		sb.AppendLine();
		sb.AppendLine("## Code Evidence");
		sb.AppendLine();
		sb.AppendLine("This optional section evaluates the configured rules against the current project compilation.");
		sb.AppendLine();
		AppendRuleMatches(sb, config, matches);
		AppendAllowedDependencyUsages(sb, config, dependencies, projectDirectory);
		AppendUnclassifiedTypes(sb, types, matches);
		AppendViolations(sb, diagnostics, projectDirectory);
		return sb.ToString();
	}

	private static IReadOnlyList<TypeRuleMatch> GetMatches(IReadOnlyList<INamedTypeSymbol> types, AnalyzerConfiguration config)
	{
		var matches = new List<TypeRuleMatch>();
		foreach (var type in types)
		{
			var namespaceName = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();
			var match = config.FindLayer(type.Name, namespaceName, type);
			if (match is not { } layerMatch)
			{
				continue;
			}

			foreach (var matcherMatch in layerMatch.MatcherMatches)
			{
				matches.Add(new TypeRuleMatch(type, NormalizePath(matcherMatch.XmlPath), matcherMatch.XmlLineNumber));
			}
		}
		return matches;
	}

	private static void AppendRuleMatches(StringBuilder sb, AnalyzerConfiguration config, IReadOnlyList<TypeRuleMatch> matches)
	{
		sb.AppendLine("### Effective Matcher Rule Matches");
		sb.AppendLine();
		var ancestors = new List<ArchitectureDocumentationItem>();
		var matcherCount = 0;
		foreach (var item in config.Documentation.Items)
		{
			while (ancestors.Count > item.Depth)
			{
				ancestors.RemoveAt(ancestors.Count - 1);
			}

			var parent = item.Depth > 0 && ancestors.Count >= item.Depth ? ancestors[item.Depth - 1] : default;
			if (item.Kind is "Class" or "Namespace" or "Assembly" && parent.Kind is "Layer" or "Forbidden")
			{
				matcherCount++;
				var ruleMatches = matches
					.Where(match => match.XmlLineNumber == item.XmlLineNumber && string.Equals(match.XmlPath, NormalizePath(item.SourcePath), StringComparison.OrdinalIgnoreCase))
					.Select(match => GetTypeName(match.Type))
					.OrderBy(name => name, StringComparer.Ordinal)
					.ToArray();
				sb.AppendLine($"#### {Escape(parent.Kind)} `{Escape(parent.Label)}` / {Escape(item.Kind)} `{Escape(item.Label)}`");
				sb.AppendLine();
				sb.AppendLine(ruleMatches.Length == 1 ? "1 project type currently resolves through this rule." : $"{ruleMatches.Length} project types currently resolve through this rule.");
				sb.AppendLine();
				foreach (var typeName in ruleMatches)
				{
					sb.AppendLine($"- `{Escape(typeName)}`");
				}
				if (ruleMatches.Length == 0)
				{
					sb.AppendLine("- No current project types.");
				}
				sb.AppendLine();
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

		if (matcherCount == 0)
		{
			sb.AppendLine("No class or namespace matcher rules are configured.");
			sb.AppendLine();
		}
	}

	private static void AppendAllowedDependencyUsages(StringBuilder sb, AnalyzerConfiguration config, IReadOnlyList<ProjectDependencyObservation> dependencies, string projectDirectory)
	{
		sb.AppendLine("### Dependency Rule Usages");
		sb.AppendLine();
		if (config.Graph.DependencyEdges.Length == 0)
		{
			sb.AppendLine("No allowed dependency rules are configured.");
			sb.AppendLine();
			return;
		}

		foreach (var edge in config.Graph.DependencyEdges)
		{
			var usages = dependencies
				.Where(dependency => config.Graph.Matches(edge, dependency.CallerLayer, dependency.DependencyLayer) && edge.AllowsSite(dependency.Site))
				.OrderBy(dependency => dependency.CallerType.Name, StringComparer.Ordinal)
				.ThenBy(dependency => dependency.DependencyType.Name, StringComparer.Ordinal)
				.ThenBy(dependency => dependency.Site, StringComparer.Ordinal)
				.ThenBy(dependency => dependency.Location.SourceSpan.Start)
				.ToArray();
			sb.AppendLine($"#### {(edge.IsBlocked ? "BlockedDependency" : "AllowedDependency")} `{Escape(edge.From)} -> {Escape(edge.To)}`");
			sb.AppendLine();
			var action = edge.IsBlocked ? "blocked" : "permitted";
			sb.AppendLine(usages.Length == 1 ? $"1 current code usage is {action} by this rule." : $"{usages.Length} current code usages are {action} by this rule.");
			sb.AppendLine();
			foreach (var usage in usages)
			{
				sb.AppendLine($"- `{Escape(GetTypeName(usage.CallerType))}` -> `{Escape(GetTypeName(usage.DependencyType))}` at `{Escape(usage.Site)}` ({Escape(FormatLocation(usage.Location, projectDirectory))})");
			}
			if (usages.Length == 0)
			{
				sb.AppendLine("- No current code usages.");
			}
			sb.AppendLine();
		}
	}

	private static void AppendUnclassifiedTypes(StringBuilder sb, IReadOnlyList<INamedTypeSymbol> types, IReadOnlyList<TypeRuleMatch> matches)
	{
		var matchedTypes = matches.Select(match => match.Type).ToHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
		var unclassifiedTypes = types.Where(type => !matchedTypes.Contains(type)).Select(GetTypeName).OrderBy(name => name, StringComparer.Ordinal).ToArray();
		sb.AppendLine("### Unclassified Project Types");
		sb.AppendLine();
		if (unclassifiedTypes.Length == 0)
		{
			sb.AppendLine("Every source-defined project type resolves through a configured matcher.");
			sb.AppendLine();
			return;
		}

		sb.AppendLine("These source-defined types do not currently resolve to a configured layer. Generated convention exceptions also appear here because they intentionally exempt the caller from its layer matcher.");
		sb.AppendLine();
		foreach (var typeName in unclassifiedTypes)
		{
			sb.AppendLine($"- `{Escape(typeName)}`");
		}
		sb.AppendLine();
	}

	private static void AppendViolations(StringBuilder sb, ImmutableArray<Diagnostic> diagnostics, string projectDirectory)
	{
		sb.AppendLine("### Current Rule Violations");
		sb.AppendLine();
		if (diagnostics.Length == 0)
		{
			sb.AppendLine("The analyzer reports no violations for this compilation and configuration.");
			sb.AppendLine();
			return;
		}

		sb.AppendLine("| Diagnostic | Site | Caller | Dependency | Location | Message |");
		sb.AppendLine("|------------|------|--------|------------|----------|---------|");
		foreach (var diagnostic in diagnostics.OrderBy(diagnostic => diagnostic.Location.SourceTree?.FilePath, StringComparer.OrdinalIgnoreCase).ThenBy(diagnostic => diagnostic.Location.SourceSpan.Start))
		{
			var properties = diagnostic.Properties;
			var site = GetProperty(properties, ArchitecturalDiagnostics.PropertySite);
			var caller = GetProperty(properties, ArchitecturalDiagnostics.PropertyCallerTypeName);
			var dependency = GetProperty(properties, ArchitecturalDiagnostics.PropertyDepTypeName);
			var location = FormatLocation(diagnostic.Location, projectDirectory);
			sb.AppendLine($"| `{EscapeTable(diagnostic.Id)}` | {EscapeTable(site)} | `{EscapeTable(caller)}` | `{EscapeTable(dependency)}` | `{EscapeTable(location)}` | {EscapeTable(diagnostic.GetMessage())} |");
		}
		sb.AppendLine();
	}

	private static string GetProperty(ImmutableDictionary<string, string?> properties, string name)
	{
		var result = properties.TryGetValue(name, out var value) ? value ?? string.Empty : string.Empty;

		return result;
	}

    private static string FormatLocation(Location location, string projectDirectory)
	{
		if (!location.IsInSource)
		{
			return string.Empty;
		}

		var span = location.GetLineSpan();
		var path = span.Path;
		if (!string.IsNullOrWhiteSpace(path))
		{
			try
			{
				path = Path.GetRelativePath(projectDirectory, path);
			}
			catch
			{
			}
		}
		return $"{path}:{span.StartLinePosition.Line + 1}";
	}

	private static string GetTypeName(INamedTypeSymbol type)
	{
		var result = type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

		return result;
	}

    private static string NormalizePath(string path)
	{
		try
		{
			return Path.GetFullPath(path);
		}
		catch
		{
			return path;
		}
	}

	private static string Escape(string text)
	{
		var result = text.Replace("\r", " ").Replace("\n", " ").Replace("`", "\\`");

		return result;
	}

	private static string EscapeTable(string text)
	{
		var result = text.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ").Replace("`", "\\`");

		return result;
	}

    private sealed record TypeRuleMatch(INamedTypeSymbol Type, string XmlPath, int XmlLineNumber);
}
