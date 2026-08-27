using System.Text;
using RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model;

namespace RonSijm.AnaalIJzer.Outputs.Documentation;

internal static partial class ArchitectureDocumentationMarkdownBuilder
{
	private static void AppendContractPolicies(StringBuilder sb, AnalyzerConfig config)
	{
		var policies = config.Documentation.Items.Where(item => item.Kind == "ContractPolicy").ToArray();
		if (policies.Length == 0)
		{
			return;
		}

		sb.AppendLine("## Contract Policies");
		sb.AppendLine();
		sb.AppendLine("| Scope | Allowed type kinds | Allowed member kinds | Property accessors | Method bodies | Static members | Nested types | Description |");
		sb.AppendLine("|-------|--------------------|----------------------|--------------------|---------------|----------------|--------------|-------------|");
		foreach (var policy in policies)
		{
			var allowedTypeKinds = policy.GetAttribute("allowedTypeKinds") ?? string.Empty;
			var allowedMemberKinds = policy.GetAttribute("allowedMemberKinds") ?? string.Empty;
			var propertyAccessors = policy.GetAttribute("allowedPropertyAccessors") ?? "Any";
			var allowMethodBodies = policy.GetAttribute("allowMethodBodies") ?? "false";
			var allowStaticMembers = policy.GetAttribute("allowStaticMembers") ?? "false";
			var allowNestedTypes = policy.GetAttribute("allowNestedTypes") ?? "false";
			sb.AppendLine($"| `{EscapeTable(policy.LayerPath)}` | {EscapeTable(allowedTypeKinds)} | {EscapeTable(allowedMemberKinds)} | {EscapeTable(propertyAccessors)} | {EscapeTable(allowMethodBodies)} | {EscapeTable(allowStaticMembers)} | {EscapeTable(allowNestedTypes)} | {EscapeTable(policy.Description ?? string.Empty)} |");
		}

		sb.AppendLine();
	}

	private static void AppendProjectArchitecture(StringBuilder sb, AnalyzerConfig config)
	{
		if (!config.Documentation.Items.Any(item => item.Kind == "ProjectArchitecture"))
		{
			return;
		}

		sb.AppendLine("## Project Architecture");
		sb.AppendLine();

		var projectArchitecture = config.Documentation.Items.First(item => item.Kind == "ProjectArchitecture");
		var requireRecognizedProjects = projectArchitecture.GetAttribute("requireRecognizedProjects") ?? "false";
		sb.AppendLine($"`requireRecognizedProjects`: `{EscapeTable(requireRecognizedProjects)}`");
		if (!string.IsNullOrWhiteSpace(projectArchitecture.Description))
		{
			sb.AppendLine();
			sb.AppendLine(EscapeMarkdown(projectArchitecture.Description!));
		}

		sb.AppendLine();
		var groups = config.Documentation.Items.Where(item => item.Kind == "ProjectGroup").ToArray();
		if (groups.Length > 0)
		{
			sb.AppendLine("### Project Groups");
			sb.AppendLine();
			sb.AppendLine("| Group | Matchers | Description |");
			sb.AppendLine("|-------|----------|-------------|");
			for (var itemIndex = 0; itemIndex < config.Documentation.Items.Length; itemIndex++)
			{
				var group = config.Documentation.Items[itemIndex];
				if (group.Kind != "ProjectGroup")
				{
					continue;
				}

				var matchers = config.Documentation.Items
					.Skip(itemIndex + 1)
					.TakeWhile(item => item.Depth > group.Depth)
					.Where(item => item.Depth == group.Depth + 1 && item.Kind == "Project")
					.Select(item => item.Label)
					.ToArray();
				sb.AppendLine($"| `{EscapeTable(group.Label)}` | {EscapeTable(string.Join(", ", matchers))} | {EscapeTable(group.Description ?? string.Empty)} |");
			}

			sb.AppendLine();
		}

		var rules = config.Documentation.Items.Where(item => item.Kind is "AllowedProjectReference" or "BlockedProjectReference").ToArray();
		if (rules.Length > 0)
		{
			sb.AppendLine("### Project Reference Rules");
			sb.AppendLine();
			sb.AppendLine("| Rule | Edge | Description |");
			sb.AppendLine("|------|------|-------------|");
			foreach (var rule in rules)
			{
				var mode = rule.Kind == "AllowedProjectReference" ? "Allowed" : "Blocked";
				sb.AppendLine($"| {mode} | `{EscapeTable(rule.Label)}` | {EscapeTable(rule.Description ?? string.Empty)} |");
			}

			sb.AppendLine();
		}

		var packagePolicies = config.Documentation.Items.Where(item => item.Kind == "PackagePolicy").ToArray();
		if (packagePolicies.Length > 0)
		{
			sb.AppendLine("### Package Policies");
			sb.AppendLine();
			sb.AppendLine("| Project group | Include transitive | Mode | Matcher | Description |");
			sb.AppendLine("|---------------|--------------------|------|---------|-------------|");

			for (var itemIndex = 0; itemIndex < config.Documentation.Items.Length; itemIndex++)
			{
				var policy = config.Documentation.Items[itemIndex];
				if (policy.Kind != "PackagePolicy")
				{
					continue;
				}

				var projectGroup = policy.GetAttribute("projectGroup") ?? string.Empty;
				var includeTransitive = policy.GetAttribute("includeTransitive") ?? "false";
				for (var containerIndex = itemIndex + 1; containerIndex < config.Documentation.Items.Length; containerIndex++)
				{
					var container = config.Documentation.Items[containerIndex];
					if (container.Depth <= policy.Depth)
					{
						break;
					}

					if (container.Depth != policy.Depth + 1 || container.Kind is not ("Allowed" or "Forbidden"))
					{
						continue;
					}

					var mode = container.Kind;
					for (var matcherIndex = containerIndex + 1; matcherIndex < config.Documentation.Items.Length; matcherIndex++)
					{
						var matcher = config.Documentation.Items[matcherIndex];
						if (matcher.Depth <= container.Depth)
						{
							break;
						}

						if (matcher.Depth != container.Depth + 1 || matcher.Kind != "Package")
						{
							continue;
						}

						sb.AppendLine($"| `{EscapeTable(projectGroup)}` | {EscapeTable(includeTransitive)} | {EscapeTable(mode)} | `{EscapeTable(matcher.Label)}` | {EscapeTable(matcher.Comment ?? matcher.Description ?? container.Description ?? policy.Description ?? string.Empty)} |");
					}
				}
			}

			sb.AppendLine();
		}
	}
}
