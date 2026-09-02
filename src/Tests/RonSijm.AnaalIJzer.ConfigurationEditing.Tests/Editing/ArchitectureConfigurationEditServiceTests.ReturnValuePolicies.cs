using AwesomeAssertions;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using Xunit;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Tests.Editing;

public sealed partial class ArchitectureConfigurationEditServiceTests
{
	[Fact]
	public void ReturnValuePolicy_IsInspectableEditableAndRemovableInXml()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Kitchen">
			    <Class endsWith="Kitchen" />
			    <ReturnValuePolicy description="Serve an actual pizza.">
			      <Literal value="null" />
			      <Invocation withAttribute="JetBrains.Annotations.CanBeNullAttribute" description="Handle optional menu lookups." />
			    </ReturnValuePolicy>
			  </Layer>
			</ArchitecturalLevels>
			""");
		var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Kitchen", "Kitchen", string.Empty, null);

		var policy = ArchitectureConfigurationEditService.GetLayerDetails(handle).ReturnValuePolicies.Should().ContainSingle().Which;
		var attributesResult = ArchitectureConfigurationEditService.SetConfigurationElementAttributes(
			policy.Handle,
			Attributes(("description", "Use an explicit fallback.")));
		var childrenResult = ArchitectureConfigurationEditService.SetConfigurationElementChildren(
			policy.Handle,
			"""
			<Literal value="" />
			<Literal value="42" />
			<Invocation withAttribute="CanBeNullAttribute" description="Handle the optional result." />
			""");

		attributesResult.Succeeded.Should().BeTrue(attributesResult.Message);
		childrenResult.Succeeded.Should().BeTrue(childrenResult.Message);
		var updatedPolicy = ArchitectureConfigurationEditService.GetLayerDetails(handle).ReturnValuePolicies.Should().ContainSingle().Which;
		updatedPolicy.Attributes["description"].Should().Be("Use an explicit fallback.");
		updatedPolicy.ChildXml.Should().Contain("value=\"42\"");
		ArchitectureConfigurationEditService.RemoveConfigurationElement(updatedPolicy.Handle).Succeeded.Should().BeTrue();
		File.ReadAllText(path).Should().NotContain("ReturnValuePolicy");
	}

	[Fact]
	public void AddReturnValuePolicy_PreservesInlineAssemblyMetadataAndNameofInterpolation()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Example.cs",
			""""
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", $"""
			<ArchitecturalLevels>
			  <Layer name="{nameof(PizzaKitchen)}">
			    <Class typeName="{nameof(PizzaKitchen)}" />
			  </Layer>
			</ArchitecturalLevels>
			""")]

			internal class PizzaKitchen { }
			"""");
		var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, path, 0, "PizzaKitchen", "PizzaKitchen", string.Empty, null);

		var result = ArchitectureConfigurationEditService.AddReturnValuePolicy(
			handle,
			Attributes(("description", "Kitchens make serving decisions.")),
			"""
			<Literal value="null" />
			<Invocation withAttribute="JetBrains.Annotations.CanBeNullAttribute" />
			""");

		result.Succeeded.Should().BeTrue(result.Message);
		var content = File.ReadAllText(path);
		content.Should().Contain("<ReturnValuePolicy");
		content.Should().Contain("JetBrains.Annotations.CanBeNullAttribute");
		content.Should().Contain("{nameof(PizzaKitchen)}");
		ArchitectureConfigurationEditService.GetLayerDetails(handle).ReturnValuePolicies.Should().ContainSingle();
	}

	[Theory]
	[InlineData("")]
	[InlineData("""<Throw />""")]
	[InlineData("""<Literal invalid="pizza" />""")]
	public void AddReturnValuePolicy_RejectsInvalidMatchers(string childXml)
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile("Architecture.anl", "<ArchitecturalLevels><Layer name=\"Kitchen\"><Class endsWith=\"Kitchen\" /></Layer></ArchitecturalLevels>");
		var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Kitchen", "Kitchen", string.Empty, null);

		var result = ArchitectureConfigurationEditService.AddReturnValuePolicy(handle, Attributes(("description", "No sentinel meals.")), childXml);

		result.Succeeded.Should().BeFalse();
		File.ReadAllText(path).Should().NotContain("ReturnValuePolicy");
	}
}
