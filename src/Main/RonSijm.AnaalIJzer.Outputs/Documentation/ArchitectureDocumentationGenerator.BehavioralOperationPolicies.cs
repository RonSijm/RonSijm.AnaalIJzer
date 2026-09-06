using System.Collections.Immutable;
using System.Text;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model;

namespace RonSijm.AnaalIJzer.Outputs.Documentation;

internal static partial class ArchitectureDocumentationMarkdownBuilder
{
	private static void AppendBehavioralOperationPolicies(StringBuilder sb, AnalyzerConfig config)
	{
		var policies = config.Documentation.Items.Where(item => item.Kind == "BehavioralOperations").ToArray();
		if (policies.Length == 0)
		{
			return;
		}

		sb.AppendLine("## Behavioral Operation Policies");
		sb.AppendLine();
		sb.AppendLine("These policies prove limited operation presence, ordering, or count facts for explicitly selected declaration bodies. They are policies, not dependency edges.");
		sb.AppendLine();
		sb.AppendLine("| Scope | Rule | Declaration | Selected operation | Related operation | Proof | Sites | Description |");
		sb.AppendLine("|-------|------|-------------|--------------------|-------------------|-------|-------|-------------|");
		for (var policyIndex = 0; policyIndex < config.Documentation.Items.Length; policyIndex++)
		{
			var policy = config.Documentation.Items[policyIndex];
			if (policy.Kind != "BehavioralOperations")
			{
				continue;
			}

			foreach (var ruleIndex in GetDirectChildIndices(config.Documentation.Items, policyIndex).Where(index => IsBehavioralOperationRule(config.Documentation.Items[index].Kind)))
			{
				var rule = config.Documentation.Items[ruleIndex];
				var children = GetDirectChildIndices(config.Documentation.Items, ruleIndex).ToArray();
				var declaration = FormatBehavioralDeclaration(config.Documentation.Items, children.SingleOrDefault(index => config.Documentation.Items[index].Kind == "DeclarationMatcher"));
				var selectedOperations = FormatBehavioralOperationMatchers(config.Documentation.Items, children.Where(index => config.Documentation.Items[index].Kind == "OperationMatcher"));
				var relatedContainer = children
					.Select(index => config.Documentation.Items[index])
					.FirstOrDefault(item => item.Kind is "BeforeOperation" or "AfterOperation");
				var relatedOperations = string.IsNullOrWhiteSpace(relatedContainer.Kind)
					? string.Empty
					: FormatBehavioralOperationMatchers(config.Documentation.Items, FindDirectChildIndices(config.Documentation.Items, relatedContainer));
				var proof = FormatBehavioralProof(rule);
				var sites = FormatOperationSites(rule);
				var description = rule.Description ?? policy.Description ?? string.Empty;
				sb.AppendLine($"| `{EscapeTable(policy.LayerPath)}` | {EscapeTable(FormatBehavioralRule(rule))} | {EscapeTable(declaration)} | {EscapeTable(selectedOperations)} | {EscapeTable(relatedOperations)} | {EscapeTable(proof)} | {EscapeTable(sites)} | {EscapeTable(description)} |");
			}
		}

		sb.AppendLine();
	}

	private static bool IsBehavioralOperationRule(string kind)
	{
		var result = kind is "RequiredOperation" or "RequiredOperationBefore" or "ForbiddenOperationAfter" or "MaximumOperationCount";

		return result;
	}

	private static string FormatBehavioralRule(ArchitectureDocumentationItem rule)
	{
		var result = rule.Kind switch
		{
			"RequiredOperation" => "Require operation",
			"RequiredOperationBefore" => "Require operation before target",
			"ForbiddenOperationAfter" => "Forbid operation after terminal",
			"MaximumOperationCount" => "Maximum " + (rule.GetAttribute("maximum") ?? "?") + " operation(s)",
			_ => rule.Kind
		};

		return result;
	}

	private static string FormatBehavioralProof(ArchitectureDocumentationItem rule)
	{
		var result = rule.Kind == "MaximumOperationCount"
			? "Lexical count"
			: rule.GetAttribute("ordering") ?? "Dominance";

		return result;
	}

	private static string FormatBehavioralDeclaration(ImmutableArray<ArchitectureDocumentationItem> items, int declarationIndex)
	{
		if (declarationIndex < 0)
		{
			return "(no declaration selector)";
		}

		var selectors = FindDirectChildIndices(items, items[declarationIndex])
			.Select(index => items[index])
			.Where(item => item.Kind is "ContainingType" or "Member")
			.Select(item => item.Kind + " " + FormatOperationMatcherAttributes(item))
			.ToArray();
		var result = selectors.Length == 0 ? "(no declaration selector)" : string.Join("; ", selectors);

		return result;
	}

	private static string FormatBehavioralOperationMatchers(ImmutableArray<ArchitectureDocumentationItem> items, IEnumerable<int> matcherIndices)
	{
		var matchers = matcherIndices
			.Select(index => FormatOperationMatcher(items[index], FindDirectChildIndices(items, items[index]).Select(childIndex => items[childIndex])))
			.ToArray();
		var result = matchers.Length == 0 ? "(no operation matcher)" : string.Join(" OR ", matchers);

		return result;
	}

	private static IEnumerable<int> FindDirectChildIndices(ImmutableArray<ArchitectureDocumentationItem> items, ArchitectureDocumentationItem parent)
	{
		var parentIndex = items.IndexOf(parent);
		var result = parentIndex < 0 ? Enumerable.Empty<int>() : GetDirectChildIndices(items, parentIndex);

		return result;
	}
}
