using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Violations;
using RonSijm.AnaalIJzer.Diagnostics;

namespace RonSijm.AnaalIJzer.Outputs.Violations;

internal static class ViolationRecordFactory
{
	internal static ViolationRecord? TryCreate(Diagnostic diagnostic)
	{
		if (diagnostic.Id is not (ArchitecturalDiagnosticIds.DependencyNotAllowed
		    or ArchitecturalDiagnosticIds.DependencyRequiredMissing
		    or ArchitecturalDiagnosticIds.TypeNotAllowed
		    or ArchitecturalDiagnosticIds.DependencyReverseDirection
		    or ArchitecturalDiagnosticIds.DependencyPeerScope
		    or ArchitecturalDiagnosticIds.NameShapeMismatch
		    or ArchitecturalDiagnosticIds.ApiExposureNotAllowed
		    or ArchitecturalDiagnosticIds.ProjectReferenceNotAllowed
		    or ArchitecturalDiagnosticIds.PackageReferenceNotAllowed
		    or ArchitecturalDiagnosticIds.VisibilityNotAllowed
		    or ArchitecturalDiagnosticIds.ContractShapeMismatch
		    or ArchitecturalDiagnosticIds.InheritanceNotAllowed
		    or ArchitecturalDiagnosticIds.ReturnNotAllowed
		    or ArchitecturalDiagnosticIds.OperationNotAllowed
		    or ArchitecturalDiagnosticIds.OperationRequiredMissing
		    or ArchitecturalDiagnosticIds.OperationCardinality
		    or ArchitecturalDiagnosticIds.OperationOrdering
		    or ArchitecturalDiagnosticIds.OperationContractNotAllowed
		    or ArchitecturalDiagnosticIds.OperationContractRequiredMissing
		    or ArchitecturalDiagnosticIds.OperationContractShapeMismatch
		    or ArchitecturalDiagnosticIds.AssemblyAttributeNotAllowed
		    or ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement
		    or ArchitecturalDiagnosticIds.ApiTransitiveExposure
		    or ArchitecturalDiagnosticIds.SourceBoundaryPlacement
		    or ArchitecturalDiagnosticIds.BoundaryEntryPlacement
		    or ArchitecturalDiagnosticIds.DependencyCycle))
		{
			return null;
		}

		var properties = diagnostic.Properties;
		var callerTypeName = Get(ArchitecturalDiagnostics.PropertyCallerTypeName) ?? "UnknownCaller";
		var callerLayerName = Get(ArchitecturalDiagnostics.PropertyCallerLayerName) ?? "UnknownLayer";
		var dependencyTypeName = diagnostic.Id switch
		{
			ArchitecturalDiagnosticIds.NameShapeMismatch => Get(ArchitecturalDiagnostics.PropertySourceName) ?? "UnknownSource",
			ArchitecturalDiagnosticIds.VisibilityNotAllowed => Get(ArchitecturalDiagnostics.PropertyDeclaredSymbolName) ?? "UnknownDeclaration",
			ArchitecturalDiagnosticIds.ContractShapeMismatch => Get(ArchitecturalDiagnostics.PropertyDeclaredSymbolName) ?? "UnknownDeclaration",
			ArchitecturalDiagnosticIds.InheritanceNotAllowed => Get(ArchitecturalDiagnostics.PropertyDeclaredSymbolName) ?? "UnknownDeclaration",
			ArchitecturalDiagnosticIds.ReturnNotAllowed => Get(ArchitecturalDiagnostics.PropertyDeclaredSymbolName) ?? "UnknownMethod",
			ArchitecturalDiagnosticIds.OperationNotAllowed => Get(ArchitecturalDiagnostics.PropertyOperationDisplayName) ?? "UnknownOperation",
			ArchitecturalDiagnosticIds.OperationRequiredMissing or ArchitecturalDiagnosticIds.OperationCardinality or ArchitecturalDiagnosticIds.OperationOrdering => Get(ArchitecturalDiagnostics.PropertyOperationDisplayName) ?? Get(ArchitecturalDiagnostics.PropertyOperationPolicyRule) ?? "UnknownOperationPolicy",
			ArchitecturalDiagnosticIds.OperationContractNotAllowed or ArchitecturalDiagnosticIds.OperationContractRequiredMissing or ArchitecturalDiagnosticIds.OperationContractShapeMismatch => Get(ArchitecturalDiagnostics.PropertyOperationContractName) ?? "UnknownOperationContract",
			ArchitecturalDiagnosticIds.AssemblyAttributeNotAllowed => Get(ArchitecturalDiagnostics.PropertyAssemblyAttributeTypeName) ?? "UnknownAssemblyAttribute",
			ArchitecturalDiagnosticIds.ProjectReferenceNotAllowed => Get(ArchitecturalDiagnostics.PropertyTargetProjectName) ?? "UnknownTargetProject",
			ArchitecturalDiagnosticIds.PackageReferenceNotAllowed => Get(ArchitecturalDiagnostics.PropertyPackageId) ?? "UnknownPackage",
			ArchitecturalDiagnosticIds.DependencyCycle => Get(ArchitecturalDiagnostics.PropertyCycleLayers) ?? "UnknownCycle",
			_ => Get(ArchitecturalDiagnostics.PropertyDepTypeName) ?? "UnknownDependency"
		};
		var dependencyLayerName = diagnostic.Id == ArchitecturalDiagnosticIds.NameShapeMismatch
			? Get(ArchitecturalDiagnostics.PropertyTargetName) ?? "UnknownTarget"
			: diagnostic.Id == ArchitecturalDiagnosticIds.ProjectReferenceNotAllowed
				? Get(ArchitecturalDiagnostics.PropertyTargetProjectGroup) ?? string.Empty
			: diagnostic.Id == ArchitecturalDiagnosticIds.PackageReferenceNotAllowed
				? Get(ArchitecturalDiagnostics.PropertyPackageVersion) ?? string.Empty
			: diagnostic.Id == ArchitecturalDiagnosticIds.AssemblyAttributeNotAllowed
				? Get(ArchitecturalDiagnostics.PropertyAssemblyAttributePolicyRule) ?? string.Empty
			: diagnostic.Id == ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement
				? Get(ArchitecturalDiagnostics.PropertyDependencyNamespace) ?? string.Empty
			: Get(ArchitecturalDiagnostics.PropertyDepLayerName) ?? string.Empty;
		var violationReason = diagnostic.Id == ArchitecturalDiagnosticIds.DependencyCycle
			? diagnostic.GetMessage().Replace("Observed architectural dependency cycle: ", string.Empty)
			: Get(ArchitecturalDiagnostics.PropertyViolationReason) ?? diagnostic.GetMessage();
		var comment = Get(ArchitecturalDiagnostics.PropertyComment);
		if (diagnostic.Id == ArchitecturalDiagnosticIds.ProjectReferenceNotAllowed)
		{
			callerTypeName = Get(ArchitecturalDiagnostics.PropertySourceProjectName) ?? "UnknownSourceProject";
			callerLayerName = Get(ArchitecturalDiagnostics.PropertySourceProjectGroup) ?? "UnknownProjectGroup";
		}

		if (diagnostic.Id == ArchitecturalDiagnosticIds.PackageReferenceNotAllowed)
		{
			callerTypeName = Get(ArchitecturalDiagnostics.PropertySourceProjectName) ?? "UnknownSourceProject";
			callerLayerName = Get(ArchitecturalDiagnostics.PropertySourceProjectGroup) ?? "UnknownProjectGroup";
		}

		if (diagnostic.Id == ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement)
		{
			callerLayerName = Get(ArchitecturalDiagnostics.PropertyCallerNamespace) ?? "UnknownNamespace";
		}

		var result = new ViolationRecord(
			diagnostic.Id,
			callerTypeName,
			callerLayerName,
			dependencyTypeName,
			dependencyLayerName,
			violationReason,
			string.IsNullOrWhiteSpace(comment) ? null : comment,
			diagnostic.Id is ArchitecturalDiagnosticIds.ApiExposureNotAllowed or ArchitecturalDiagnosticIds.ApiTransitiveExposure
				? Get(ArchitecturalDiagnostics.PropertySite)
			: diagnostic.Id == ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement
				? Get(ArchitecturalDiagnostics.PropertySite)
			: diagnostic.Id == ArchitecturalDiagnosticIds.ContractShapeMismatch
					? Get(ArchitecturalDiagnostics.PropertyContractViolationKind)
					: diagnostic.Id == ArchitecturalDiagnosticIds.ReturnNotAllowed
						? Get(ArchitecturalDiagnostics.PropertyReturnValueRuleTarget)
					: diagnostic.Id == ArchitecturalDiagnosticIds.OperationNotAllowed || ArchitecturalDiagnosticIds.IsBehavioralOperationPolicy(diagnostic.Id)
						? Get(ArchitecturalDiagnostics.PropertyOperationKind)
						: ArchitecturalDiagnosticIds.IsOperationContract(diagnostic.Id)
							? Get(ArchitecturalDiagnostics.PropertyOperationContractViolationKind)
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
