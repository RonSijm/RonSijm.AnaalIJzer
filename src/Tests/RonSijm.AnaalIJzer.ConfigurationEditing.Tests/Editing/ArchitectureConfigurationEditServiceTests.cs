using System.Collections.Immutable;
using AwesomeAssertions;
using Xunit;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Tests.Editing;

public sealed partial class ArchitectureConfigurationEditServiceTests
{
	[Fact]
	public void RemoveDependency_RemovesXmlRuleUsingEditHandle()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			  <Layer name="Waiter"><Class endsWith="Waiter" /></Layer>
			  <AllowedDependency from="Customer" to="Waiter" />
			</ArchitecturalLevels>
			""");
		var handle = CreateHandle(path, "AllowedDependency", "Customer", "Waiter");

		var result = ArchitectureConfigurationEditService.RemoveDependency(handle);

		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		File.ReadAllText(path).Should().NotContain("AllowedDependency");
	}

	[Fact]
	public void SetDependencySites_WritesAllowedSitesAndClearsBlockedSites()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			  <Layer name="Waiter"><Class endsWith="Waiter" /></Layer>
			  <AllowedDependency from="Customer" to="Waiter" blockedSites="Field" />
			</ArchitecturalLevels>
			""");
		var handle = CreateHandle(path, "AllowedDependency", "Customer", "Waiter");

		var result = ArchitectureConfigurationEditService.SetDependencySites(
			handle,
			ArchitectureSiteFilterEditMode.AllowedSites,
			ImmutableArray.Create<string>(ArchitectureDependencySiteNames.Constructor, ArchitectureDependencySiteNames.Method));

		var content = File.ReadAllText(path);
		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		content.Should().Contain("allowedSites=\"Constructor, Method\"");
		content.Should().NotContain("blockedSites");
	}

	[Fact]
	public void AddAllowedDependency_AppendsRootRuleToXmlConfiguration()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Application"><Class endsWith="Service" /></Layer>
			  <Layer name="DataAbstraction"><Class endsWith="Repository" /></Layer>
			</ArchitecturalLevels>
			""");
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);

		var result = ArchitectureConfigurationEditService.AddAllowedDependency(source, "Application/Implementation", "DataAbstraction/Contracts");

		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		File.ReadAllText(path).Should().Contain("<AllowedDependency from=\"/Application/Implementation\" to=\"/DataAbstraction/Contracts\" />");
	}

	[Fact]
	public void AddAllowedDependency_AppendsSiblingRuleInsideSharedParentLayer()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Layer name="Implementation"><Class endsWith="Service" /></Layer>
			    <Layer name="Contracts"><Class startsWith="I" /></Layer>
			  </Layer>
			</ArchitecturalLevels>
			""");
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);

		var result = ArchitectureConfigurationEditService.AddAllowedDependency(source, "Application/Implementation", "Application/Contracts");

		var content = File.ReadAllText(path);
		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		content.Should().Contain("<Layer name=\"Application\">");
		content.Should().Contain("<AllowedDependency from=\"Implementation\" to=\"Contracts\" />");
		content.Should().NotContain("<AllowedDependency from=\"/Application/Implementation\"");
	}

	[Fact]
	public void AddDependency_CanAppendBlockedRule()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			  <Layer name="Pantry"><Class endsWith="Pantry" /></Layer>
			</ArchitecturalLevels>
			""");
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);

		var result = ArchitectureConfigurationEditService.AddDependency(source, "Customer", "Pantry", "BlockedDependency");

		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		File.ReadAllText(path).Should().Contain("<BlockedDependency from=\"Customer\" to=\"Pantry\" />");
	}

	[Fact]
	public void SetDependencyKind_ChangesAllowedRuleToBlockedRule()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			  <Layer name="Pantry"><Class endsWith="Pantry" /></Layer>
			  <AllowedDependency from="Customer" to="Pantry" />
			</ArchitecturalLevels>
			""");
		var handle = CreateHandle(path, "AllowedDependency", "Customer", "Pantry");

		var result = ArchitectureConfigurationEditService.SetDependencyKind(handle, "BlockedDependency");

		var content = File.ReadAllText(path);
		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		content.Should().Contain("<BlockedDependency from=\"Customer\" to=\"Pantry\" />");
		content.Should().NotContain("<AllowedDependency from=\"Customer\" to=\"Pantry\" />");
	}

	[Fact]
	public void SetDependencyAppliesToDescendants_WritesAndRemovesAttribute()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			  <Layer name="Framework"><Class typeName="Task" /></Layer>
			  <AllowedDependency from="Customer" to="Framework" />
			</ArchitecturalLevels>
			""");
		var handle = CreateHandle(path, "AllowedDependency", "Customer", "Framework");

		AssertionExtensions.Should((bool)ArchitectureConfigurationEditService.SetDependencyAppliesToDescendants(handle, true).Succeeded).BeTrue();
		File.ReadAllText(path).Should().Contain("appliesToDescendants=\"true\"");

		AssertionExtensions.Should((bool)ArchitectureConfigurationEditService.SetDependencyAppliesToDescendants(handle, false).Succeeded).BeTrue();
		File.ReadAllText(path).Should().NotContain("appliesToDescendants");
	}

	[Fact]
	public void SetDependencyDescription_WritesDescriptionAttribute()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			  <Layer name="Waiter"><Class endsWith="Waiter" /></Layer>
			  <AllowedDependency from="Customer" to="Waiter" />
			</ArchitecturalLevels>
			""");
		var handle = CreateHandle(path, "AllowedDependency", "Customer", "Waiter");

		var result = ArchitectureConfigurationEditService.SetDependencyDescription(handle, "Customers talk to waiters.");

		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		File.ReadAllText(path).Should().Contain("description=\"Customers talk to waiters.\"");
	}

}
