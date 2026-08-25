using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Violations;

internal static class ViolationRecordFactory
{
	internal static ViolationRecord? TryCreate(Diagnostic diagnostic)
	{
		if (diagnostic.Id is not (ArchitecturalDiagnosticIds.IllegalLevelDependency
		    or ArchitecturalDiagnosticIds.UnrecognizedDependency
		    or ArchitecturalDiagnosticIds.ForbiddenDependency
		    or ArchitecturalDiagnosticIds.WrongDirectionDependency
		    or ArchitecturalDiagnosticIds.SameLayerDependency
		    or ArchitecturalDiagnosticIds.NameRuleViolation
		    or ArchitecturalDiagnosticIds.ApiSurfaceLeakage
		    or ArchitecturalDiagnosticIds.ProjectReferenceViolation
		    or ArchitecturalDiagnosticIds.PackageReferenceViolation
		    or ArchitecturalDiagnosticIds.VisibilityPolicyViolation
		    or ArchitecturalDiagnosticIds.ContractPurityViolation
		    or ArchitecturalDiagnosticIds.InheritancePolicyViolation
		    or ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure
		    or ArchitecturalDiagnosticIds.SourceLocationViolation
		    or ArchitecturalDiagnosticIds.BoundaryEntryPointViolation
		    or ArchitecturalDiagnosticIds.ObservedDependencyCycle))
		{
			return null;
		}

		var properties = diagnostic.Properties;
		var callerTypeName = Get(ArchitecturalDiagnostics.PropertyCallerTypeName) ?? "UnknownCaller";
		var callerLayerName = Get(ArchitecturalDiagnostics.PropertyCallerLayerName) ?? "UnknownLayer";
		var dependencyTypeName = diagnostic.Id switch
		{
			ArchitecturalDiagnosticIds.NameRuleViolation => Get(ArchitecturalDiagnostics.PropertySourceName) ?? "UnknownSource",
			ArchitecturalDiagnosticIds.VisibilityPolicyViolation => Get(ArchitecturalDiagnostics.PropertyDeclaredSymbolName) ?? "UnknownDeclaration",
			ArchitecturalDiagnosticIds.ContractPurityViolation => Get(ArchitecturalDiagnostics.PropertyDeclaredSymbolName) ?? "UnknownDeclaration",
			ArchitecturalDiagnosticIds.InheritancePolicyViolation => Get(ArchitecturalDiagnostics.PropertyDeclaredSymbolName) ?? "UnknownDeclaration",
			ArchitecturalDiagnosticIds.ProjectReferenceViolation => Get(ArchitecturalDiagnostics.PropertyTargetProjectName) ?? "UnknownTargetProject",
			ArchitecturalDiagnosticIds.PackageReferenceViolation => Get(ArchitecturalDiagnostics.PropertyPackageId) ?? "UnknownPackage",
			ArchitecturalDiagnosticIds.ObservedDependencyCycle => Get(ArchitecturalDiagnostics.PropertyCycleLayers) ?? "UnknownCycle",
			_ => Get(ArchitecturalDiagnostics.PropertyDepTypeName) ?? "UnknownDependency"
		};
		var dependencyLayerName = diagnostic.Id == ArchitecturalDiagnosticIds.NameRuleViolation
			? Get(ArchitecturalDiagnostics.PropertyTargetName) ?? "UnknownTarget"
			: diagnostic.Id == ArchitecturalDiagnosticIds.ProjectReferenceViolation
				? Get(ArchitecturalDiagnostics.PropertyTargetProjectGroup) ?? string.Empty
			: diagnostic.Id == ArchitecturalDiagnosticIds.PackageReferenceViolation
				? Get(ArchitecturalDiagnostics.PropertyPackageVersion) ?? string.Empty
			: Get(ArchitecturalDiagnostics.PropertyDepLayerName) ?? string.Empty;
		var violationReason = diagnostic.Id == ArchitecturalDiagnosticIds.ObservedDependencyCycle
			? diagnostic.GetMessage().Replace("Observed architectural dependency cycle: ", string.Empty)
			: Get(ArchitecturalDiagnostics.PropertyViolationReason) ?? diagnostic.GetMessage();
		var comment = Get(ArchitecturalDiagnostics.PropertyComment);
		if (diagnostic.Id == ArchitecturalDiagnosticIds.ProjectReferenceViolation)
		{
			callerTypeName = Get(ArchitecturalDiagnostics.PropertySourceProjectName) ?? "UnknownSourceProject";
			callerLayerName = Get(ArchitecturalDiagnostics.PropertySourceProjectGroup) ?? "UnknownProjectGroup";
		}

		if (diagnostic.Id == ArchitecturalDiagnosticIds.PackageReferenceViolation)
		{
			callerTypeName = Get(ArchitecturalDiagnostics.PropertySourceProjectName) ?? "UnknownSourceProject";
			callerLayerName = Get(ArchitecturalDiagnostics.PropertySourceProjectGroup) ?? "UnknownProjectGroup";
		}

		var result = new ViolationRecord(
			diagnostic.Id,
			callerTypeName,
			callerLayerName,
			dependencyTypeName,
			dependencyLayerName,
			violationReason,
			string.IsNullOrWhiteSpace(comment) ? null : comment,
			diagnostic.Id is ArchitecturalDiagnosticIds.ApiSurfaceLeakage or ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure
				? Get(ArchitecturalDiagnostics.PropertySite)
				: diagnostic.Id == ArchitecturalDiagnosticIds.ContractPurityViolation
					? Get(ArchitecturalDiagnostics.PropertyContractViolationKind)
					: Get(ArchitecturalDiagnostics.PropertyDeclarationTarget),
			Get(ArchitecturalDiagnostics.PropertyDeclaredAccessibility),
			Get(ArchitecturalDiagnostics.PropertyApiMemberName),
			Get(ArchitecturalDiagnostics.PropertyExposurePath),
			int.TryParse(Get(ArchitecturalDiagnostics.PropertyExposureDepth), out var exposureDepth) ? exposureDepth : null,
			Get(ArchitecturalDiagnostics.PropertyNestedMemberName),
			Get(ArchitecturalDiagnostics.PropertySourceProjectPath),
			Get(ArchitecturalDiagnostics.PropertySourceProjectName),
			Get(ArchitecturalDiagnostics.PropertySourceProjectGroup),
			Get(ArchitecturalDiagnostics.PropertyTargetProjectPath),
			Get(ArchitecturalDiagnostics.PropertyTargetProjectName),
			Get(ArchitecturalDiagnostics.PropertyTargetProjectGroup),
			Get(ArchitecturalDiagnostics.PropertyPackageId),
			Get(ArchitecturalDiagnostics.PropertyPackageVersion),
			Get(ArchitecturalDiagnostics.PropertyPackageReferenceKind),
			Get(ArchitecturalDiagnostics.PropertySourceFilePath),
			Get(ArchitecturalDiagnostics.PropertyNormalizedSourcePath),
			Get(ArchitecturalDiagnostics.PropertySourceAssemblyName),
			Get(ArchitecturalDiagnostics.PropertyBoundaryLayerName),
			Get(ArchitecturalDiagnostics.PropertyMatchedEntryPoint),
			Get(ArchitecturalDiagnostics.PropertyCycleLayers),
			int.TryParse(Get(ArchitecturalDiagnostics.PropertyCycleLength), out var cycleLength) ? cycleLength : null,
			Get(ArchitecturalDiagnostics.PropertyObservedSites),
			Get(ArchitecturalDiagnostics.PropertyCycleScope));

		return result;

		string? Get(string key)
		{
			var value = properties.TryGetValue(key, out var propertyValue) ? propertyValue : null;

			return value;
		}
	}

	internal static string ExtractSuggestedLayerSuffix(string typeName)
	{
		if (typeName.Length > 1 && typeName[0] == 'I' && char.IsUpper(typeName[1]))
		{
			return typeName.Substring(1);
		}

		return typeName;
	}
}
