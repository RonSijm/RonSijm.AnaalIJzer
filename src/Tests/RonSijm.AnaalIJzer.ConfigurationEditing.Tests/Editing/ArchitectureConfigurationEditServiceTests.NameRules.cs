using AwesomeAssertions;
using Xunit;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Tests.Editing;

public sealed partial class ArchitectureConfigurationEditServiceTests
{
	[Fact]
	public void NameRules_AreInspectableEditableAndRemovableInXml()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Endpoints">
			    <Class endsWith="Controller" />
			    <NameRules>
			      <RequireDeclarationNameMatchesType allowedSites="Method">
			        <Type implements="IHonestType" />
			      </RequireDeclarationNameMatchesType>
			    </NameRules>
			  </Layer>
			</ArchitecturalLevels>
			""");
		var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Endpoints", "Endpoints", string.Empty, null);

		var details = ArchitectureConfigurationEditService.GetLayerDetails(handle);
		var rule = details.NameRules.Should().ContainSingle().Which;
		var edit = ArchitectureConfigurationEditService.SetConfigurationElementChildren(
			rule.Handle,
			"""
			<Type implements="IHonestType" />
			<Name endsWith="Id" />
			""");

		AssertionExtensions.Should((bool)edit.Succeeded).BeTrue(edit.Message);
		File.ReadAllText(path).Should().Contain("<Name endsWith=\"Id\" />");
		var updatedRule = ArchitectureConfigurationEditService.GetLayerDetails(handle).NameRules.Should().ContainSingle().Which;
		AssertionExtensions.Should((bool)ArchitectureConfigurationEditService.RemoveConfigurationElement(updatedRule.Handle).Succeeded).BeTrue();
		File.ReadAllText(path).Should().NotContain("NameRules");
	}

	[Fact]
	public void AddNameRule_PreservesInlineAssemblyMetadataAndNameofInterpolation()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Example.cs",
			""""
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", $"""
			<ArchitecturalLevels>
			  <Layer name="{nameof(PatientController)}">
			    <Class typeName="{nameof(PatientController)}" />
			  </Layer>
			</ArchitecturalLevels>
			""")]

			public class PatientController { }
			"""");
		var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, path, 0, "PatientController", "PatientController", string.Empty, null);

		var result = ArchitectureConfigurationEditService.AddNameRule(handle, "RequireDeclarationNameMatchesType", Attributes(("allowedSites", "Method, Property")));

		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		var content = File.ReadAllText(path);
		content.Should().Contain("<RequireDeclarationNameMatchesType allowedSites=\"Method, Property\" />");
		content.Should().Contain("{nameof(PatientController)}");
		ArchitectureConfigurationEditService.GetLayerDetails(handle).NameRules.Should().ContainSingle();
	}
}
