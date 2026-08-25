using System.Collections.Immutable;
using AwesomeAssertions;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.GraphModel.Building;
using RonSijm.AnaalIJzer.Graphing.Model;
using Xunit;

namespace RonSijm.AnaalIJzer.GraphModel.Tests.Building;

public sealed class ArchitectureGraphSnapshotFactoryTests
{
	[Fact]
	public void CreateNoConfigurationSnapshot_PreservesCreationTargets()
	{
		var creationTargets = ImmutableArray.Create(
			new ArchitectureConfigurationCreationTarget(
				"Example project",
				"Create a configuration beside the project.",
				new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, @"D:\src\Example\Architecture.anl"),
				ArchitectureConfigurationRegistrationKind.ProjectFile,
				@"D:\src\Example\Example.csproj"));

		var result = ArchitectureGraphSnapshotFactory.CreateNoConfigurationSnapshot(creationTargets);

		result.HasConfiguration.Should().BeFalse();
		result.Layers.Should().BeEmpty();
		result.Rules.Should().BeEmpty();
		result.ConfigurationCreationTargets.Should().BeEquivalentTo(creationTargets);
		result.Evidence.Should().BeSameAs(ArchitectureGraphEvidence.Empty);
	}

	[Fact]
	public void AttachEvidence_PreservesConfigurationSnapshotAndReplacesEvidence()
	{
		var configSnapshot = new ArchitectureGraphSnapshot(
			hasConfiguration: true,
			hasConfigurationIssues: true,
			layers: ImmutableArray.Create(new ArchitectureGraphLayer("Application", "Application", "Runs the kitchen", 0, 2, true, @"D:\Architecture.anl", ArchitectureConfigurationSourceKind.XmlFile, 4)),
			rules: ImmutableArray.Create(new ArchitectureGraphRule("Controller", "Application", "", "AllowedDependency", "all sites", false, false, true, "Controller", "Application", @"D:\Architecture.anl", ArchitectureConfigurationSourceKind.XmlFile, 8, 3, ImmutableArray<string>.Empty, ImmutableArray<string>.Empty, "Waiters may call the kitchen")),
			activeLayerPaths: ["Application"],
			configurationIssueMessages: ["Broken on purpose"],
			configurationSource: new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, @"D:\Architecture.anl"),
			evidence: ArchitectureGraphEvidence.Empty,
			configurationCreationTargets: ImmutableArray.Create(new ArchitectureConfigurationCreationTarget(
				"Example project",
				"Create a configuration beside the project.",
				new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, @"D:\src\Example\Architecture.anl"),
				ArchitectureConfigurationRegistrationKind.ProjectFile,
				@"D:\src\Example\Example.csproj")),
			exceptionReviews: ImmutableArray.Create(new ArchitectureGraphExceptionReview("Application", "Class", "typeName=\"Example\"", "Valid", "Looks good", null, null, null, @"D:\Architecture.anl", 12, 5)));
		var evidence = new ArchitectureGraphEvidence(
			types: ImmutableArray.Create(new ArchitectureGraphTypeEvidence("Application", "PizzaKitchen", "Example.PizzaKitchen", @"D:\src\Example\PizzaKitchen.cs", 14)),
			dependencies: ImmutableArray.Create(new ArchitectureGraphDependencyEvidence("Controller", "Application", "PizzaController", "PizzaKitchen", "Constructor", "Allowed", null, "Allowed edge", @"D:\src\Example\PizzaController.cs", 7)));
		var exceptionReviews = ImmutableArray.Create(new ArchitectureGraphExceptionReview("Controller", "Class", "typeName=\"PizzaController\"", "Review", "Check this", "Temporary", "Team", "2026-08-31", @"D:\Architecture.anl", 18, 2));

		var result = ArchitectureGraphSnapshotFactory.AttachEvidence(configSnapshot, evidence, exceptionReviews);

		result.HasConfiguration.Should().BeTrue();
		result.HasConfigurationIssues.Should().BeTrue();
		result.Layers.Should().BeEquivalentTo(configSnapshot.Layers);
		result.Rules.Should().BeEquivalentTo(configSnapshot.Rules);
		result.ActiveLayerPaths.Should().BeEquivalentTo(configSnapshot.ActiveLayerPaths);
		result.ConfigurationIssueMessages.Should().BeEquivalentTo(configSnapshot.ConfigurationIssueMessages);
		result.ConfigurationSource.Should().Be(configSnapshot.ConfigurationSource);
		result.ConfigurationCreationTargets.Should().BeEquivalentTo(configSnapshot.ConfigurationCreationTargets);
		result.Evidence.Should().BeSameAs(evidence);
		result.ExceptionReviews.Should().BeEquivalentTo(exceptionReviews);
	}

	[Fact]
	public void CreateSnapshot_ProjectsInputsIntoGraphContracts()
	{
		var configurationSource = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, @"D:\src\Example\Properties\AnaalIJzerSettings.cs");
		var input = new ArchitectureGraphSnapshotInput(
			hasConfiguration: true,
			hasConfigurationIssues: false,
			layers: ImmutableArray.Create(
				new ArchitectureGraphLayerInput("Restaurant", "Restaurant", "Shared restaurant rules", 0, 1, true, @"D:\src\Example\Architecture.anl", ArchitectureConfigurationSourceKind.XmlFile, 2),
				new ArchitectureGraphLayerInput("Restaurant/Waiter", "Waiter", "Talks to customers", 1, 2, true, @"D:\src\Example\Architecture.anl", ArchitectureConfigurationSourceKind.XmlFile, 6)),
			rules: ImmutableArray.Create(
				new ArchitectureGraphRuleInput(
					"/Restaurant/Waiter",
					"/Restaurant/Chef",
					"Restaurant",
					"AllowedDependency",
					"Constructor, Method",
					appliesToDescendants: true,
					isWildcard: false,
					isActive: true,
					configuredFrom: "Waiter",
					configuredTo: "Chef",
					sourcePath: @"D:\src\Example\Architecture.anl",
					sourceKind: ArchitectureConfigurationSourceKind.XmlFile,
					xmlLineNumber: 12,
					xmlLinePosition: 4,
					allowedSites: ["Constructor", "Method"],
					blockedSites: ["Field"],
					description: "Waiters may ask chefs to prepare food")),
			activeLayerPaths: ["Restaurant/Waiter"],
			configurationIssueMessages: ["Nothing to see here"],
			configurationSource);
		var evidence = new ArchitectureGraphEvidence(
			types: ImmutableArray.Create(new ArchitectureGraphTypeEvidence("Restaurant/Waiter", "PizzaWaiter", "Example.PizzaWaiter", @"D:\src\Example\PizzaWaiter.cs", 10)),
			dependencies: ImmutableArray.Create(new ArchitectureGraphDependencyEvidence("Restaurant/Waiter", "Restaurant/Chef", "PizzaWaiter", "PizzaChef", "Method", "Allowed", null, "Allowed by configured edge", @"D:\src\Example\PizzaWaiter.cs", 19)));
		var exceptionReviews = ImmutableArray.Create(new ArchitectureGraphExceptionReview("Restaurant", "Class", "typeName=\"TemporaryKitchen\"", "ExpiringSoon", "Document this soon", "Temporary", "Kitchen team", "2026-08-15", @"D:\src\Example\Architecture.anl", 20, 2));

		var result = ArchitectureGraphSnapshotFactory.CreateSnapshot(input, evidence, exceptionReviews);

		result.HasConfiguration.Should().BeTrue();
		result.ConfigurationSource.Should().Be(configurationSource);
		result.Layers.Should().HaveCount(2);
		result.Layers[1].Path.Should().Be("Restaurant/Waiter");
		result.Layers[1].Description.Should().Be("Talks to customers");
		result.Rules.Should().ContainSingle();
		result.Rules[0].From.Should().Be("/Restaurant/Waiter");
		result.Rules[0].To.Should().Be("/Restaurant/Chef");
		result.Rules[0].AllowedSites.Should().BeEquivalentTo(["Constructor", "Method"]);
		result.Rules[0].BlockedSites.Should().BeEquivalentTo(["Field"]);
		result.Rules[0].Description.Should().Be("Waiters may ask chefs to prepare food");
		result.ActiveLayerPaths.Should().BeEquivalentTo(["Restaurant/Waiter"]);
		result.Evidence.Should().BeSameAs(evidence);
		result.ExceptionReviews.Should().BeEquivalentTo(exceptionReviews);
	}
}
