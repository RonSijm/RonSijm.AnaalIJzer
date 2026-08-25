using System.Text;
using RonSijm.AnaalIJzer.Model;

namespace RonSijm.AnaalIJzer.Documentation;

internal static partial class ArchitectureDocumentationMarkdownBuilder
{
	private static void AppendInheritancePolicies(StringBuilder sb, AnalyzerConfig config)
	{
		var policies = config.Documentation.Items.Where(item => item.Kind == "InheritancePolicy").ToArray();
		if (policies.Length == 0)
		{
			return;
		}

		sb.AppendLine("## Inheritance Policies");
		sb.AppendLine();
		sb.AppendLine("| Scope | Type kinds | Required base types | Required interfaces | Description |");
		sb.AppendLine("|-------|------------|---------------------|---------------------|-------------|");
		foreach (var policy in policies)
		{
			var typeKinds = policy.GetAttribute("typeKinds") ?? string.Empty;
			var requiredBaseTypes = policy.GetAttribute("requiredBaseTypes") ?? "None";
			var requiredInterfaces = policy.GetAttribute("requiredInterfaces") ?? "None";
			sb.AppendLine($"| `{EscapeTable(policy.LayerPath)}` | {EscapeTable(typeKinds)} | {EscapeTable(requiredBaseTypes)} | {EscapeTable(requiredInterfaces)} | {EscapeTable(policy.Description ?? string.Empty)} |");
		}

		sb.AppendLine();
	}
}
