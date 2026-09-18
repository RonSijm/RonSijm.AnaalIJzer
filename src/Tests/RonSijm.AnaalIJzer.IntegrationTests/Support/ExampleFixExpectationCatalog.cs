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
			"Diagnostics/DEP/Example.Arch_DEP_002.UnrecognizedDependency",
			@"Diagnostics\DEP\Example.Arch_DEP_002.UnrecognizedDependency\Example.cs",
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
			"Diagnostics/DEP/Example.Arch_DEP_004.WrongDirection",
			@"Diagnostics\DEP\Example.Arch_DEP_004.WrongDirection\Example.cs",
			"Add allowed dependency 'Pantry' -> 'Chef'",
			"Flip configured dependency 'Chef' -> 'Pantry' to 'Pantry' -> 'Chef'"),
		Expect(
			"Diagnostics/CONF/Example.Arch_CONF_006.CyclicGraph",
			@"Diagnostics\CONF\Example.Arch_CONF_006.CyclicGraph\Example.cs",
			"Break configured cycle by blocking 'Ordering' -> 'Inventory'",
			"Break configured cycle by removing allowed dependency 'Ordering' -> 'Inventory'"),
		Expect(
			"Scenarios/Example.ProjectReferenceBoundaries/Example.ProjectReferenceBoundaries.Domain",
			"Architecture.anl",
			"Remove blocking <BlockedProjectReference from=\"Domain\" to=\"Infrastructure\" />"),
		ExpectNoProposals("Scenarios/Example.PackageReferenceBoundaries/Example.PackageReferenceBoundaries.Domain"),
		ExpectNoProposals("Diagnostics/DEP/Example.Arch_DEP_006.ObservedCycle"),
		ExpectNoProposals("Diagnostics/RET/Example.Arch_RET_001.ExplicitNullReturn"),
		ExpectNoProposals("Diagnostics/RET/Example.Arch_RET_001.AnnotatedInvocationReturn"),
		ExpectNoProposals("Diagnostics/RET/Example.Arch_RET_001.ConfiguredLiteralReturns"),
		ExpectNoProposals("Diagnostics/RET/Example.Arch_RET_001.OnlyIdentifierReturn"),
		ExpectNoProposals("Features/Example.GlobalReturnValuePolicy"),
		ExpectNoProposals("Diagnostics/OPCT/Example.Arch_OPCT_001.ParticipantNotAllowed"),
		ExpectNoProposals("Diagnostics/OPCT/Example.Arch_OPCT_002.RequiredOwnerInvocation"),
		ExpectNoProposals("Diagnostics/OPCT/Example.Arch_OPCT_008.ResponseShapeMismatch"),
		ExpectNoProposals("Diagnostics/ASSM/Example.Arch_ASSM_001.AssemblyAttributePolicy.Code"),
		ExpectNoProposals("Diagnostics/ASSM/Example.Arch_ASSM_001.AssemblyAttributePolicy.Project"),
		ExpectNoProposals("Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.AncestorToDescendant"),
		ExpectNoProposals("Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.DescendantToAncestor"),
		ExpectNoProposals("Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.SameNamespace"),
		ExpectNoProposals("Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.SiblingToSibling"),
		ExpectNoProposals("Features/Example.NamespaceHierarchySites")
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
