using AwesomeAssertions;
using Xunit;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Tests.Editing;

public sealed partial class ArchitectureConfigurationEditServiceTests
{
	[Fact]
	public void ApiSurfacePolicy_IsInspectableEditableAndRemovableInXml()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface requireRecognizedTypes="true" description="Public contract">
			      <AllowedLayer path="/Contracts" allowedSites="MethodReturn" />
			      <BlockedLayer path="/QuerySurface" />
			    </ApiSurface>
			  </Layer>
			  <Layer name="Contracts"><Class endsWith="Projection" /></Layer>
			  <Layer name="QuerySurface"><Class endsWith="Queryable" /></Layer>
			</ArchitecturalLevels>
			""");
		var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Application", "Application", string.Empty, null);

		var policy = ArchitectureConfigurationEditService.GetLayerDetails(handle).ApiSurfacePolicies.Should().ContainSingle().Which;
		var attributesResult = ArchitectureConfigurationEditService.SetConfigurationElementAttributes(policy.Handle, Attributes(("description", "Updated contract")));
		var childrenResult = ArchitectureConfigurationEditService.SetConfigurationElementChildren(policy.Handle, """<BlockedLayer path="/QuerySurface" blockedSites="Method" />""");

		attributesResult.Succeeded.Should().BeTrue(attributesResult.Message);
		childrenResult.Succeeded.Should().BeTrue(childrenResult.Message);
		var updated = ArchitectureConfigurationEditService.GetLayerDetails(handle).ApiSurfacePolicies.Should().ContainSingle().Which;
		updated.Attributes["description"].Should().Be("Updated contract");
		updated.ChildXml.Should().Contain("blockedSites=\"Method\"");
		ArchitectureConfigurationEditService.RemoveConfigurationElement(updated.Handle).Succeeded.Should().BeTrue();
		File.ReadAllText(path).Should().NotContain("ApiSurface");
	}

	[Fact]
	public void AddApiSurfacePolicy_PreservesInlineNameofInterpolation()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Example.cs",
			""""
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", $"""
			<ArchitecturalLevels>
			  <Layer name="{nameof(CandyService)}">
			    <Class typeName="{nameof(CandyService)}" />
			  </Layer>
			  <Layer name="{nameof(LollyQueryable)}">
			    <Class typeName="{nameof(LollyQueryable)}" />
			  </Layer>
			</ArchitecturalLevels>
			""")]

			public class CandyService { }
			public class LollyQueryable { }
			"""");
		var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, path, 0, "CandyService", "CandyService", string.Empty, null);

		var result = ArchitectureConfigurationEditService.AddApiSurfacePolicy(
			handle,
			Attributes(("requireRecognizedTypes", "true"), ("description", "Do not expose query surfaces.")),
			"""
			<TransitiveExposure maxDepth="4" description="Inspect DTO members." />
			<BlockedLayer path="/LollyQueryable" allowedSites="MethodReturn" />
			""");

		result.Succeeded.Should().BeTrue(result.Message);
		var content = File.ReadAllText(path);
		content.Should().Contain("<ApiSurface");
		content.Should().Contain("<TransitiveExposure maxDepth=\"4\" description=\"Inspect DTO members.\" />");
		content.Should().Contain("<BlockedLayer path=\"/LollyQueryable\" allowedSites=\"MethodReturn\" />");
		content.Should().Contain("{nameof(CandyService)}");
		content.Should().Contain("{nameof(LollyQueryable)}");
	}

	[Fact]
	public void TransitiveExposure_IsInspectableAndEditableAsApiSurfaceChildXml()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface>
			      <TransitiveExposure maxDepth="3" description="Inspect contracts." />
			      <BlockedLayer path="/QuerySurface" />
			    </ApiSurface>
			  </Layer>
			</ArchitecturalLevels>
			""");
		var layerHandle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Application", "Application", string.Empty, null);
		var policy = ArchitectureConfigurationEditService.GetLayerDetails(layerHandle).ApiSurfacePolicies.Should().ContainSingle().Which;

		var result = ArchitectureConfigurationEditService.SetConfigurationElementChildren(
			policy.Handle,
			"""
			<TransitiveExposure maxDepth="6" description="Inspect the complete contract graph." />
			<BlockedLayer path="/QuerySurface" />
			""");

		result.Succeeded.Should().BeTrue(result.Message);
		var updated = ArchitectureConfigurationEditService.GetLayerDetails(layerHandle).ApiSurfacePolicies.Should().ContainSingle().Which;
		updated.ChildXml.Should().Contain("maxDepth=\"6\"");
		updated.ChildXml.Should().Contain("Inspect the complete contract graph.");
	}
}
