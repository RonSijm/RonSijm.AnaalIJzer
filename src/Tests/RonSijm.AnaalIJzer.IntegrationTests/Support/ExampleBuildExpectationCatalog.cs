using RonSijm.AnaalIJzer;

namespace RonSijm.AnaalIJzer.IntegrationTests;

internal static class ExampleBuildExpectationCatalog
{
	public static IReadOnlyList<ExampleBuildExpectation> All { get; } =
	[
		Expect("Diagnostics/Example.Arch001.GenericTypeArgument", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 3)),
		Expect("Diagnostics/Example.Arch001.NoEdge", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 1)),
		ExpectFile("Diagnostics/Example.Arch001.NonConstructorInjection", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 8)),
		Expect("Diagnostics/Example.Arch001.SkipsLayer", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 1)),
		Expect("Diagnostics/Example.Arch002.UnrecognizedDependency", (ArchitecturalDiagnosticIds.UnrecognizedDependency, 1)),
		Expect("Diagnostics/Example.Arch003.ForbiddenType", (ArchitecturalDiagnosticIds.ForbiddenDependency, 1)),
		Expect("Diagnostics/Example.Arch004.WrongDirection", (ArchitecturalDiagnosticIds.WrongDirectionDependency, 1)),
		Expect("Diagnostics/Example.Arch005.SameLayer", (ArchitecturalDiagnosticIds.SameLayerDependency, 1)),
		Expect("Diagnostics/Example.Arch006.UnknownLayer", (ArchitecturalDiagnosticIds.InvalidConfiguration, 1)),
		Expect("Diagnostics/Example.Arch007.CyclicGraph", (ArchitecturalDiagnosticIds.CyclicDependencyGraph, 1)),
		Expect("Diagnostics/Example.Arch009.ApiSurfaceLeakage", (ArchitecturalDiagnosticIds.ApiSurfaceLeakage, 1)),
		Expect("Diagnostics/Example.Arch012.VisibilityPolicy", (ArchitecturalDiagnosticIds.VisibilityPolicyViolation, 1)),
		Expect("Diagnostics/Example.Arch013.ContractPurity", (ArchitecturalDiagnosticIds.ContractPurityViolation, 1)),
		Expect("Diagnostics/Example.Arch013.ContractPurity.MethodBodyNotAllowed", (ArchitecturalDiagnosticIds.ContractPurityViolation, 1)),
		Expect("Diagnostics/Example.Arch014.TransitiveExposure", (ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure, 1)),
		ExpectFile("Diagnostics/Example.Arch016.BoundaryEntryPoints", (ArchitecturalDiagnosticIds.BoundaryEntryPointViolation, 1)),
		ExpectFile("Diagnostics/Example.Arch018.ObservedCycle", (ArchitecturalDiagnosticIds.ObservedDependencyCycle, 1)),
		Expect("Diagnostics/Example.Arch019.InheritancePolicy", (ArchitecturalDiagnosticIds.InheritancePolicyViolation, 1)),
		ExpectFile("Features/Example.AllowedSites", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 26)),
		Expect("Features/Example.AllowedTypes", (ArchitecturalDiagnosticIds.ForbiddenDependency, 1)),
		ExpectFile("Features/Example.ArchitectureHealth"),
		Expect("Features/Example.AssemblyMatcher", (ArchitecturalDiagnosticIds.WrongDirectionDependency, 1)),
		Expect("Features/Example.BlockedDependency", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 1)),
		ExpectFile("Features/Example.CascadingDependencyRules"),
		Expect("Features/Example.CombinedMatchers", (ArchitecturalDiagnosticIds.SameLayerDependency, 1)),
		Expect("Features/Example.DeclarationNameMatchesType", (ArchitecturalDiagnosticIds.NameRuleViolation, 6)),
		Expect("Features/Example.ExceptionPolicy", (ArchitecturalDiagnosticIds.ExceptionReview, 1)),
		Expect("Features/Example.Exceptions", (ArchitecturalDiagnosticIds.ForbiddenDependency, 1)),
		ExpectFile("Features/Example.IncludeSettings", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 1)),
		Expect("Features/Example.InlineXml", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 1)),
		Expect("Features/Example.LayerScopedRecognizedDependencies", (ArchitecturalDiagnosticIds.UnrecognizedDependency, 1)),
		Expect("Features/Example.NameRules", (ArchitecturalDiagnosticIds.NameRuleViolation, 4)),
		Expect("Features/Example.NestedExceptions", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 2)),
		ExpectFile("Features/Example.NestedLayers"),
		Expect("Features/Example.NonClassCallers", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 3)),
		ExpectFile("Features/Example.RequiredRecognizedDependencySites", (ArchitecturalDiagnosticIds.UnrecognizedDependency, 13)),
		Expect("Features/Example.SameLayerInheritance", (ArchitecturalDiagnosticIds.SameLayerDependency, 1)),
		Expect("Features/Example.ScopedTypePolicies", (ArchitecturalDiagnosticIds.ForbiddenDependency, 2)),
		ExpectFile("Features/Example.SourceLocations", (ArchitecturalDiagnosticIds.SourceLocationViolation, 1)),
		ExpectFile("Scenarios/Example.HonestTypeEndpointNames", (ArchitecturalDiagnosticIds.NameRuleViolation, 2)),
		ExpectFile("Scenarios/Example.PackageReferenceBoundaries/Example.PackageReferenceBoundaries.Data"),
		ExpectFile("Scenarios/Example.PackageReferenceBoundaries/Example.PackageReferenceBoundaries.Domain", (ArchitecturalDiagnosticIds.PackageReferenceViolation, 1)),
		ExpectFile("Scenarios/Example.ProjectReferenceBoundaries/Example.ProjectReferenceBoundaries.Application"),
		ExpectFile("Scenarios/Example.ProjectReferenceBoundaries/Example.ProjectReferenceBoundaries.Domain", (ArchitecturalDiagnosticIds.ProjectReferenceViolation, 1)),
		ExpectFile("Scenarios/Example.ProjectReferenceBoundaries/Example.ProjectReferenceBoundaries.Infrastructure"),
		ExpectFile("Scenarios/Example.RepositoryQuerySurface", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 2)),
		ExpectFile("Documentation/Example.DocumentationDemo", (ArchitecturalDiagnosticIds.UnrecognizedDependency, 1)),
		Expect("Documentation/Example.ReportDemo", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 1), (ArchitecturalDiagnosticIds.UnrecognizedDependency, 1), (ArchitecturalDiagnosticIds.ForbiddenDependency, 1), (ArchitecturalDiagnosticIds.WrongDirectionDependency, 1), (ArchitecturalDiagnosticIds.SameLayerDependency, 1)),
		Expect("Features/Example.WildcardTo")
	];

	private static ExampleBuildExpectation Expect(string projectName, params (string Id, int Count)[] diagnostics)
	{
		var result = new ExampleBuildExpectation(projectName, ExampleConfigStyle.InlineInExample, diagnostics.ToDictionary(diagnostic => diagnostic.Id, diagnostic => diagnostic.Count, StringComparer.Ordinal));

		return result;
	}

	private static ExampleBuildExpectation ExpectFile(string projectName, params (string Id, int Count)[] diagnostics)
	{
		var result = new ExampleBuildExpectation(projectName, ExampleConfigStyle.SettingsFile, diagnostics.ToDictionary(diagnostic => diagnostic.Id, diagnostic => diagnostic.Count, StringComparer.Ordinal));

		return result;
	}
}

internal enum ExampleConfigStyle
{
	InlineInExample,
	SettingsFile
}

internal sealed record ExampleBuildExpectation(string RelativeProjectPath, ExampleConfigStyle ConfigStyle, IReadOnlyDictionary<string, int> Diagnostics);
