using System.Collections.Immutable;
using AwesomeAssertions;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
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
		AssertionExtensions.Should((string)groups[0].Title).Contain("Customer");
		AssertionExtensions.Should((bool)groups[0].IsHighlighted).BeTrue();
		AssertionExtensions.Should((bool)groups[1].IsHighlighted).BeFalse();
		AssertionExtensions.Should((string)groups[3].Title).Be("Wildcard and global rules");
		AssertionExtensions.Should((bool)groups[3].IsHighlighted).BeTrue();
	}

	[Fact]
	public void HighlightCurrent_BuildsDiagramNodesAndEdges()
	{
		var snapshot = CreateSnapshot();

		var groups = ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.HighlightCurrent);
		var restaurantGraph = groups[0];

		ImmutableArrayExtensions.Select(restaurantGraph.Nodes, node => node.Path).Should().Equal("Customer", "Waiter", "Chef");
		restaurantGraph.Nodes.Should().OnlyContain(node => node.X >= 0 && node.Y >= 0);
		ImmutableArrayExtensions.Select(restaurantGraph.Edges, edge => edge.From + "->" + edge.To).Should().Equal("Customer->Waiter", "Waiter->Chef");
		restaurantGraph.Edges.Should().OnlyContain(edge => !edge.IsBlocked);
	}

	[Fact]
	public void FilterToCurrent_ReturnsOnlyActiveGroups()
	{
		var snapshot = CreateSnapshot();

		var groups = ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.FilterToCurrent);

		groups.Should().HaveCount(2);
		groups.Should().OnlyContain(group => group.IsActive);
		ImmutableArrayExtensions.Select(groups, group => group.Title).Should().Contain("Wildcard and global rules");
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
		var snapshot = new ArchitectureGraphSnapshot(true, false, layers, rules, ImmutableArray.Create("Customer"), ImmutableArray<string>.Empty);

		var group = ImmutableArrayExtensions.Single(ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.ShowAll));

		AssertionExtensions.Should((string)ImmutableArrayExtensions.Single(group.Nodes, node => node.Path == "Customer").EditHandle.LayerPath).Be("Customer");
		AssertionExtensions.Should((int)ImmutableArrayExtensions.Single(group.Nodes, node => node.Path == "Customer").EditHandle.XmlLineNumber).Be(12);
		ImmutableArrayExtensions.Single(group.Edges).EditHandle.Description.Should().Be("Customers ask waiters.");
		AssertionExtensions.Should((int)ImmutableArrayExtensions.Single(group.Edges).EditHandle.XmlLineNumber).Be(13);
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
		var snapshot = new ArchitectureGraphSnapshot(true, false, layers, rules, ImmutableArray.Create("Application/Contracts"), ImmutableArray<string>.Empty);

		var group = ImmutableArrayExtensions.Single(ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.HighlightCurrent), group => ImmutableArrayExtensions.Any(group.Nodes, node => node.Path == "Application/Contracts"));
		var boundary = ImmutableArrayExtensions.Single(group.Boundaries);

		AssertionExtensions.Should((string)boundary.Path).Be("Application");
		AssertionExtensions.Should((bool)boundary.IsActive).BeTrue();
		ImmutableArrayExtensions.Select(group.Nodes, node => node.Path).Should().NotContain("Application");
		AssertionExtensions.Should((double)boundary.X).BeLessThan(ImmutableArrayExtensions.Where(group.Nodes, node => node.Path.StartsWith("Application", StringComparison.Ordinal)).Min(node => node.X));
		AssertionExtensions.Should((double)boundary.Y).BeLessThan(ImmutableArrayExtensions.Where(group.Nodes, node => node.Path.StartsWith("Application", StringComparison.Ordinal)).Min(node => node.Y));
		AssertionExtensions.Should((double)boundary.Width).BeGreaterThan(170);
		AssertionExtensions.Should((double)boundary.Height).BeGreaterThan(72);
	}

	[Fact]
	public void Build_UsesParentBoundaryAsConnectionEndpointWithoutDuplicateNode()
	{
		var layers = ImmutableArray.Create(
			new ArchitectureGraphLayer("Application", "Application", "Application boundary", 0, 1, false),
			new ArchitectureGraphLayer("Application/Contracts", "Contracts", "Public application contracts", 1, 2, true),
			new ArchitectureGraphLayer("Crosscutting", "Crosscutting", null, 0, 3, false));
		var rules = ImmutableArray.Create(new ArchitectureGraphRule("Application", "Crosscutting", string.Empty, "AllowedDependency", "all sites", false, false, true));
		var snapshot = new ArchitectureGraphSnapshot(true, false, layers, rules, ImmutableArray.Create("Application/Contracts"), ImmutableArray<string>.Empty);

		var group = ImmutableArrayExtensions.Single(ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.HighlightCurrent));

		ImmutableArrayExtensions.Select(group.Boundaries, boundary => boundary.Path).Should().Contain("Application");
		ImmutableArrayExtensions.Select(group.Nodes, node => node.Path).Should().NotContain("Application");
		ImmutableArrayExtensions.Select(group.Nodes, node => node.Path).Should().Contain("Application/Contracts");
		ImmutableArrayExtensions.Select(group.Nodes, node => node.Path).Should().Contain("Crosscutting");
		ImmutableArrayExtensions.Select(group.Edges, edge => edge.From + "->" + edge.To).Should().Contain("Application->Crosscutting");
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
		var snapshot = new ArchitectureGraphSnapshot(true, false, layers, rules, ImmutableArray.Create("Ordering/Implementation"), ImmutableArray<string>.Empty);

		var group = ImmutableArrayExtensions.Single(ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.ShowAll), group => ImmutableArrayExtensions.Any(group.Nodes, node => node.Path == "Ordering/Implementation"));
		var ordering = ImmutableArrayExtensions.Single(group.Boundaries, boundary => boundary.Path == "Ordering");
		var billing = ImmutableArrayExtensions.Single(group.Boundaries, boundary => boundary.Path == "Billing");

		Overlaps(ordering, billing).Should().BeFalse();
		ImmutableArrayExtensions.Select(group.Nodes, node => node.Path).Should().NotContain("Ordering");
		ImmutableArrayExtensions.Select(group.Nodes, node => node.Path).Should().NotContain("Billing");
		AssertionExtensions.Should((double)ImmutableArrayExtensions.Single(group.Nodes, node => node.Path == "Ordering/Implementation").X).BeLessThan(ImmutableArrayExtensions.Single(group.Nodes, node => node.Path == "Ordering/Contracts").X);
		AssertionExtensions.Should((double)ImmutableArrayExtensions.Single(group.Nodes, node => node.Path == "Billing/Implementation").X).BeLessThan(ImmutableArrayExtensions.Single(group.Nodes, node => node.Path == "Billing/Contracts").X);
		AssertBoundaryContainsNodes(ordering, ImmutableArrayExtensions.Where(group.Nodes, node => node.Path == "Ordering" || node.Path.StartsWith("Ordering/", StringComparison.Ordinal)));
		AssertBoundaryContainsNodes(billing, ImmutableArrayExtensions.Where(group.Nodes, node => node.Path == "Billing" || node.Path.StartsWith("Billing/", StringComparison.Ordinal)));
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
		var snapshot = new ArchitectureGraphSnapshot(true, false, layers, rules, ImmutableArray.Create("Application", "Application/Implementation"), ImmutableArray<string>.Empty);

		var group = ImmutableArrayExtensions.Single(ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.ShowAll), group => ImmutableArrayExtensions.Any(group.Nodes, node => node.Path == "Controller"));
		var controller = ImmutableArrayExtensions.Single(group.Nodes, node => node.Path == "Controller");
		var application = ImmutableArrayExtensions.Single(group.Boundaries, boundary => boundary.Path == "Application");
		var auth = ImmutableArrayExtensions.Single(group.Nodes, node => node.Path == "Auth");
		var serviceAgent = ImmutableArrayExtensions.Single(group.Nodes, node => node.Path == "ServiceAgent");
		var ports = ImmutableArrayExtensions.Single(group.Boundaries, boundary => boundary.Path == "Ports");
		var databaseFactory = ImmutableArrayExtensions.Single(group.Nodes, node => node.Path == "DatabaseFactory");
		var database = ImmutableArrayExtensions.Single(group.Nodes, node => node.Path == "Database");
		var laneCount = ImmutableArrayExtensions.Select(group.Nodes, node => node.Y)
			.Concat<double>(ImmutableArrayExtensions.Select(group.Boundaries, boundary => boundary.Y))
			.Distinct()
			.Count();

		AssertionExtensions.Should((double)controller.X).BeLessThan(application.X);
		Right(application).Should().BeLessThan(ports.X);
		AssertionExtensions.Should((double)auth.X).BeGreaterThan(application.X);
		AssertionExtensions.Should((double)auth.X).BeLessThan(ports.X);
		Right(ports).Should().BeLessThan(databaseFactory.X);
		AssertionExtensions.Should((double)databaseFactory.X).BeLessThan(database.X);
		Overlaps(application, ports).Should().BeFalse();
		laneCount.Should().BeGreaterThan(1);
		AssertionExtensions.Should((double)auth.Y).NotBe(application.Y);
		AssertionExtensions.Should((double)serviceAgent.Y).NotBe(ports.Y);
	}

	[Fact]
	public void Build_WhenEvidenceEnabled_ShowsTypeCountsAndViolationEdges()
	{
		var layers = ImmutableArray.Create(
			new ArchitectureGraphLayer("Customer", "Customer", null, 0, 1, true),
			new ArchitectureGraphLayer("Chef", "Chef", null, 0, 2, false));
		var evidence = new ArchitectureGraphEvidence(
			ImmutableArray.Create(
				new ArchitectureGraphTypeEvidence("Customer", "CustomerType", "CustomerType", "CustomerType.cs", 1),
				new ArchitectureGraphTypeEvidence("Chef", "ChefType", "ChefType", "ChefType.cs", 1)),
			ImmutableArray.Create(
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
					4)));
		var snapshot = new ArchitectureGraphSnapshot(
			true,
			false,
			layers,
			ImmutableArray<ArchitectureGraphRule>.Empty,
			ImmutableArray.Create("Customer"),
			ImmutableArray<string>.Empty,
			evidence: evidence);

		var group = ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.ShowAll, includeEvidence: true).Should().ContainSingle().Subject;

		ImmutableArrayExtensions.Single(group.Nodes, node => node.Path == "Customer").TypeCount.Should().Be(1);
		ImmutableArrayExtensions.Single(group.Nodes, node => node.Path == "Customer").OutgoingViolationCount.Should().Be(1);
		var evidenceEdge = group.Edges.Should().ContainSingle(edge => edge.IsEvidence).Subject;
		evidenceEdge.From.Should().Be("Customer");
		evidenceEdge.To.Should().Be("Chef");
		evidenceEdge.ViolationCount.Should().Be(1);
		evidenceEdge.ObservedUsageCount.Should().Be(2);
	}

	[Fact]
	public void Build_WhenEvidenceDisabled_KeepsEvidenceOutOfGraph()
	{
		var layers = ImmutableArray.Create(
			new ArchitectureGraphLayer("Customer", "Customer", null, 0, 1, true),
			new ArchitectureGraphLayer("Chef", "Chef", null, 0, 2, false));
		var evidence = new ArchitectureGraphEvidence(
			ImmutableArray<ArchitectureGraphTypeEvidence>.Empty,
			ImmutableArray.Create(new ArchitectureGraphDependencyEvidence(
				"Customer",
				"Chef",
				"CustomerType",
				"ChefType",
				"Constructor",
				"MissingAllowedDependency",
				"ARCH001",
				"no allowed dependency is configured",
				"CustomerType.cs",
				3)));
		var snapshot = new ArchitectureGraphSnapshot(
			true,
			false,
			layers,
			ImmutableArray<ArchitectureGraphRule>.Empty,
			ImmutableArray.Create("Customer"),
			ImmutableArray<string>.Empty,
			evidence: evidence);

		var groups = ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.ShowAll);

		groups.Should().HaveCount(2);
		groups.Should().OnlyContain(group => group.Edges.All(edge => !edge.IsEvidence));
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
			ImmutableArray.Create("Waiter"),
			ImmutableArray<string>.Empty);

		return result;
	}

	private static void AssertBoundaryContainsNodes(ArchitectureGraphBoundaryViewModel boundary, IEnumerable<ArchitectureGraphNodeViewModel> nodes)
	{
		foreach (var node in nodes)
		{
			AssertionExtensions.Should((double)node.X).BeGreaterThan(boundary.X);
			AssertionExtensions.Should((double)node.Y).BeGreaterThan(boundary.Y);
			AssertionExtensions.Should((double)(node.X + 170)).BeLessThan(boundary.X + boundary.Width);
			AssertionExtensions.Should((double)(node.Y + 72)).BeLessThan(boundary.Y + boundary.Height);
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
