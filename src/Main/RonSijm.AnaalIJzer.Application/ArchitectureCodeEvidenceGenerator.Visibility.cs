using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.Engine.LayerModel;
using RonSijm.AnaalIJzer.Engine.Visibility;
using RonSijm.AnaalIJzer.SymbolFacts;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ArchitectureCodeEvidenceGenerator
{
	private static void AppendVisibilityPolicyEvidence(StringBuilder sb, AnalyzerConfiguration config, IReadOnlyList<INamedTypeSymbol> types, string projectDirectory)
	{
		var policies = EnumerateVisibilityPolicies(config.Layers).ToArray();
		if (policies.Length == 0)
		{
			return;
		}

		var declarations = EnumerateSourceDeclarations(types).ToArray();
		sb.AppendLine("### Visibility Policy Declarations");
		sb.AppendLine();
		foreach (var policy in policies)
		{
			var evidence = declarations
				.Select(symbol => TryCreateVisibilityEvidence(symbol, policy, config, projectDirectory))
				.OfType<VisibilityDeclarationEvidence>()
				.OrderBy(item => item.DeclarationName, StringComparer.Ordinal)
				.ThenBy(item => item.Target, StringComparer.Ordinal)
				.ToArray();
			var mode = policy.IsAllowList ? "allows only" : "blocks";
			var configured = string.Join(", ", policy.Accessibilities.OrderBy(item => item).Select(item => item.ToDisplayText()));
			sb.AppendLine($"#### Layer `{Escape(policy.OwnerLayerPath)}` / {string.Join(", ", policy.Targets.OrderBy(item => item))}");
			sb.AppendLine();
			sb.AppendLine($"This policy {mode} `{Escape(configured)}`.");
			sb.AppendLine();
			foreach (var item in evidence)
			{
				var status = item.IsViolation ? "violates" : "passes";
				var effective = item.IsEffectivelyExternallyVisible ? "externally visible" : "not effectively external";
				sb.AppendLine($"- **{status}** `{Escape(item.DeclarationName)}`: {Escape(item.Target)}, {Escape(item.Accessibility)}, {effective} ({Escape(item.Location)})");
			}

			if (evidence.Length == 0)
			{
				sb.AppendLine("- No current source declarations match this policy.");
			}

			sb.AppendLine();
		}
	}

	private static IEnumerable<VisibilityPolicy> EnumerateVisibilityPolicies(ImmutableArray<LayerNode> layers)
	{
		foreach (var layer in layers)
		{
			foreach (var policy in layer.VisibilityPolicies)
			{
				yield return policy;
			}

			foreach (var childPolicy in EnumerateVisibilityPolicies(layer.Children))
			{
				yield return childPolicy;
			}
		}
	}

	private static IEnumerable<ISymbol> EnumerateSourceDeclarations(IReadOnlyList<INamedTypeSymbol> types)
	{
		var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
		foreach (var type in types)
		{
			if (seen.Add(type))
			{
				yield return type;
			}

			foreach (var member in type.GetMembers())
			{
				if (!member.IsImplicitlyDeclared && member.DeclaringSyntaxReferences.Length > 0 && seen.Add(member))
				{
					yield return member;
				}
			}
		}
	}

	private static VisibilityDeclarationEvidence? TryCreateVisibilityEvidence(ISymbol symbol, VisibilityPolicy policy, AnalyzerConfiguration config, string projectDirectory)
	{
		if (!symbol.TryGetArchitectureDeclarationTarget(out var target)
		    || !policy.Targets.Contains(target)
		    || !symbol.TryGetArchitectureAccessibility(out var accessibility))
		{
			return null;
		}

		var ownerType = symbol switch
		{
			INamedTypeSymbol namedType => namedType.ContainingType ?? namedType,
			_ => symbol.ContainingType
		};
		if (ownerType is null)
		{
			return null;
		}

		var layerMatch = config.Engine.FindLayer(ownerType.Name, ownerType.ContainingNamespace?.ToDisplayString() ?? string.Empty, ownerType);
		if (layerMatch is null || !ContainsLayer(layerMatch.Value, policy.OwnerLayerPath))
		{
			return null;
		}

		var location = symbol.Locations
			.Where(item => item.IsInSource)
			.OrderBy(item => item.SourceTree?.FilePath, StringComparer.OrdinalIgnoreCase)
			.ThenBy(item => item.SourceSpan.Start)
			.FirstOrDefault();
		var declarationName = symbol is INamedTypeSymbol
			? symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
			: symbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) + "." + symbol.Name;
		var result = new VisibilityDeclarationEvidence(
			declarationName,
			target.ToString(),
			accessibility.ToDisplayText(),
			policy.Evaluate(target, accessibility) is not null,
			symbol.IsEffectivelyExternallyVisible(),
			location is null ? string.Empty : FormatLocation(location, projectDirectory));

		return result;
	}

	private static bool ContainsLayer(LayerMatch match, string layerPath)
	{
		var result = match.Layers.Any(layer => string.Equals(layer.Name, layerPath, StringComparison.Ordinal));

		return result;
	}

	private sealed record VisibilityDeclarationEvidence(string DeclarationName, string Target, string Accessibility, bool IsViolation, bool IsEffectivelyExternallyVisible, string Location);
}

