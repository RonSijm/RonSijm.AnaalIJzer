using System.Collections.Immutable;
using AwesomeAssertions;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.GraphModel.Model;
using Xunit;

namespace RonSijm.AnaalIJzer.Graphing.Tests.Building;

public sealed class ArchitectureGraphViewModelBuilderTests
{
	[Fact]
	public void HighlightCurrent_HighlightsGraphContainingActiveLayer()
	{
		var snapshot = CreateSnapshot();

		var groups = ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.HighlightCurrent);

		groups.Should().HaveCount(4);
		groups[0].Title.Should().Contain("Customer");
		groups[0].IsHighlighted.Should().BeTrue();
		groups[1].IsHighlighted.Should().BeFalse();
		groups[3].Title.Should().Be("Wildcard and global rules");
		groups[3].IsHighlighted.Should().BeTrue();
	}

	[Fact]
	public void HighlightCurrent_BuildsDiagramNodesAndEdges()
	{
		var snapshot = CreateSnapshot();

		var groups = ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.HighlightCurrent);
		var restaurantGraph = groups[0];

		restaurantGraph.Nodes.Select(node => node.Path).Should().Equal("Customer", "Waiter", "Chef");
		restaurantGraph.Nodes.Should().OnlyContain(node => node.X >= 0 && node.Y >= 0);
		restaurantGraph.Edges.Select(edge => edge.From + "->" + edge.To).Should().Equal("Customer->Waiter", "Waiter->Chef");
		restaurantGraph.Edges.Should().OnlyContain(edge => !edge.IsBlocked);
	}

	[Fact]
	public void FilterToCurrent_ReturnsOnlyActiveGroups()
	{
		var snapshot = CreateSnapshot();

		var groups = ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.FilterToCurrent);

		groups.Should().HaveCount(2);
		groups.Should().OnlyContain(group => group.IsActive);
		groups.Select(group => group.Title).Should().Contain("Wildcard and global rules");
	}

	[Fact]
	public void ShowAll_DoesNotHighlightCurrentGraph()
	{
		var snapshot = CreateSnapshot();

		var groups = ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.ShowAll);

		groups.Should().HaveCount(4);
		groups.Should().OnlyContain(group => !group.IsHighlighted);
	}

	[Fact]
	public void Build_PreservesEditableLayerAndRuleMetadata()
	{
		var layers = ImmutableArray.Create(
			new ArchitectureGraphLayer("Customer", "Customer", "Orders food.", 0, 1, true, "C:\\settings\\Architecture.anl", ArchitectureConfigurationSourceKind.XmlFile, 12),
			new ArchitectureGraphLayer("Waiter", "Waiter", "Takes orders.", 0, 2, false, "C:\\settings\\Architecture.anl", ArchitectureConfigurationSourceKind.XmlFile, 13));
		var rules = ImmutableArray.Create(new ArchitectureGraphRule("Customer", "Waiter", string.Empty, "AllowedDependency", "all sites", false, false, true, sourcePath: "C:\\settings\\Architecture.anl", sourceKind: ArchitectureConfigurationSourceKind.XmlFile, xmlLineNumber: 13, description: "Customers ask waiters."));
		var snapshot = new ArchitectureGraphSnapshot(true, false, layers, rules, ["Customer"], ImmutableArray<string>.Empty);

		var group = ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.ShowAll).Single();

		group.Nodes.Single(node => node.Path == "Customer").EditHandle.LayerPath.Should().Be("Customer");
		group.Nodes.Single(node => node.Path == "Customer").EditHandle.XmlLineNumber.Should().Be(12);
		group.Edges.Single().EditHandle.Description.Should().Be("Customers ask waiters.");
		group.Edges.Single().EditHandle.XmlLineNumber.Should().Be(13);
	}

	[Fact]
	public void Build_GroupsNestedLayersIntoBoundaries()
	{
		var layers = ImmutableArray.Create(
			new ArchitectureGraphLayer("Application", "Application", "Application boundary", 0, 1, false),
			new ArchitectureGraphLayer("Application/Contracts", "Contracts", "Public application contracts", 1, 2, true),
			new ArchitectureGraphLayer("Application/Implementation", "Implementation", "Application implementation", 1, 3, false),
			new ArchitectureGraphLayer("Crosscutting", "Crosscutting", null, 0, 4, false));
		var rules = ImmutableArray.Create(new ArchitectureGraphRule("Application/Implementation", "Application/Contracts", "Application", "AllowedDependency", "Inheritance", false, false, true));
		var snapshot = new ArchitectureGraphSnapshot(true, false, layers, rules, ["Application/Contracts"], ImmutableArray<string>.Empty);

		var group = ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.HighlightCurrent).Single(group => group.Nodes.Any(node => node.Path == "Application/Contracts"));
		var boundary = group.Boundaries.Single();

		boundary.Path.Should().Be("Application");
		boundary.IsActive.Should().BeTrue();
		group.Nodes.Select(node => node.Path).Should().NotContain("Application");
		boundary.X.Should().BeLessThan(group.Nodes.Where(node => node.Path.StartsWith("Application", StringComparison.Ordinal)).Min(node => node.X));
		boundary.Y.Should().BeLessThan(group.Nodes.Where(node => node.Path.StartsWith("Application", StringComparison.Ordinal)).Min(node => node.Y));
		boundary.Width.Should().BeGreaterThan(170);
		boundary.Height.Should().BeGreaterThan(72);
	}

	[Fact]
	public void Build_UsesParentBoundaryAsConnectionEndpointWithoutDuplicateNode()
	{
		var layers = ImmutableArray.Create(
			new ArchitectureGraphLayer("Application", "Application", "Application boundary", 0, 1, false),
			new ArchitectureGraphLayer("Application/Contracts", "Contracts", "Public application contracts", 1, 2, true),
			new ArchitectureGraphLayer("Crosscutting", "Crosscutting", null, 0, 3, false));
		var rules = ImmutableArray.Create(new ArchitectureGraphRule("Application", "Crosscutting", string.Empty, "AllowedDependency", "all sites", false, false, true));
		var snapshot = new ArchitectureGraphSnapshot(true, false, layers, rules, ["Application/Contracts"], ImmutableArray<string>.Empty);

		var group = ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.HighlightCurrent).Single();

		group.Boundaries.Select(boundary => boundary.Path).Should().Contain("Application");
		group.Nodes.Select(node => node.Path).Should().NotContain("Application");
		group.Nodes.Select(node => node.Path).Should().Contain("Application/Contracts");
		group.Nodes.Select(node => node.Path).Should().Contain("Crosscutting");
		group.Edges.Select(edge => edge.From + "->" + edge.To).Should().Contain("Application->Crosscutting");
	}

	[Fact]
	public void Build_LaysOutNestedBoundariesWithoutOverlapAndKeepsDependenciesLeftToRight()
	{
		var layers = ImmutableArray.Create(
			new ArchitectureGraphLayer("Ordering", "Ordering", null, 0, 1, false),
			new ArchitectureGraphLayer("Ordering/Contracts", "Contracts", null, 1, 2, false),
			new ArchitectureGraphLayer("Ordering/Implementation", "Implementation", null, 1, 3, true),
			new ArchitectureGraphLayer("Billing", "Billing", null, 0, 4, false),
			new ArchitectureGraphLayer("Billing/Contracts", "Contracts", null, 1, 5, false),
			new ArchitectureGraphLayer("Billing/Implementation", "Implementation", null, 1, 6, false),
			new ArchitectureGraphLayer("Framework", "Framework", null, 0, 7, false));
		var rules = ImmutableArray.Create(
			new ArchitectureGraphRule("Ordering/Implementation", "Ordering/Contracts", "Ordering", "AllowedDependency", "Inheritance", false, false, true),
			new ArchitectureGraphRule("Billing/Implementation", "Billing/Contracts", "Billing", "AllowedDependency", "Inheritance", false, false, false),
			new ArchitectureGraphRule("Ordering/Implementation", "Billing/Contracts", string.Empty, "AllowedDependency", "Constructor", false, false, false),
			new ArchitectureGraphRule("Billing/Implementation", "Framework", string.Empty, "AllowedDependency", "Constructor", false, false, false));
		var snapshot = new ArchitectureGraphSnapshot(true, false, layers, rules, ["Ordering/Implementation"], ImmutableArray<string>.Empty);

		var group = ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.ShowAll).Single(group => group.Nodes.Any(node => node.Path == "Ordering/Implementation"));
		var ordering = group.Boundaries.Single(boundary => boundary.Path == "Ordering");
		var billing = group.Boundaries.Single(boundary => boundary.Path == "Billing");

		Overlaps(ordering, billing).Should().BeFalse();
		group.Nodes.Select(node => node.Path).Should().NotContain("Ordering");
		group.Nodes.Select(node => node.Path).Should().NotContain("Billing");
		group.Nodes.Single(node => node.Path == "Ordering/Implementation").X.Should().BeLessThan(group.Nodes.Single(node => node.Path == "Ordering/Contracts").X);
		group.Nodes.Single(node => node.Path == "Billing/Implementation").X.Should().BeLessThan(group.Nodes.Single(node => node.Path == "Billing/Contracts").X);
		AssertBoundaryContainsNodes(ordering, group.Nodes.Where(node => node.Path == "Ordering" || node.Path.StartsWith("Ordering/", StringComparison.Ordinal)));
		AssertBoundaryContainsNodes(billing, group.Nodes.Where(node => node.Path == "Billing" || node.Path.StartsWith("Billing/", StringComparison.Ordinal)));
	}

	[Fact]
	public void Build_PrefersHorizontalDependencyFlowForNestedAndBranchedGraphs()
	{
		var layers = ImmutableArray.Create(
			new ArchitectureGraphLayer("Controller", "Controller", null, 0, 1, false),
			new ArchitectureGraphLayer("Application", "Application", null, 0, 2, true),
			new ArchitectureGraphLayer("Application/Implementation", "ApplicationImplementation", null, 1, 3, true),
			new ArchitectureGraphLayer("Application/Interfaces", "ApplicationInterfaces", null, 1, 4, false),
			new ArchitectureGraphLayer("ServiceAgent", "ServiceAgent", null, 0, 5, false),
			new ArchitectureGraphLayer("Auth", "Auth", null, 0, 6, false),
			new ArchitectureGraphLayer("Ports", "Ports", null, 0, 7, false),
			new ArchitectureGraphLayer("Ports/Interfaces", "PortInterfaces", null, 1, 8, false),
			new ArchitectureGraphLayer("Ports/Implementation", "PortImplementation", null, 1, 9, false),
			new ArchitectureGraphLayer("DatabaseFactory", "DatabaseFactory", null, 0, 10, false),
			new ArchitectureGraphLayer("Database", "Database", null, 0, 11, false),
			new ArchitectureGraphLayer("DatabaseConnections", "DatabaseConnections", null, 0, 12, false));
		var rules = ImmutableArray.Create(
			new ArchitectureGraphRule("Controller", "Application", string.Empty, "AllowedDependency", "all sites", false, false, true),
			new ArchitectureGraphRule("Application", "ServiceAgent", string.Empty, "AllowedDependency", "all sites", false, false, true),
			new ArchitectureGraphRule("Application", "Ports", string.Empty, "AllowedDependency", "all sites", false, false, true),
			new ArchitectureGraphRule("Application/Implementation", "Application/Interfaces", "Application", "AllowedDependency", "InterfaceImplementation", false, false, true),
			new ArchitectureGraphRule("Auth", "Ports", string.Empty, "AllowedDependency", "all sites", false, false, false),
			new ArchitectureGraphRule("Ports/Interfaces", "Ports/Implementation", "Ports", "AllowedDependency", "InterfaceImplementation", false, false, false),
			new ArchitectureGraphRule("Ports", "DatabaseFactory", string.Empty, "AllowedDependency", "all sites", false, false, false),
			new ArchitectureGraphRule("Ports", "Database", string.Empty, "AllowedDependency", "all sites", false, false, false),
			new ArchitectureGraphRule("Ports", "DatabaseConnections", string.Empty, "AllowedDependency", "all sites", false, false, false),
			new ArchitectureGraphRule("DatabaseFactory", "Database", string.Empty, "AllowedDependency", "all sites", false, false, false),
			new ArchitectureGraphRule("DatabaseFactory", "DatabaseConnections", string.Empty, "AllowedDependency", "all sites", false, false, false));
		var snapshot = new ArchitectureGraphSnapshot(true, false, layers, rules, ["Application", "Application/Implementation"], ImmutableArray<string>.Empty);

		var group = ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.ShowAll).Single(group => group.Nodes.Any(node => node.Path == "Controller"));
		var controller = group.Nodes.Single(node => node.Path == "Controller");
		var application = group.Boundaries.Single(boundary => boundary.Path == "Application");
		var auth = group.Nodes.Single(node => node.Path == "Auth");
		var serviceAgent = group.Nodes.Single(node => node.Path == "ServiceAgent");
		var ports = group.Boundaries.Single(boundary => boundary.Path == "Ports");
		var databaseFactory = group.Nodes.Single(node => node.Path == "DatabaseFactory");
		var database = group.Nodes.Single(node => node.Path == "Database");
		var laneCount = group.Nodes.Select(node => node.Y)
			.Concat(group.Boundaries.Select(boundary => boundary.Y))
			.Distinct()
			.Count();

		controller.X.Should().BeLessThan(application.X);
		Right(application).Should().BeLessThan(ports.X);
		auth.X.Should().BeGreaterThan(application.X);
		auth.X.Should().BeLessThan(ports.X);
		Right(ports).Should().BeLessThan(databaseFactory.X);
		databaseFactory.X.Should().BeLessThan(database.X);
		Overlaps(application, ports).Should().BeFalse();
		laneCount.Should().BeGreaterThan(1);
		auth.Y.Should().NotBe(application.Y);
		serviceAgent.Y.Should().NotBe(ports.Y);
	}

	[Fact]
	public void Build_WhenEvidenceEnabled_ShowsTypeCountsAndViolationEdges()
	{
		var layers = ImmutableArray.Create(
			new ArchitectureGraphLayer("Customer", "Customer", null, 0, 1, true),
			new ArchitectureGraphLayer("Chef", "Chef", null, 0, 2, false));
		var evidence = new ArchitectureGraphEvidence(
			[
				new ArchitectureGraphTypeEvidence("Customer", "CustomerType", "CustomerType", "CustomerType.cs", 1),
				new ArchitectureGraphTypeEvidence("Chef", "ChefType", "ChefType", "ChefType.cs", 1)
			],
			[
				new ArchitectureGraphDependencyEvidence(
					"Customer",
					"Chef",
					"CustomerType",
					"ChefType",
					"Constructor",
					"MissingAllowedDependency",
					"ARCH001",
					"no allowed dependency is configured",
					"CustomerType.cs",
					3),
				new ArchitectureGraphDependencyEvidence(
					"Customer",
					"Chef",
					"CustomerType",
					"ChefType",
					"Method",
					"Allowed",
					null,
					"allowed by configured dependency rules",
					"CustomerType.cs",
					4)
			]);
		var snapshot = new ArchitectureGraphSnapshot(
			true,
			false,
			layers,
			ImmutableArray<ArchitectureGraphRule>.Empty,
			["Customer"],
			ImmutableArray<string>.Empty,
			evidence: evidence);

		var group = ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.ShowAll, includeEvidence: true).Should().ContainSingle().Subject;

		group.Nodes.Single(node => node.Path == "Customer").TypeCount.Should().Be(1);
		group.Nodes.Single(node => node.Path == "Customer").OutgoingViolationCount.Should().Be(1);
		var evidenceEdge = group.Edges.Should().ContainSingle(edge => edge.IsEvidence).Subject;
		evidenceEdge.From.Should().Be("Customer");
		evidenceEdge.To.Should().Be("Chef");
		evidenceEdge.ViolationCount.Should().Be(1);
		evidenceEdge.ObservedUsageCount.Should().Be(2);
	}

	[Fact]
	public void Build_WhenEvidenceContainsTransitiveExposure_IncludesPathAndDepthInEdgeDescription()
	{
		var layers = ImmutableArray.Create(
			new ArchitectureGraphLayer("Application", "Application", null, 0, 1, true),
			new ArchitectureGraphLayer("QuerySurface", "QuerySurface", null, 0, 2, false));
		var evidence = new ArchitectureGraphEvidence(
			ImmutableArray<ArchitectureGraphTypeEvidence>.Empty,
			[
				new ArchitectureGraphDependencyEvidence(
					"Application",
					"QuerySurface",
					"CandyService",
					"LollyQueryable",
					"Property",
					"TypePolicyViolation",
					"ARCH014",
					"public contracts may not reveal query surfaces",
					"CandyService.cs",
					12,
					"CandyService.OrderRaw -> CandyReceipt.RawQuery -> LollyQueryable",
					1)
			]);
		var snapshot = new ArchitectureGraphSnapshot(
			true,
			false,
			layers,
			ImmutableArray<ArchitectureGraphRule>.Empty,
			["Application"],
			ImmutableArray<string>.Empty,
			evidence: evidence);

		var group = ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.ShowAll, includeEvidence: true).Should().ContainSingle().Subject;
		var evidenceEdge = group.Edges.Should().ContainSingle(edge => edge.IsEvidence).Subject;

		evidenceEdge.Description.Should().Contain("via CandyService.OrderRaw -> CandyReceipt.RawQuery -> LollyQueryable (depth 1)");
	}

	[Fact]
	public void Build_WhenEvidenceDisabled_KeepsEvidenceOutOfGraph()
	{
		var layers = ImmutableArray.Create(
			new ArchitectureGraphLayer("Customer", "Customer", null, 0, 1, true),
			new ArchitectureGraphLayer("Chef", "Chef", null, 0, 2, false));
		var evidence = new ArchitectureGraphEvidence(
			ImmutableArray<ArchitectureGraphTypeEvidence>.Empty,
			[
				new ArchitectureGraphDependencyEvidence(
				"Customer",
				"Chef",
				"CustomerType",
				"ChefType",
				"Constructor",
				"MissingAllowedDependency",
				"ARCH001",
				"no allowed dependency is configured",
				"CustomerType.cs",
				3)
			]);
		var snapshot = new ArchitectureGraphSnapshot(
			true,
			false,
			layers,
			ImmutableArray<ArchitectureGraphRule>.Empty,
			["Customer"],
			ImmutableArray<string>.Empty,
			evidence: evidence);

		var groups = ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.ShowAll);

		groups.Should().HaveCount(2);
		groups.Should().OnlyContain(group => group.Edges.All(edge => !edge.IsEvidence));
	}

	[Fact]
	public void Build_PropagatesExceptionReviewCountsAndSummariesToNodesAndBoundaries()
	{
		var layers = ImmutableArray.Create(
			new ArchitectureGraphLayer("Application", "Application", null, 0, 1, false),
			new ArchitectureGraphLayer("Application/Contracts", "Contracts", null, 1, 2, true),
			new ArchitectureGraphLayer("Application/Implementation", "Implementation", null, 1, 3, false));
		var exceptionReviews = ImmutableArray.Create(
			new ArchitectureGraphExceptionReview("Application/Contracts", "Class", "typeName=\"OrderContract\"", "Invalid", "Missing owner", null, null, null, "Architecture.anl", 10, 1),
			new ArchitectureGraphExceptionReview("Application/Implementation", "Class", "typeName=\"OrderService\"", "Stale", "No longer matches code", null, "Team", "2026-08-30", "Architecture.anl", 14, 1));
		var snapshot = new ArchitectureGraphSnapshot(
			true,
			false,
			layers,
			ImmutableArray<ArchitectureGraphRule>.Empty,
			["Application/Contracts"],
			ImmutableArray<string>.Empty,
			exceptionReviews: exceptionReviews);

		var group = ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.ShowAll).Should().ContainSingle().Subject;
		var boundary = group.Boundaries.Should().ContainSingle().Subject;
		var contracts = group.Nodes.Should().ContainSingle(node => node.Path == "Application/Contracts").Subject;
		var implementation = group.Nodes.Should().ContainSingle(node => node.Path == "Application/Implementation").Subject;

		boundary.ExceptionReviewCount.Should().Be(2);
		boundary.ExceptionReviewSummaries.Should().Contain("[Invalid] Class typeName=\"OrderContract\"");
		boundary.ExceptionReviewSummaries.Should().Contain("[Stale] Class typeName=\"OrderService\"");
		contracts.ExceptionReviewCount.Should().Be(1);
		contracts.ExceptionReviewSummaries.Should().ContainSingle().Which.Should().Be("[Invalid] Class typeName=\"OrderContract\"");
		implementation.ExceptionReviewCount.Should().Be(1);
		implementation.ExceptionReviewSummaries.Should().ContainSingle().Which.Should().Be("[Stale] Class typeName=\"OrderService\"");
	}

	private static ArchitectureGraphSnapshot CreateSnapshot()
	{
		var layers = ImmutableArray.Create(
			new ArchitectureGraphLayer("Customer", "Customer", null, 0, 1, false),
			new ArchitectureGraphLayer("Waiter", "Waiter", null, 0, 2, true),
			new ArchitectureGraphLayer("Chef", "Chef", null, 0, 3, false),
			new ArchitectureGraphLayer("Pantry", "Pantry", null, 0, 4, false),
			new ArchitectureGraphLayer("Framework", "Framework", null, 0, 5, false));
		var rules = ImmutableArray.Create(
			new ArchitectureGraphRule("Customer", "Waiter", string.Empty, "AllowedDependency", "all sites", false, false, true),
			new ArchitectureGraphRule("Waiter", "Chef", string.Empty, "AllowedDependency", "all sites", false, false, true),
			new ArchitectureGraphRule("*", "Framework", string.Empty, "AllowedDependency", "all sites", true, true, true));

		var result = new ArchitectureGraphSnapshot(
			true,
			false,
			layers,
			rules,
			["Waiter"],
			ImmutableArray<string>.Empty);

		return result;
	}

	private static void AssertBoundaryContainsNodes(ArchitectureGraphBoundaryViewModel boundary, IEnumerable<ArchitectureGraphNodeViewModel> nodes)
	{
		foreach (var node in nodes)
		{
			node.X.Should().BeGreaterThan(boundary.X);
			node.Y.Should().BeGreaterThan(boundary.Y);
			(node.X + 170).Should().BeLessThan(boundary.X + boundary.Width);
			(node.Y + 72).Should().BeLessThan(boundary.Y + boundary.Height);
		}
	}

	private static bool Overlaps(ArchitectureGraphBoundaryViewModel first, ArchitectureGraphBoundaryViewModel second)
	{
		var result = first.X < second.X + second.Width
		             && first.X + first.Width > second.X
		             && first.Y < second.Y + second.Height
		             && first.Y + first.Height > second.Y;

		return result;
	}

	private static double Right(ArchitectureGraphBoundaryViewModel boundary)
	{
		var result = boundary.X + boundary.Width;

		return result;
	}
}
