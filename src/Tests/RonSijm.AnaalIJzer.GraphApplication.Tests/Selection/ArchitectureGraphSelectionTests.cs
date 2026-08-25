namespace RonSijm.AnaalIJzer.GraphApplication.Tests.Selection;

public sealed class ArchitectureGraphSelectionTests
{
	[Fact]
	public void ForLayer_UsesLayerPathAndSourcePath()
	{
		var handle = new ArchitectureLayerEditHandle(
			ArchitectureConfigurationSourceKind.XmlFile,
			@"D:\repo\Architecture.anl",
			12,
			"Application/Implementation",
			"Implementation",
			"Application",
			"Application services.");

		var result = ArchitectureGraphSelection.ForLayer(handle);

		result.Kind.Should().Be(ArchitectureGraphSelectionKind.Layer);
		result.Title.Should().Be("Application/Implementation");
		result.Subtitle.Should().Be(@"D:\repo\Architecture.anl");
		result.LayerHandle.Should().BeSameAs(handle);
		result.DependencyHandle.Should().BeSameAs(ArchitectureDependencyRuleEditHandle.None);
	}

	[Fact]
	public void ForDependency_CarriesConfiguredSites()
	{
		var handle = new ArchitectureDependencyRuleEditHandle(
			ArchitectureConfigurationSourceKind.XmlFile,
			@"D:\repo\Architecture.anl",
			20,
			4,
			"AllowedDependency",
			"Application",
			"Implementation",
			"Contracts",
			"/Application/Implementation",
			"/Application/Contracts",
			false,
			ImmutableArray.Create(ArchitectureDependencySiteNames.MethodReturn, ArchitectureDependencySiteNames.New),
			ImmutableArray.Create(ArchitectureDependencySiteNames.Field),
			"Application implementations may create and return contracts.");

		var result = ArchitectureGraphSelection.ForDependency(handle);

		result.Kind.Should().Be(ArchitectureGraphSelectionKind.DependencyRule);
		result.Title.Should().Be("AllowedDependency Implementation -> Contracts");
		result.Subtitle.Should().Be(@"D:\repo\Architecture.anl");
		result.DependencyHandle.Should().BeSameAs(handle);
		result.AllowedSites.Should().Equal(ArchitectureDependencySiteNames.MethodReturn, ArchitectureDependencySiteNames.New);
		result.BlockedSites.Should().Equal(ArchitectureDependencySiteNames.Field);
	}

	[Fact]
	public void ForCodeEvidence_PopulatesObservedDependencyDetails()
	{
		var result = ArchitectureGraphSelection.ForCodeEvidence(
			"CustomerController",
			"OrderKitchen",
			"Observed allowed method dependency.",
			"CustomerController called OrderKitchen from a method.");

		result.Kind.Should().Be(ArchitectureGraphSelectionKind.CodeEvidence);
		result.Title.Should().Be("Observed code dependency CustomerController -> OrderKitchen");
		result.Subtitle.Should().Be("Observed allowed method dependency.");
		result.EvidenceDetails.Should().Be("CustomerController called OrderKitchen from a method.");
	}
}
