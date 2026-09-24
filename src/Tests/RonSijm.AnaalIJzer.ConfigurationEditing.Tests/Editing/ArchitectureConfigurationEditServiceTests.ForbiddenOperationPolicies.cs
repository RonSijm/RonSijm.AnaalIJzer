using AwesomeAssertions;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using Xunit;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Tests.Editing;

public sealed partial class ArchitectureConfigurationEditServiceTests
{
    [Fact]
    public void BehavioralOperationPolicy_IsInspectableEditableAndRemovableInXml()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile(
            "Architecture.anl",
            """
			<ArchitecturalLevels>
			  <Layer name="Kitchen">
			    <Class endsWith="Kitchen" />
			    <BehavioralOperations description="Validate before cooking.">
			      <RequiredOperationBefore>
			        <DeclarationMatcher><Member exactName="Submit" memberKind="Method" /></DeclarationMatcher>
			        <OperationMatcher kind="Invocation"><Member exactName="Validate" memberKind="Method" /></OperationMatcher>
			        <BeforeOperation><OperationMatcher kind="Invocation"><Member exactName="Save" memberKind="Method" /></OperationMatcher></BeforeOperation>
			      </RequiredOperationBefore>
			    </BehavioralOperations>
			  </Layer>
			</ArchitecturalLevels>
			""");
        var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Kitchen", "Kitchen", string.Empty, null);

        var policy = ArchitectureConfigurationEditService.GetLayerDetails(handle).BehavioralOperationPolicies.Should().ContainSingle().Which;
        var attributesResult = ArchitectureConfigurationEditService.SetConfigurationElementAttributes(policy.Handle, Attributes(("description", "Validate before publication.")));
        var childrenResult = ArchitectureConfigurationEditService.SetConfigurationElementChildren(
            policy.Handle,
            """
			<MaximumOperationCount maximum="1">
			  <DeclarationMatcher><Member exactName="Submit" memberKind="Method" /></DeclarationMatcher>
			  <OperationMatcher kind="Invocation"><Member exactName="Publish" memberKind="Method" /></OperationMatcher>
			</MaximumOperationCount>
			""");

