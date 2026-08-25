using System.Collections.Immutable;
using System.Text;
using RonSijm.AnaalIJzer.Exceptions;
using RonSijm.AnaalIJzer.Model;

namespace RonSijm.AnaalIJzer.Documentation;

internal static partial class ArchitectureDocumentationMarkdownBuilder
{
	private static void AppendRootSettings(StringBuilder sb, AnalyzerConfig config)
	{
		var rows = new List<(string Name, string Value, string Description)>();

		if (config.RequiredRecognizedDependencySites.Count > 0)
		{
			rows.Add(("requireRecognizedDependencies", string.Join(", ", config.RequiredRecognizedDependencySites), "Dependency sites that must resolve to configured layers."));
		}

		if (config.EnforceAcyclic)
		{
			rows.Add(("enforceAcyclic", "true", "Configured AllowedDependency edges may not form a cycle."));
		}

		if (config.EnforceObservedAcyclic)
		{
			rows.Add(("enforceObservedAcyclic", "true", "Observed source dependencies may not form a cycle."));
		}

		if (config.Output.EnableDocumentation)
		{
			rows.Add(("enableDocumentation", "true", "Arse may generate documentation output for this configuration."));
		}

		if (!string.IsNullOrWhiteSpace(config.Output.DocumentationPath))
		{
			rows.Add(("documentationPath", config.Output.DocumentationPath!, "Default relative output path for generated documentation."));
		}

		if (config.Output.EnableReport)
		{
			rows.Add(("enableReport", "true", "Arse may generate a violation report for this configuration."));
		}

		if (!string.IsNullOrWhiteSpace(config.Output.ReportPath))
		{
			rows.Add(("reportPath", config.Output.ReportPath!, "Default relative output path for generated violation reports."));
		}

		if (rows.Count == 0)
		{
			return;
		}

		sb.AppendLine("## Root Settings");
		sb.AppendLine();
		sb.AppendLine("| Setting | Value | Meaning |");
		sb.AppendLine("|---------|-------|---------|");

		foreach (var row in rows)
		{
			sb.AppendLine($"| `{EscapeTable(row.Name)}` | `{EscapeTable(row.Value)}` | {EscapeTable(row.Description)} |");
		}

		sb.AppendLine();
	}

	private static void AppendExceptionPolicy(StringBuilder sb, AnalyzerConfig config)
	{
		if (!config.ExceptionPolicy.IsEnabled && config.ExceptionDefinitions.Length == 0 && config.ExceptionReviews.Length == 0)
		{
			return;
		}

		sb.AppendLine("## Exception Policy");
		sb.AppendLine();
		sb.AppendLine("| Setting | Value | Meaning |");
		sb.AppendLine("|---------|-------|---------|");
		sb.AppendLine($"| `enabled` | `{(config.ExceptionPolicy.IsEnabled ? "true" : "false")}` | Temporary exception governance is {(config.ExceptionPolicy.IsEnabled ? "enabled" : "disabled")}. |");
		if (config.ExceptionPolicy.IsEnabled)
		{
			sb.AppendLine($"| `requireReason` | `{config.ExceptionPolicy.RequireReason.ToString().ToLowerInvariant()}` | Each exception matcher must declare a human-readable reason. |");
			sb.AppendLine($"| `requireOwner` | `{config.ExceptionPolicy.RequireOwner.ToString().ToLowerInvariant()}` | Each exception matcher must declare an owner responsible for removal. |");
			sb.AppendLine($"| `requireExpiresOn` | `{config.ExceptionPolicy.RequireExpiresOn.ToString().ToLowerInvariant()}` | Each exception matcher must declare an ISO expiry date. |");
			sb.AppendLine($"| `warnBeforeDays` | `{config.ExceptionPolicy.WarnBeforeDays}` | Expiring exceptions warn this many days before expiry. |");
		}

		if (!string.IsNullOrWhiteSpace(config.ExceptionPolicy.Description))
		{
			sb.AppendLine($"| `description` | `{EscapeTable(config.ExceptionPolicy.Description!)}` | Human-authored policy description. |");
		}

		sb.AppendLine();

		var definitions = FlattenExceptionDefinitions(config.ExceptionDefinitions);
		if (definitions.Length > 0)
		{
			sb.AppendLine("### Exception Matchers");
			sb.AppendLine();
			sb.AppendLine("| Matcher | Reason | Owner | Expires | Status | Source |");
			sb.AppendLine("|---------|--------|-------|---------|--------|--------|");
			foreach (var definition in definitions)
			{
				var source = definition.XmlLineNumber > 0
					? definition.XmlPath + ":" + definition.XmlLineNumber
					: definition.XmlPath;
				sb.AppendLine(
					$"| `{EscapeTable(definition.MatcherKind + " " + definition.MatcherLabel)}` | {EscapeTable(definition.Metadata.Reason ?? string.Empty)} | {EscapeTable(definition.Metadata.Owner ?? string.Empty)} | {EscapeTable(definition.Metadata.ExpiresOnText ?? string.Empty)} | {EscapeTable(definition.Status.ToString())} | `{EscapeTable(source)}` |");
			}

			sb.AppendLine();
		}

		if (config.ExceptionReviews.Length > 0)
		{
			sb.AppendLine("### Exception Reviews");
			sb.AppendLine();
			sb.AppendLine("| Status | Matcher | Message |");
			sb.AppendLine("|--------|---------|---------|");
			foreach (var review in config.ExceptionReviews.OrderBy(review => review.Status).ThenBy(review => review.MatcherLabel, StringComparer.Ordinal))
			{
				sb.AppendLine($"| {EscapeTable(review.Status.ToString())} | `{EscapeTable(review.MatcherKind + " " + review.MatcherLabel)}` | {EscapeTable(review.Message)} |");
			}

			sb.AppendLine();
		}
	}

	private static ImmutableArray<ArchitectureExceptionDefinition> FlattenExceptionDefinitions(ImmutableArray<ArchitectureExceptionDefinition> definitions)
	{
		var builder = ImmutableArray.CreateBuilder<ArchitectureExceptionDefinition>();
		foreach (var definition in definitions)
		{
			FlattenExceptionDefinitions(builder, definition);
		}

		var result = builder.ToImmutable();

		return result;
	}

	private static void FlattenExceptionDefinitions(ImmutableArray<ArchitectureExceptionDefinition>.Builder builder, ArchitectureExceptionDefinition definition)
	{
		builder.Add(definition);
		foreach (var nested in definition.Nested)
		{
			FlattenExceptionDefinitions(builder, nested);
		}
	}
}
