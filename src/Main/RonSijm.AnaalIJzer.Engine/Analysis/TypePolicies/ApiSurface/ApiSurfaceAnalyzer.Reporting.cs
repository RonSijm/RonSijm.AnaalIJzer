using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Model;
using RonSijm.AnaalIJzer.Core.ApiSurface.Engine.Policies;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Diagnostics;

namespace RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.ApiSurface;

internal static partial class ApiSurfaceAnalyzer
{
	private static void ReportDirectViolation(
		SymbolAnalysisContext context,
		INamedTypeSymbol ownerType,
		LayerMatch callerLayer,
		INamedTypeSymbol dependencyType,
		LayerMatch? dependencyLayer,
		ApiSurfaceTypeReference reference,
		ApiSurfaceEvaluation evaluation,
		string apiMemberName,
		ISet<ApiSurfaceDiagnosticKey> reported)
	{
		var key = new ApiSurfaceDiagnosticKey(reference.Location.SourceTree, reference.Location.SourceSpan, dependencyType, reference.Site);
		if (!reported.Add(key))
		{
			return;
		}

		var policy = evaluation.Policy;
		var dependencyLayerName = dependencyLayer?.Layer.Name ?? "unrecognized";
		var properties = CreateProperties(
			ownerType.Name,
			callerLayer.Layer.Name,
			dependencyType.Name,
			dependencyLayerName,
			reference.Site,
			evaluation.Reason,
			apiMemberName,
			policy);

		context.ReportDiagnostic(Diagnostic.Create(
			ArchitecturalDiagnostics.ApiSurfaceLeakage,
			reference.Location,
			properties,
			ownerType.Name,
			callerLayer.Layer.Name,
			dependencyType.Name,
			dependencyLayerName,
			reference.Site,
			evaluation.Reason));
	}

	private static void ReportTransitiveViolation(
		SymbolAnalysisContext context,
		INamedTypeSymbol ownerType,
		LayerMatch callerLayer,
		ApiSurfaceTypeReference rootReference,
		TransitiveExposureViolationCandidate violation,
		ISet<ApiSurfaceDiagnosticKey> reported)
	{
		var key = new ApiSurfaceDiagnosticKey(rootReference.Location.SourceTree, rootReference.Location.SourceSpan, violation.ForbiddenType, violation.Site);
		if (!reported.Add(key))
		{
			return;
		}

		var dependencyLayerName = violation.ForbiddenLayerName ?? "unrecognized";
		var path = violation.Path.ToDisplayText(violation.ForbiddenType.Name);
		var properties = CreateProperties(
				ownerType.Name,
				callerLayer.Layer.Name,
				violation.ForbiddenType.Name,
				dependencyLayerName,
				violation.Site,
				violation.Evaluation.Reason,
				violation.Path.RootMember,
				violation.Evaluation.Policy)
			.Add(ArchitecturalDiagnostics.PropertyExposureRootMember, violation.Path.RootMember)
			.Add(ArchitecturalDiagnostics.PropertyExposurePath, path)
			.Add(ArchitecturalDiagnostics.PropertyExposureDepth, violation.Depth.ToString(System.Globalization.CultureInfo.InvariantCulture))
			.Add(ArchitecturalDiagnostics.PropertyNestedMemberName, violation.NestedMember?.Name)
			.Add(ArchitecturalDiagnostics.PropertyNestedMemberContainingType, violation.NestedMember?.ContainingType?.ToDisplayString());
		var additionalLocations = violation.NestedLocation is { IsInSource: true } nestedLocation
		                          && !nestedLocation.Equals(rootReference.Location)
			? ImmutableArray.Create(nestedLocation)
			: ImmutableArray<Location>.Empty;

		context.ReportDiagnostic(Diagnostic.Create(
			ArchitecturalDiagnostics.ForbiddenTransitiveExposure,
			rootReference.Location,
			additionalLocations,
			properties,
			ownerType.Name,
			callerLayer.Layer.Name,
			violation.ForbiddenType.Name,
			dependencyLayerName,
			path,
			violation.Evaluation.Reason));
	}

	private static ImmutableDictionary<string, string?> CreateProperties(
		string callerName,
		string callerLayerName,
		string dependencyTypeName,
		string dependencyLayerName,
		string site,
		string reason,
		string apiMemberName,
		ApiSurfacePolicy policy)
	{
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitecturalDiagnostics.PropertyCallerTypeName, callerName)
			.Add(ArchitecturalDiagnostics.PropertyCallerLayerName, callerLayerName)
			.Add(ArchitecturalDiagnostics.PropertyDepTypeName, dependencyTypeName)
			.Add(ArchitecturalDiagnostics.PropertyDepLayerName, dependencyLayerName)
			.Add(ArchitecturalDiagnostics.PropertySite, site)
			.Add(ArchitecturalDiagnostics.PropertyViolationReason, reason)
			.Add(ArchitecturalDiagnostics.PropertyApiMemberName, apiMemberName)
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlPath, policy.XmlPath)
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, policy.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture))
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, policy.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture));

		return properties;
	}
}