        attributesResult.Succeeded.Should().BeTrue(attributesResult.Message);
        childrenResult.Succeeded.Should().BeTrue(childrenResult.Message);
        var updatedPolicy = ArchitectureConfigurationEditService.GetLayerDetails(handle).BehavioralOperationPolicies.Should().ContainSingle().Which;
        updatedPolicy.Attributes["description"].Should().Be("Validate before publication.");
        updatedPolicy.ChildXml.Should().Contain("MaximumOperationCount");
        ArchitectureConfigurationEditService.RemoveConfigurationElement(updatedPolicy.Handle).Succeeded.Should().BeTrue();
        File.ReadAllText(path).Should().NotContain("BehavioralOperations");
    }

    [Fact]
    public void AddBehavioralOperationPolicy_PreservesInlineAssemblyMetadataAndNameofInterpolation()
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

        var result = ArchitectureConfigurationEditService.AddBehavioralOperationPolicy(
            handle,
            Attributes(("description", "Validate before saving.")),
            """
			<RequiredOperationBefore>
			  <DeclarationMatcher><Member exactName="Submit" memberKind="Method" /></DeclarationMatcher>
			  <OperationMatcher kind="Invocation"><Member exactName="Validate" memberKind="Method" /></OperationMatcher>
			  <BeforeOperation><OperationMatcher kind="Invocation"><Member exactName="Save" memberKind="Method" /></OperationMatcher></BeforeOperation>
			</RequiredOperationBefore>
			""");

        result.Succeeded.Should().BeTrue(result.Message);
        var content = File.ReadAllText(path);
        content.Should().Contain("<BehavioralOperations");
        content.Should().Contain("RequiredOperationBefore");
        content.Should().Contain("{nameof(PizzaKitchen)}");
        ArchitectureConfigurationEditService.GetLayerDetails(handle).BehavioralOperationPolicies.Should().ContainSingle();
    }

    [Theory]
    [InlineData("")]
    [InlineData("""<RequiredOperation><OperationMatcher kind="Invocation" /></RequiredOperation>""")]
    [InlineData("""<MaximumOperationCount maximum="0"><DeclarationMatcher><Member exactName="Submit" memberKind="Method" /></DeclarationMatcher><OperationMatcher kind="Invocation" /></MaximumOperationCount>""")]
    public void AddBehavioralOperationPolicy_RejectsInvalidRuleShape(string childXml)
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile("Architecture.anl", "<ArchitecturalLevels><Layer name=\"Kitchen\"><Class endsWith=\"Kitchen\" /></Layer></ArchitecturalLevels>");
        var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Kitchen", "Kitchen", string.Empty, null);

        var result = ArchitectureConfigurationEditService.AddBehavioralOperationPolicy(handle, Attributes(("description", "Use a validator.")), childXml);

        result.Succeeded.Should().BeFalse();
        File.ReadAllText(path).Should().NotContain("BehavioralOperations");
    }

    [Fact]
    public void ForbiddenOperationPolicy_IsInspectableEditableAndRemovableInXml()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile(
            "Architecture.anl",
            """
			<ArchitecturalLevels>
			  <Layer name="Kitchen">
			    <Class endsWith="Kitchen" />
			    <ForbiddenOperations description="Kitchens use the restaurant clock.">
			      <ForbiddenOperation allowedSites="StaticMember">
			        <OperationMatcher kind="PropertyRead" staticAccess="true">
			          <ContainingType exactFullName="System.DateTime" />
			          <Member exactName="UtcNow" memberKind="Property" />
			        </OperationMatcher>
			      </ForbiddenOperation>
			    </ForbiddenOperations>
			  </Layer>
			</ArchitecturalLevels>
			""");
        var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Kitchen", "Kitchen", string.Empty, null);

        var policy = ArchitectureConfigurationEditService.GetLayerDetails(handle).ForbiddenOperationPolicies.Should().ContainSingle().Which;
        var attributesResult = ArchitectureConfigurationEditService.SetConfigurationElementAttributes(policy.Handle, Attributes(("description", "Kitchens use a shared clock adapter.")));
        var childrenResult = ArchitectureConfigurationEditService.SetConfigurationElementChildren(
            policy.Handle,
            """
			<ForbiddenOperation allowedSites="StaticMember">
			  <OperationMatcher kind="PropertyRead" staticAccess="true">
			    <ContainingType exactFullName="System.Environment" />
			    <Member exactName="MachineName" memberKind="Property" />
			  </OperationMatcher>
			</ForbiddenOperation>
			""");

        attributesResult.Succeeded.Should().BeTrue(attributesResult.Message);
        childrenResult.Succeeded.Should().BeTrue(childrenResult.Message);
        var updatedPolicy = ArchitectureConfigurationEditService.GetLayerDetails(handle).ForbiddenOperationPolicies.Should().ContainSingle().Which;
        updatedPolicy.Attributes["description"].Should().Be("Kitchens use a shared clock adapter.");
        updatedPolicy.ChildXml.Should().Contain("MachineName");
        ArchitectureConfigurationEditService.RemoveConfigurationElement(updatedPolicy.Handle).Succeeded.Should().BeTrue();
        File.ReadAllText(path).Should().NotContain("ForbiddenOperations");
    }

    [Fact]
    public void AddForbiddenOperationPolicy_PreservesInlineAssemblyMetadataAndNameofInterpolation()
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

        var result = ArchitectureConfigurationEditService.AddForbiddenOperationPolicy(
            handle,
            Attributes(("description", "Kitchens do not read the clock directly.")),
            """
			<ForbiddenOperation allowedSites="StaticMember">
			  <OperationMatcher kind="PropertyRead" staticAccess="true">
			    <ContainingType exactFullName="System.DateTime" />
			    <Member exactName="UtcNow" memberKind="Property" />
			  </OperationMatcher>
			</ForbiddenOperation>
			""");

        result.Succeeded.Should().BeTrue(result.Message);
        var content = File.ReadAllText(path);
        content.Should().Contain("<ForbiddenOperations");
        content.Should().Contain("DateTime");
        content.Should().Contain("{nameof(PizzaKitchen)}");
        ArchitectureConfigurationEditService.GetLayerDetails(handle).ForbiddenOperationPolicies.Should().ContainSingle();
    }

    [Theory]
    [InlineData("")]
    [InlineData("""<OperationMatcher kind="PropertyRead" />""")]
    [InlineData("""<ForbiddenOperation><OperationMatcher /></ForbiddenOperation>""")]
    public void AddForbiddenOperationPolicy_RejectsInvalidRuleShape(string childXml)
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile("Architecture.anl", "<ArchitecturalLevels><Layer name=\"Kitchen\"><Class endsWith=\"Kitchen\" /></Layer></ArchitecturalLevels>");
        var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Kitchen", "Kitchen", string.Empty, null);

        var result = ArchitectureConfigurationEditService.AddForbiddenOperationPolicy(handle, Attributes(("description", "Use the clock adapter.")), childXml);

        result.Succeeded.Should().BeFalse();
        File.ReadAllText(path).Should().NotContain("ForbiddenOperations");
    }
}