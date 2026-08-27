using System.Text;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ArchitectureCodeEvidenceGenerator
{
	private static IReadOnlyList<TypeRuleMatch> GetMatches(IReadOnlyList<INamedTypeSymbol> types, AnalyzerConfiguration config)
	{
		var matches = new List<TypeRuleMatch>();
		foreach (var type in types)
		{
			var namespaceName = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();
			var match = config.Engine.FindLayer(type.Name, namespaceName, type);
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

	private sealed record TypeRuleMatch(INamedTypeSymbol Type, string XmlPath, int XmlLineNumber);
}

