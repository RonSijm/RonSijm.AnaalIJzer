using System.Text;
using RonSijm.AnaalIJzer.Model;

namespace RonSijm.AnaalIJzer.Documentation;

internal static partial class ArchitectureDocumentationMarkdownBuilder
{
	private static void AppendApiSurfacePolicies(StringBuilder sb, AnalyzerConfig config)
	{
		if (!config.Documentation.Items.Any(item => item.Kind == "ApiSurface"))
		{
			return;
		}

		sb.AppendLine("## API Surface Policies");
		sb.AppendLine();
		sb.AppendLine("| Scope | Recognition | Transitive depth | Rule | Layer | Sites | Description |");
		sb.AppendLine("|-------|-------------|------------------|------|-------|-------|-------------|");
		for (var policyIndex = 0; policyIndex < config.Documentation.Items.Length; policyIndex++)
		{
			var policy = config.Documentation.Items[policyIndex];
			if (policy.Kind != "ApiSurface")
			{
				continue;
			}

			var recognition = string.Equals(policy.GetAttribute("requireRecognizedTypes"), "true", StringComparison.OrdinalIgnoreCase)
			                  || policy.GetAttribute("requireRecognizedTypes") == "1"
				? "Required"
				: "Optional";
			var transitive = config.Documentation.Items
				.Skip(policyIndex + 1)
				.TakeWhile(item => item.Depth > policy.Depth)
				.FirstOrDefault(item => item.Depth == policy.Depth + 1 && item.Kind == "TransitiveExposure");
			var transitiveDepth = transitive.Kind == "TransitiveExposure"
				? transitive.GetAttribute("maxDepth") ?? "3"
				: "Disabled";
			foreach (var rule in config.Documentation.Items.Skip(policyIndex + 1).TakeWhile(item => item.Depth > policy.Depth).Where(item => item.Depth == policy.Depth + 1 && item.Kind is "AllowedLayer" or "BlockedLayer"))
			{
				var mode = rule.Kind == "AllowedLayer" ? "Allow" : "Block";
				var sites = rule.GetAttribute("allowedSites") is { } allowedSites
					? "Only " + allowedSites
					: rule.GetAttribute("blockedSites") is { } blockedSites
						? "Except " + blockedSites
						: "All";
				sb.AppendLine($"| `{EscapeTable(policy.LayerPath)}` | {recognition} | {EscapeTable(transitiveDepth)} | {mode} | `{EscapeTable(rule.GetAttribute("path") ?? string.Empty)}` | {EscapeTable(sites)} | {EscapeTable(rule.Description ?? transitive.Description ?? policy.Description ?? string.Empty)} |");
			}
		}

		sb.AppendLine();
	}

	private static void AppendSourceLocationPolicies(StringBuilder sb, AnalyzerConfig config)
	{
		var policies = config.Documentation.Items.Where(item => item.Kind == "SourceLocations").ToArray();
		if (policies.Length == 0)
		{
			return;
		}

		sb.AppendLine("## Source Locations");
		sb.AppendLine();
		sb.AppendLine("| Scope | Relative to | Matcher | Assembly | Description |");
		sb.AppendLine("|-------|-------------|---------|----------|-------------|");

		for (var policyIndex = 0; policyIndex < config.Documentation.Items.Length; policyIndex++)
		{
			var policy = config.Documentation.Items[policyIndex];
			if (policy.Kind != "SourceLocations")
			{
				continue;
			}

			var relativeTo = policy.GetAttribute("relativeTo") ?? "Project";
			foreach (var rule in config.Documentation.Items.Skip(policyIndex + 1).TakeWhile(item => item.Depth > policy.Depth).Where(item => item.Depth == policy.Depth + 1 && item.Kind == "Source"))
			{
				sb.AppendLine($"| `{EscapeTable(policy.LayerPath)}` | {EscapeTable(relativeTo)} | `{EscapeTable(rule.Label)}` | {EscapeTable(rule.GetAttribute("assemblyName") ?? string.Empty)} | {EscapeTable(rule.Description ?? policy.Description ?? string.Empty)} |");
			}
		}

		sb.AppendLine();
	}

	private static void AppendBoundaryEntryPointPolicies(StringBuilder sb, AnalyzerConfig config)
	{
		var items = config.Documentation.Items;
		var policies = items.Where(item => item.Kind == "EntryPoints").ToArray();
		if (policies.Length == 0)
		{
			return;
		}

		sb.AppendLine("## Boundary Entry Points");
		sb.AppendLine();
		sb.AppendLine("| Boundary | Selector | Sites | Description |");
		sb.AppendLine("|----------|----------|-------|-------------|");

		for (var policyIndex = 0; policyIndex < items.Length; policyIndex++)
		{
			var policy = items[policyIndex];
			if (policy.Kind != "EntryPoints")
			{
				continue;
			}

			for (var entryPointIndex = policyIndex + 1; entryPointIndex < items.Length; entryPointIndex++)
			{
				var entryPoint = items[entryPointIndex];
				if (entryPoint.Depth <= policy.Depth)
				{
					break;
				}

				if (entryPoint.Depth != policy.Depth + 1 || entryPoint.Kind != "EntryPoint")
				{
					continue;
				}

				var selector = entryPoint.GetAttribute("layer");
				if (string.IsNullOrWhiteSpace(selector))
				{
					var selectors = new List<string>();
					for (var matcherIndex = entryPointIndex + 1; matcherIndex < items.Length; matcherIndex++)
					{
						var matcher = items[matcherIndex];
						if (matcher.Depth <= entryPoint.Depth)
						{
							break;
						}

						if (matcher.Depth == entryPoint.Depth + 1 && matcher.Kind is "Class" or "Namespace" or "Assembly")
						{
							selectors.Add(matcher.Label);
						}
					}

					selector = string.Join(" or ", selectors);
				}

				var sites = entryPoint.GetAttribute("allowedSites") is { } allowedSites
					? "Only " + allowedSites
					: entryPoint.GetAttribute("blockedSites") is { } blockedSites
						? "Except " + blockedSites
						: "All";
				var description = entryPoint.Description ?? policy.Description ?? string.Empty;
				sb.AppendLine($"| `{EscapeTable(policy.LayerPath)}` | `{EscapeTable(selector ?? string.Empty)}` | {EscapeTable(sites)} | {EscapeTable(description)} |");
			}
		}

		sb.AppendLine();
	}
}
