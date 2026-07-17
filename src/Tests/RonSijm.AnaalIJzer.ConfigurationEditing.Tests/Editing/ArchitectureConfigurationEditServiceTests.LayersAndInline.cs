using System.Collections.Immutable;
using AwesomeAssertions;
using Xunit;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Tests.Editing;

public sealed partial class ArchitectureConfigurationEditServiceTests
{
	[Fact]
	public void AddLayer_AppendsRootAndChildLayers()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="DiningRoom"><Class endsWith="DiningRoom" /></Layer>
			</ArchitecturalLevels>
			""");
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);

		AssertionExtensions.Should((bool)ArchitectureConfigurationEditService.AddLayer(source, string.Empty, "Kitchen", "Class", Attributes(("endsWith", "Kitchen"))).Succeeded).BeTrue();
		AssertionExtensions.Should((bool)ArchitectureConfigurationEditService.AddLayer(source, "Kitchen", "Chef", "Class", Attributes(("endsWith", "Chef"))).Succeeded).BeTrue();

		var content = File.ReadAllText(path);
		content.Should().Contain("<Layer name=\"Kitchen\">");
		content.Should().Contain("<Layer name=\"Chef\">");
		content.Should().Contain("<Class endsWith=\"Chef\" />");
	}

	[Fact]
	public void MoveLayer_MovesLayerToNewParent()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="DiningRoom"><Class endsWith="DiningRoom" /></Layer>
			  <Layer name="Kitchen"><Class endsWith="Kitchen" /></Layer>
			  <Layer name="Chef"><Class endsWith="Chef" /></Layer>
			</ArchitecturalLevels>
			""");
		var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Chef", "Chef", string.Empty, null);

		var result = ArchitectureConfigurationEditService.MoveLayer(handle, "Kitchen");

		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		File.ReadAllText(path).Should().Contain("<Layer name=\"Kitchen\">");
		AssertionExtensions.Should((bool)ArchitectureConfigurationEditService.GetLayerDetails(new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Kitchen/Chef", "Chef", "Kitchen", null)).Succeeded).BeTrue();
	}

	[Fact]
	public void RemoveLayer_RemovesLayerSubtree()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Kitchen">
			    <Class endsWith="Kitchen" />
			    <Layer name="Chef"><Class endsWith="Chef" /></Layer>
			  </Layer>
			</ArchitecturalLevels>
			""");
		var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Kitchen", "Kitchen", string.Empty, null);

		var result = ArchitectureConfigurationEditService.RemoveLayer(handle);

		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		var content = File.ReadAllText(path);
		content.Should().NotContain("Kitchen");
		content.Should().NotContain("Chef");
	}

	[Fact]
	public void SetDependencySites_EditsInlineAssemblyMetadataLiteral()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"AnaalIJzerSettings.cs",
			""""
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", """
			<ArchitecturalLevels>
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			  <Layer name="Waiter"><Class endsWith="Waiter" /></Layer>
			  <AllowedDependency from="Customer" to="Waiter" />
			</ArchitecturalLevels>
			""")]
			"""");
		var handle = CreateHandle(path, "AllowedDependency", "Customer", "Waiter", ArchitectureConfigurationSourceKind.InlineAssemblyMetadata);

		var result = ArchitectureConfigurationEditService.SetDependencySites(
			handle,
			ArchitectureSiteFilterEditMode.BlockedSites,
			ImmutableArray.Create(ArchitectureDependencySiteNames.Local));

		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		File.ReadAllText(path).Should().Contain("blockedSites=\"Local\"");
	}

	[Fact]
	public void SetDependencySites_PreservesNameofInterpolationInInlineAssemblyMetadata()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"AnaalIJzerSettings.cs",
			""""
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", $"""
			<ArchitecturalLevels>
			  <Layer name="{nameof(Customer)}"><Class endsWith="Customer" /></Layer>
			  <Layer name="Waiter"><Class endsWith="Waiter" /></Layer>
			  <AllowedDependency from="{nameof(Customer)}" to="Waiter" />
			</ArchitecturalLevels>
			""")]
			public class Customer { }
			"""");
		var handle = CreateHandle(path, "AllowedDependency", "Customer", "Waiter", ArchitectureConfigurationSourceKind.InlineAssemblyMetadata);

		var result = ArchitectureConfigurationEditService.SetDependencySites(
			handle,
			ArchitectureSiteFilterEditMode.BlockedSites,
			ImmutableArray.Create(ArchitectureDependencySiteNames.Local));

		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		var content = File.ReadAllText(path);
		content.Should().Contain("from=\"{nameof(Customer)}\"");
		content.Should().Contain("blockedSites=\"Local\"");
	}

	[Fact]
	public void AddConfigurationElement_PreservesNameofInterpolationInInlineAssemblyMetadata()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"AnaalIJzerSettings.cs",
			""""
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", $"""
			<ArchitecturalLevels>
			  <Layer name="{nameof(Chef)}">
			    <Class typeName="{nameof(Chef)}" />
			  </Layer>
			</ArchitecturalLevels>
			""")]
			public class Chef { }
			public class SauceChef { }
			"""");
		var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, path, 0, "Chef", "Chef", string.Empty, null);

		var result = ArchitectureConfigurationEditService.AddLayerMatcher(handle, "Class", Attributes(("typeName", "SauceChef")));

		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		var content = File.ReadAllText(path);
		content.Should().Contain("name=\"{nameof(Chef)}\"");
		content.Should().Contain("typeName=\"{nameof(Chef)}\"");
		content.Should().Contain("<Class typeName=\"SauceChef\" />");
	}
}
