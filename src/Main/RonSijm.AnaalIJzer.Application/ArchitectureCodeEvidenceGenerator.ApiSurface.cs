using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Declarations;
using RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Model;
using RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Traversal;
using RonSijm.AnaalIJzer.Core.ApiSurface.Engine.Policies;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Core.Visibility;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ArchitectureCodeEvidenceGenerator
{
	private static void AppendApiSurfaceEvidence(StringBuilder sb, Compilation compilation, AnalyzerConfiguration config, IReadOnlyList<INamedTypeSymbol> types, string projectDirectory, CancellationToken cancellationToken)
	{
		var policies = EnumerateApiSurfacePolicies(config.Layers).ToArray();
		if (policies.Length == 0)
		{
			return;
		}

		var declarations = EnumerateSourceDeclarations(types)
			.Where(symbol => symbol.IsEffectivelyExternallyVisible())
			.ToArray();
		var transitiveMemberCache = new ConcurrentDictionary<INamedTypeSymbol, ImmutableArray<ExposureMemberTypeReference>>(SymbolEqualityComparer.Default);
		sb.AppendLine("### API Exposure Evidence");
		sb.AppendLine();
		foreach (var policy in policies)
		{
			sb.AppendLine($"#### Layer `{Escape(policy.OwnerLayerPath)}`");
			sb.AppendLine();
			var evidence = new List<ApiExposureEvidence>();
			foreach (var declaration in declarations)
			{
				var ownerType = declaration as INamedTypeSymbol ?? declaration.ContainingType;
				if (ownerType is null)
				{
					continue;
				}

				var callerMatch = FindLayer(config, ownerType);
				if (callerMatch is null || !ContainsLayer(callerMatch.Value, policy.OwnerLayerPath))
				{
					continue;
				}

				foreach (var reference in ApiSurfaceDeclarationWalker.GetReferences(declaration, compilation, cancellationToken))
				{
					var dependencyMatch = FindLayer(config, reference.Type);
					var evaluation = policy.Evaluate(CreateApiSurfaceLayerSelection(dependencyMatch), reference.Type.Name, reference.Site);
					evidence.Add(new ApiExposureEvidence(
						GetApiMemberName(declaration),
						reference.Type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
						dependencyMatch?.Layer.Name ?? "unrecognized",
						reference.Site,
						evaluation is not null,
						evaluation?.Reason ?? "permitted by this API surface policy",
						FormatLocation(reference.Location, projectDirectory),
						null,
						null));
					if (evaluation is not null || policy.TransitiveExposure is not { } transitive || dependencyMatch is null)
					{
						continue;
					}

					var transitiveViolation = TransitiveExposureWalker.FindFirstViolation(
						reference.Type,
						GetApiMemberName(declaration),
						transitive.MaxDepth,
						transitiveMemberCache,
						(candidateType, site, depth) =>
						{
							var dependencyLayer = FindLayer(config, candidateType);
							var policyEvaluation = config.Engine.EvaluateApiSurfacePolicies(callerMatch.Value, dependencyLayer, candidateType.Name, site, depth);
							var result = (policyEvaluation, dependencyLayer?.Layer.Name);

							return result;
						},
						cancellationToken);
					if (transitiveViolation is null
					    || !string.Equals(transitiveViolation.Value.Evaluation.Policy.OwnerLayerPath, policy.OwnerLayerPath, StringComparison.Ordinal))
					{
						continue;
					}

					evidence.Add(new ApiExposureEvidence(
						GetApiMemberName(declaration),
						transitiveViolation.Value.ForbiddenType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
						transitiveViolation.Value.ForbiddenLayerName ?? "unrecognized",
						transitiveViolation.Value.Site,
						true,
						transitiveViolation.Value.Evaluation.Reason,
						FormatLocation(reference.Location, projectDirectory),
						transitiveViolation.Value.Path.ToDisplayText(transitiveViolation.Value.ForbiddenType.Name),
						transitiveViolation.Value.Depth));
				}
			}

			foreach (var item in evidence.OrderBy(item => item.MemberName, StringComparer.Ordinal).ThenBy(item => item.ExposedType, StringComparer.Ordinal).ThenBy(item => item.Site, StringComparer.Ordinal))
			{
				var status = item.IsViolation ? "violates" : "passes";
				var path = item.ExposurePath is null
					? string.Empty
					: $" through `{Escape(item.ExposurePath)}` at depth `{item.ExposureDepth}`";
				sb.AppendLine($"- **{status}** `{Escape(item.MemberName)}` exposes `{Escape(item.ExposedType)}` (`{Escape(item.ExposedLayer)}`) at `{Escape(item.Site)}`{path}: {Escape(item.Reason)} ({Escape(item.Location)})");
			}

			if (evidence.Count == 0)
			{
				sb.AppendLine("- No current externally visible declarations are selected by this policy.");
			}

			sb.AppendLine();
		}
	}

	private static IEnumerable<ApiSurfacePolicy> EnumerateApiSurfacePolicies(ImmutableArray<LayerNode> layers)
	{
		foreach (var layer in layers)
		{
			foreach (var policy in layer.ApiSurfacePolicies)
			{
				yield return policy;
			}

			foreach (var childPolicy in EnumerateApiSurfacePolicies(layer.Children))
			{
				yield return childPolicy;
			}
		}
	}

	private static LayerMatch? FindLayer(AnalyzerConfiguration config, INamedTypeSymbol type)
	{
		var namespaceName = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();
		var result = config.Engine.FindLayer(type.Name, namespaceName, type);

		return result;
	}

	private static ApiSurfaceLayerSelection CreateApiSurfaceLayerSelection(LayerMatch? layerMatch)
	{
		if (layerMatch is null)
		{
			return ApiSurfaceLayerSelection.Unrecognized;
		}

		var result = new ApiSurfaceLayerSelection(
			layerMatch.Value.Layer.Name,
			[..layerMatch.Value.Layers.Select(layer => layer.Name)]);

		return result;
	}

	private static string GetApiMemberName(ISymbol symbol)
	{
		var result = symbol is INamedTypeSymbol
			? symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
			: symbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) + "." + symbol.Name;

		return result;
	}

	private sealed record ApiExposureEvidence(
		string MemberName,
		string ExposedType,
		string ExposedLayer,
		string Site,
		bool IsViolation,
		string Reason,
		string Location,
		string? ExposurePath,
		int? ExposureDepth);
}

