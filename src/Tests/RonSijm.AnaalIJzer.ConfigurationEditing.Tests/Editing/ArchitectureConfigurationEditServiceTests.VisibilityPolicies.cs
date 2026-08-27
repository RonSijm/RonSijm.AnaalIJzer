using AwesomeAssertions;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using Xunit;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Tests.Editing;

public sealed partial class ArchitectureConfigurationEditServiceTests
{
	[Fact]
	public void VisibilityPolicies_AreInspectableEditableAndRemovableInXml()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="QuerySurface">
			    <Class endsWith="Queryable" />
			    <VisibilityPolicy targets="Type" allowedAccessibilities="Internal, File" description="Keep query surfaces internal." />
			  </Layer>
			</ArchitecturalLevels>
			""");
		var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "QuerySurface", "QuerySurface", string.Empty, null);

		var details = ArchitectureConfigurationEditService.GetLayerDetails(handle);
		var policy = details.VisibilityPolicies.Should().ContainSingle().Which;
		var edit = ArchitectureConfigurationEditService.SetConfigurationElementAttributes(
			policy.Handle,
			Attributes(
				("targets", "Field, Property"),
				("blockedAccessibilities", "Public, Protected"),
				("description", "Do not expose stored query state.")));

		edit.Succeeded.Should().BeTrue(edit.Message);
		var updatedPolicy = ArchitectureConfigurationEditService.GetLayerDetails(handle).VisibilityPolicies.Should().ContainSingle().Which;
		updatedPolicy.Attributes["blockedAccessibilities"].Should().Be("Public, Protected");
		updatedPolicy.Attributes.Should().NotContainKey("allowedAccessibilities");
		ArchitectureConfigurationEditService.RemoveConfigurationElement(updatedPolicy.Handle).Succeeded.Should().BeTrue();
		File.ReadAllText(path).Should().NotContain("VisibilityPolicy");
	}

	[Fact]
	public void AddVisibilityPolicy_PreservesInlineAssemblyMetadataAndNameofInterpolation()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Example.cs",
			""""
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", $"""
			<ArchitecturalLevels>
			  <Layer name="{nameof(LollyQueryable)}">
			    <Class typeName="{nameof(LollyQueryable)}" />
			  </Layer>
			</ArchitecturalLevels>
			""")]

			internal class LollyQueryable { }
			"""");
		var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, path, 0, "LollyQueryable", "LollyQueryable", string.Empty, null);

		var result = ArchitectureConfigurationEditService.AddVisibilityPolicy(
			handle,
			Attributes(
				("targets", "Type"),
				("allowedAccessibilities", "Internal, File"),
				("description", "Repository-owned query surface.")));

		result.Succeeded.Should().BeTrue(result.Message);
		var content = File.ReadAllText(path);
		content.Should().Contain("<VisibilityPolicy");
		content.Should().Contain("allowedAccessibilities=\"Internal, File\"");
		content.Should().Contain("{nameof(LollyQueryable)}");
		ArchitectureConfigurationEditService.GetLayerDetails(handle).VisibilityPolicies.Should().ContainSingle();
	}

	[Theory]
	[InlineData("targets", "Type")]
	[InlineData("allowedAccessibilities", "Internal")]
	public void AddVisibilityPolicy_RejectsIncompletePolicy(string key, string value)
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile("Architecture.anl", "<ArchitecturalLevels><Layer name=\"Policy\"><Class endsWith=\"Service\" /></Layer></ArchitecturalLevels>");
		var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Policy", "Policy", string.Empty, null);

		var result = ArchitectureConfigurationEditService.AddVisibilityPolicy(handle, Attributes((key, value)));

		result.Succeeded.Should().BeFalse();
		File.ReadAllText(path).Should().NotContain("VisibilityPolicy");
	}
}
