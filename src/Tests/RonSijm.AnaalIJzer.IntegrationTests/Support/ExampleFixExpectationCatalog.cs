namespace RonSijm.AnaalIJzer.IntegrationTests.Support;

internal static class ExampleFixExpectationCatalog
{
	public static IReadOnlyList<ExampleFixExpectation> All { get; } =
	[
		Expect(
			"Features/Example.IncludeSettings",
			@"Features\Example.IncludeSettings\Architecture.anl",
			"Add allowed dependency 'Presentation' -> 'Persistence'"),
		Expect(
			"Features/Example.InlineXml",
			@"Features\Example.InlineXml\Example.cs",
			"Add allowed dependency 'Presentation' -> 'Persistence'"),
		Expect(
			"Features/Example.AllowedSites",
			@"Features\Example.AllowedSites\Architecture.anl",
			"Add site 'Constructor' to allowedSites for 'Caller' -> 'AllowedLocalDependency'",
			"Remove site 'Field' from blockedSites for 'Caller' -> 'BlockedFieldDependency'"),
		Expect(
			"Diagnostics/Example.Arch002.UnrecognizedDependency",
			@"Diagnostics\Example.Arch002.UnrecognizedDependency\Example.cs",
			"Classify 'MysteryBox' into layer 'Chef'",
			"Stop requiring recognized dependencies at Constructor globally"),
		Expect(
			"Features/Example.NameRules",
			@"Features\Example.NameRules\Example.cs",
			"Add <Allow from=\"legacy.customer.id\" to=\"customer.id\" /> to name rule",
			"Add site-scoped <Allow from=\"legacy.customer.id\" to=\"customer.id\" /> for Method"),
		Expect(
			"Features/Example.SourceLocations",
			@"Features\Example.SourceLocations\Architecture.anl",
			"Add source location 'Infrastructure/MisplacedCandyService.cs' to layer 'Ordering'"),
		Expect(
			"Diagnostics/Example.Arch004.WrongDirection",
			@"Diagnostics\Example.Arch004.WrongDirection\Example.cs",
			"Add allowed dependency 'Pantry' -> 'Chef'",
			"Flip configured dependency 'Chef' -> 'Pantry' to 'Pantry' -> 'Chef'"),
		Expect(
			"Diagnostics/Example.Arch007.CyclicGraph",
			@"Diagnostics\Example.Arch007.CyclicGraph\Example.cs",
			"Break configured cycle by blocking 'Ordering' -> 'Inventory'",
			"Break configured cycle by removing allowed dependency 'Ordering' -> 'Inventory'"),
		Expect(
			"Scenarios/Example.ProjectReferenceBoundaries/Example.ProjectReferenceBoundaries.Domain",
			"Architecture.anl",
			"Remove blocking <BlockedProjectReference from=\"Domain\" to=\"Infrastructure\" />"),
		ExpectNoProposals("Scenarios/Example.PackageReferenceBoundaries/Example.PackageReferenceBoundaries.Domain"),
		ExpectNoProposals("Diagnostics/Example.Arch018.ObservedCycle"),
		ExpectNoProposals("Diagnostics/Example.Arch020.ExplicitNullReturn"),
		ExpectNoProposals("Diagnostics/Example.Arch020.AnnotatedInvocationReturn"),
		ExpectNoProposals("Diagnostics/Example.Arch020.ConfiguredLiteralReturns")
	];

	private static ExampleFixExpectation Expect(string relativeProjectPath, string expectedTargetSuffix, params string[] expectedTitles)
	{
		var result = new ExampleFixExpectation(relativeProjectPath, expectedTitles, expectedTargetSuffix, false);

		return result;
	}

	private static ExampleFixExpectation ExpectNoProposals(string relativeProjectPath)
	{
		var result = new ExampleFixExpectation(relativeProjectPath, [], null, true);

		return result;
	}
}

internal sealed record ExampleFixExpectation(
	string RelativeProjectPath,
	IReadOnlyList<string> ExpectedTitles,
	string? ExpectedTargetSuffix,
	bool ExpectNoProposals);
