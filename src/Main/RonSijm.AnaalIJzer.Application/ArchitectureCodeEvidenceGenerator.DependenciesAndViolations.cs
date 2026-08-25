using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ArchitectureCodeEvidenceGenerator
{
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
			var site = GetProperty(properties, ArchitectureDiagnosticProperties.PropertySite);
			if (site.Length == 0)
			{
				site = GetProperty(properties, ArchitectureDiagnosticProperties.PropertyDeclarationTarget);
			}

			var caller = GetProperty(properties, ArchitectureDiagnosticProperties.PropertyCallerTypeName);
			var dependency = GetProperty(properties, ArchitectureDiagnosticProperties.PropertyDepTypeName);
			if (dependency.Length == 0)
			{
				dependency = GetProperty(properties, ArchitectureDiagnosticProperties.PropertyDeclaredSymbolName);
			}

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
}

