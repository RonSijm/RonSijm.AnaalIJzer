using AwesomeAssertions;
using Xunit;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Tests.Editing;

public sealed partial class ArchitectureConfigurationEditServiceTests
{
	[Fact]
	public void InheritancePolicies_AreInspectableEditableAndRemovableInXml()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="PersistenceEntities">
			    <Namespace startsWith="Shop.Persistence" />
			    <InheritancePolicy typeKinds="Class" requiredBaseTypes="Entity" description="Entities inherit Entity." />
			  </Layer>
			</ArchitecturalLevels>
			""");
		var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "PersistenceEntities", "PersistenceEntities", string.Empty, null);

		var details = ArchitectureConfigurationEditService.GetLayerDetails(handle);
		var policy = details.InheritancePolicies.Should().ContainSingle().Which;
		var edit = ArchitectureConfigurationEditService.SetConfigurationElementAttributes(
			policy.Handle,
			Attributes(
				("typeKinds", "Class, Record"),
				("requiredBaseTypes", "Entity, AggregateRoot"),
				("requiredInterfaces", "IAuditedEntity"),
				("description", "Updated entity contract.")));

		edit.Succeeded.Should().BeTrue(edit.Message);
		var updatedPolicy = ArchitectureConfigurationEditService.GetLayerDetails(handle).InheritancePolicies.Should().ContainSingle().Which;
		updatedPolicy.Attributes["typeKinds"].Should().Be("Class, Record");
		updatedPolicy.Attributes["requiredBaseTypes"].Should().Be("Entity, AggregateRoot");
		updatedPolicy.Attributes["requiredInterfaces"].Should().Be("IAuditedEntity");
		ArchitectureConfigurationEditService.RemoveConfigurationElement(updatedPolicy.Handle).Succeeded.Should().BeTrue();
		File.ReadAllText(path).Should().NotContain("InheritancePolicy");
	}

	[Fact]
	public void AddInheritancePolicy_PreservesInlineAssemblyMetadataAndNameofInterpolation()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Example.cs",
			""""
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", $"""
			<ArchitecturalLevels>
			  <Layer name="{nameof(CandyEntity)}">
			    <Class typeName="{nameof(CandyEntity)}" />
			  </Layer>
			</ArchitecturalLevels>
			""")]

			internal class CandyEntity { }
			"""");
		var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, path, 0, "CandyEntity", "CandyEntity", string.Empty, null);

		var result = ArchitectureConfigurationEditService.AddInheritancePolicy(
			handle,
			Attributes(
				("typeKinds", "Class"),
				("requiredBaseTypes", "Entity"),
				("description", "Persistence entities inherit Entity.")));

		result.Succeeded.Should().BeTrue(result.Message);
		var content = File.ReadAllText(path);
		content.Should().Contain("<InheritancePolicy");
		content.Should().Contain("requiredBaseTypes=\"Entity\"");
		content.Should().Contain("{nameof(CandyEntity)}");
		ArchitectureConfigurationEditService.GetLayerDetails(handle).InheritancePolicies.Should().ContainSingle();
	}

	[Theory]
	[InlineData("requiredBaseTypes", "Entity")]
	[InlineData("typeKinds", "Class")]
	public void AddInheritancePolicy_RejectsIncompletePolicy(string key, string value)
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile("Architecture.anl", "<ArchitecturalLevels><Layer name=\"Policy\"><Class endsWith=\"Entity\" /></Layer></ArchitecturalLevels>");
		var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Policy", "Policy", string.Empty, null);

		var result = ArchitectureConfigurationEditService.AddInheritancePolicy(handle, Attributes((key, value)));

		result.Succeeded.Should().BeFalse();
		File.ReadAllText(path).Should().NotContain("InheritancePolicy");
	}
}
