using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Violations;
using RonSijm.AnaalIJzer.Diagnostics;

namespace RonSijm.AnaalIJzer.Outputs.Violations;

internal static class ViolationRecordFindingExtensions
{
	internal static ArchitectureFinding ToArchitectureFinding(this ViolationRecord violation)
	{
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitecturalDiagnostics.PropertyCallerTypeName, violation.CallerTypeName)
			.Add(ArchitecturalDiagnostics.PropertyCallerLayerName, violation.CallerLayerName)
			.Add(ArchitecturalDiagnostics.PropertyDepTypeName, violation.DependencyTypeName)
			.Add(ArchitecturalDiagnostics.PropertyDepLayerName, violation.DepLayerName)
			.Add(ArchitecturalDiagnostics.PropertyViolationReason, violation.ViolationReason)
			.Add(ArchitecturalDiagnostics.PropertyDeclarationTarget, violation.DeclarationTarget)
			.Add(ArchitecturalDiagnostics.PropertyDeclaredAccessibility, violation.DeclaredAccessibility)
			.Add(ArchitecturalDiagnostics.PropertyApiMemberName, violation.ApiMemberName)
			.Add(ArchitecturalDiagnostics.PropertyExposurePath, violation.ExposurePath)
			.Add(ArchitecturalDiagnostics.PropertyExposureDepth, violation.ExposureDepth?.ToString(System.Globalization.CultureInfo.InvariantCulture))
			.Add(ArchitecturalDiagnostics.PropertyNestedMemberName, violation.NestedMemberName)
			.Add(ArchitecturalDiagnostics.PropertySourceProjectPath, violation.SourceProjectPath)
			.Add(ArchitecturalDiagnostics.PropertySourceProjectName, violation.SourceProjectName)
			.Add(ArchitecturalDiagnostics.PropertySourceProjectGroup, violation.SourceProjectGroup)
			.Add(ArchitecturalDiagnostics.PropertyTargetProjectPath, violation.TargetProjectPath)
			.Add(ArchitecturalDiagnostics.PropertyTargetProjectName, violation.TargetProjectName)
			.Add(ArchitecturalDiagnostics.PropertyTargetProjectGroup, violation.TargetProjectGroup)
			.Add(ArchitecturalDiagnostics.PropertyPackageId, violation.PackageId)
			.Add(ArchitecturalDiagnostics.PropertyPackageVersion, violation.PackageVersion)
			.Add(ArchitecturalDiagnostics.PropertyPackageReferenceKind, violation.PackageReferenceKind)
			.Add(ArchitecturalDiagnostics.PropertySourceFilePath, violation.SourceFilePath)
			.Add(ArchitecturalDiagnostics.PropertyNormalizedSourcePath, violation.NormalizedSourcePath)
			.Add(ArchitecturalDiagnostics.PropertySourceAssemblyName, violation.SourceAssemblyName)
			.Add(ArchitecturalDiagnostics.PropertyBoundaryLayerName, violation.BoundaryLayerName)
			.Add(ArchitecturalDiagnostics.PropertyMatchedEntryPoint, violation.MatchedEntryPoint)
			.Add(ArchitecturalDiagnostics.PropertyCycleLayers, violation.CycleLayers)
			.Add(ArchitecturalDiagnostics.PropertyCycleLength, violation.CycleLength?.ToString(System.Globalization.CultureInfo.InvariantCulture))
			.Add(ArchitecturalDiagnostics.PropertyObservedSites, violation.ObservedSites)
			.Add(ArchitecturalDiagnostics.PropertyCycleScope, violation.CycleScope);
		var result = new ArchitectureFinding(
			ArchitectureFindingSeverity.Error,
			violation.DiagnosticId,
			violation.ViolationReason,
			violation.CallerTypeName,
			reasonCode: violation.DeclarationTarget,
			properties: properties);

		return result;
	}
}
