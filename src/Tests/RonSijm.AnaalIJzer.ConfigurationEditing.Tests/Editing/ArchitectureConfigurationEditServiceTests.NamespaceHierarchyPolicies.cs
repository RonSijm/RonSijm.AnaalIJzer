using AwesomeAssertions;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using Xunit;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Tests.Editing;

public sealed partial class ArchitectureConfigurationEditServiceTests
{
	[Fact]
	public void NamespaceHierarchyPolicy_IsInspectableEditableAndRemovableInXml()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <NamespaceHierarchyPolicy rootNamespace="Restaurant" description="Feature namespaces own their recipes.">
			    <BlockedRelation relation="DescendantToAncestor" />
			  </NamespaceHierarchyPolicy>
			</ArchitecturalLevels>
			""");
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);

		var details = ArchitectureConfigurationEditService.GetRootDetails(source);

		details.Succeeded.Should().BeTrue(details.Message);
		var policy = details.NamespaceHierarchyPolicies.Should().ContainSingle().Subject;
		policy.Attributes["rootNamespace"].Should().Be("Restaurant");
		ArchitectureConfigurationEditService.SetConfigurationElementAttributes(
			policy.Handle,
			Attributes(("rootNamespace", "Restaurant.Orders"), ("description", "Orders own their details."))).Succeeded.Should().BeTrue();
		ArchitectureConfigurationEditService.SetConfigurationElementChildren(
			policy.Handle,
			"""
			<BlockedRelation relation="DescendantToAncestor" allowedSites="Constructor, Field" />
			<BlockedRelation relation="SiblingToSibling" blockedSites="Method" />
			""").Succeeded.Should().BeTrue();

		var updated = ArchitectureConfigurationEditService.GetRootDetails(source).NamespaceHierarchyPolicies.Should().ContainSingle().Subject;
		updated.Attributes["rootNamespace"].Should().Be("Restaurant.Orders");
		updated.ChildXml.Should().Contain("SiblingToSibling");
		ArchitectureConfigurationEditService.RemoveConfigurationElement(updated.Handle).Succeeded.Should().BeTrue();
		File.ReadAllText(path).Should().NotContain("NamespaceHierarchyPolicy");
	}

	[Fact]
	public void AddNamespaceHierarchyPolicy_PreservesInlineAssemblyMetadataAndNameofInterpolation()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"AnaalIJzerSettings.cs",
			""""
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", $"""
			<ArchitecturalLevels>
			  <Layer name="{nameof(PizzaRequest)}">
			    <Class typeName="{nameof(PizzaRequest)}" />
			  </Layer>
			</ArchitecturalLevels>
			""")]

			public sealed class PizzaRequest { }
			"""");
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, path);

		var result = ArchitectureConfigurationEditService.AddNamespaceHierarchyPolicy(
			source,
			Attributes(("rootNamespace", "Restaurant.Orders"), ("description", "Order namespaces own their details.")),
			"""
			<BlockedRelation relation="DescendantToAncestor" allowedSites="Constructor" />
			""");

		result.Succeeded.Should().BeTrue(result.Message);
		var content = File.ReadAllText(path);
		content.Should().Contain("{nameof(PizzaRequest)}");
		content.Should().Contain("<NamespaceHierarchyPolicy");
		content.Should().Contain("rootNamespace=\"Restaurant.Orders\"");
		content.Should().Contain("allowedSites=\"Constructor\"");
	}
}
