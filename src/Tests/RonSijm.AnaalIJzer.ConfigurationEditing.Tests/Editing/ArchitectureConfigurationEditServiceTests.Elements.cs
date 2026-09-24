using System.Text;
using AwesomeAssertions;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using Xunit;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Tests.Editing;

public sealed partial class ArchitectureConfigurationEditServiceTests
{
    [Fact]
    public void SetLayerDescription_WritesDescriptionAttribute()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile(
            "Architecture.anl",
            """
			<ArchitecturalLevels>
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			</ArchitecturalLevels>
			""");
        var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Customer", "Customer", string.Empty, null);

        var result = ArchitectureConfigurationEditService.SetLayerDescription(handle, "People ordering food.");

        result.Succeeded.Should().BeTrue(result.Message);
        File.ReadAllText(path).Should().Contain("<Layer name=\"Customer\" description=\"People ordering food.\">");
    }

    [Fact]
    public void XmlFileEdits_ReadUtf8BomFileWithMismatchedUtf16DeclarationAndRewriteUtf8Declaration()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile(
            "Architecture.anl",
            """
			<?xml version="1.0" encoding="utf-16"?>
			<ArchitecturalLevels>
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			</ArchitecturalLevels>
			""",
            new UTF8Encoding(true));
        var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Customer", "Customer", string.Empty, null);

        var details = ArchitectureConfigurationEditService.GetLayerDetails(handle);
        var edit = ArchitectureConfigurationEditService.SetLayerDescription(handle, "People ordering food.");

        details.Succeeded.Should().BeTrue(details.Message);
        edit.Succeeded.Should().BeTrue(edit.Message);
        var content = File.ReadAllText(path);
        content.Should().Contain("encoding=\"utf-8\"");
        content.Should().Contain("description=\"People ordering food.\"");
    }

    [Fact]
    public void GetLayerDetails_ReturnsMatchersAndScopedTypePolicies()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile(
            "Architecture.anl",
            """
			<ArchitecturalLevels>
			  <Layer name="Kitchen" description="Makes food." requireRecognizedDependencies="Constructor">
			    <Class endsWith="Kitchen" />
			    <Namespace contains="Restaurant.Kitchen" />
			    <Allowed>
			      <Class typeKind="Class" />
			    </Allowed>
			    <Forbidden>
			      <Class endsWith="Store" />
			    </Forbidden>
			  </Layer>
			</ArchitecturalLevels>
			""");
        var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Kitchen", "Kitchen", string.Empty, null);

        var result = ArchitectureConfigurationEditService.GetLayerDetails(handle);

        result.Succeeded.Should().BeTrue(result.Message);
        result.Name.Should().Be("Kitchen");
        result.Description.Should().Be("Makes food.");
        result.RequireRecognizedDependencies.Should().Be("Constructor");
        result.Matchers.Select(item => item.Summary).Should().Contain("<Class endsWith=\"Kitchen\" />");
        result.Matchers.Select(item => item.Summary).Should().Contain("<Namespace contains=\"Restaurant.Kitchen\" />");
        result.AllowedPolicies.Single().Summary.Should().Be("<Class typeKind=\"Class\" />");
        result.ForbiddenPolicies.Single().Summary.Should().Be("<Class endsWith=\"Store\" />");
    }

    [Fact]
    public void SetLayerNameAndRequireRecognizedDependencies_UpdateLayerAttributes()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile(
            "Architecture.anl",
            """
			<ArchitecturalLevels>
			  <Layer name="Kitchen"><Class endsWith="Kitchen" /></Layer>
			</ArchitecturalLevels>
			""");
        var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Kitchen", "Kitchen", string.Empty, null);

        ArchitectureConfigurationEditService.SetLayerName(handle, "Chef").Succeeded.Should().BeTrue();
        var renamedHandle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Chef", "Chef", string.Empty, null);
        ArchitectureConfigurationEditService.SetLayerRequireRecognizedDependencies(renamedHandle, "Constructor, Local").Succeeded.Should().BeTrue();

        var content = File.ReadAllText(path);
        content.Should().Contain("<Layer name=\"Chef\" requireRecognizedDependencies=\"Constructor, Local\">");
    }

    [Fact]
    public void AddUpdateAndRemoveLayerMatcher_EditsLayerMatcher()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile(
            "Architecture.anl",
            """
			<ArchitecturalLevels>
			  <Layer name="Kitchen"><Class endsWith="Kitchen" /></Layer>
			</ArchitecturalLevels>
			""");
        var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Kitchen", "Kitchen", string.Empty, null);

        ArchitectureConfigurationEditService.AddLayerMatcher(handle, "Namespace", Attributes(("contains", "Restaurant.Kitchen"))).Succeeded.Should().BeTrue();
        var details = ArchitectureConfigurationEditService.GetLayerDetails(handle);
        var namespaceMatcher = details.Matchers.Single(item => item.ElementKind == "Namespace");

        ArchitectureConfigurationEditService.SetConfigurationElementAttributes(namespaceMatcher.Handle, Attributes(("startsWith", "Restaurant.Kitchen"))).Succeeded.Should().BeTrue();
        File.ReadAllText(path).Should().Contain("<Namespace startsWith=\"Restaurant.Kitchen\" />");

        var updatedDetails = ArchitectureConfigurationEditService.GetLayerDetails(handle);
        var updatedNamespaceMatcher = updatedDetails.Matchers.Single(item => item.ElementKind == "Namespace");
        ArchitectureConfigurationEditService.RemoveConfigurationElement(updatedNamespaceMatcher.Handle).Succeeded.Should().BeTrue();
        File.ReadAllText(path).Should().NotContain("Namespace");
    }

    [Fact]
    public void SetConfigurationElementChildren_EditsMatcherExceptions()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile(
            "Architecture.anl",
            """
			<ArchitecturalLevels>
			  <Layer name="Kitchen"><Class endsWith="Kitchen" /></Layer>
			</ArchitecturalLevels>
			""");
        var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Kitchen", "Kitchen", string.Empty, null);
        var matcher = ArchitectureConfigurationEditService.GetLayerDetails(handle).Matchers.Single();

        var result = ArchitectureConfigurationEditService.SetConfigurationElementChildren(
            matcher.Handle,
            """
			<Exceptions>
			  <Class typeName="OutdoorKitchen" />
			</Exceptions>
			""");

        result.Succeeded.Should().BeTrue(result.Message);
        File.ReadAllText(path).Should().Contain("<Exceptions>");
        File.ReadAllText(path).Should().Contain("<Class typeName=\"OutdoorKitchen\" />");
        ArchitectureConfigurationEditService.GetLayerDetails(handle).Matchers.Single().ChildXml.Should().Contain("OutdoorKitchen");
    }

    [Fact]
    public void AddTypePolicyMatcher_AppendsPolicyContainerWhenMissing()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile(
            "Architecture.anl",
            """
			<ArchitecturalLevels>
			  <Layer name="Kitchen"><Class endsWith="Kitchen" /></Layer>
			</ArchitecturalLevels>
			""");
        var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Kitchen", "Kitchen", string.Empty, null);

        var result = ArchitectureConfigurationEditService.AddTypePolicyMatcher(handle, "Allowed", "Class", Attributes(("typeKind", "Class")));

        result.Succeeded.Should().BeTrue(result.Message);
        var content = File.ReadAllText(path);
        content.Should().Contain("<Allowed>");
        content.Should().Contain("<Class typeKind=\"Class\" />");
    }

    [Fact]
    public void GetRootDetails_ReturnsRootSettingsIncludesAndGlobalPolicies()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile(
            "Architecture.anl",
            """
			<ArchitecturalLevels description="Rules" requireRecognizedDependencies="Constructor" enforceAcyclic="true" enableReport="true" reportPath="reports/violations.md" enableDocumentation="true" documentationPath="docs/architecture.md">
			  <ExceptionPolicy requireReason="true" requireOwner="true" requireExpiresOn="true" warnBeforeDays="21" description="Every exception must expire." />
			  <Include path="Shared.anl" />
			  <Allowed>
			    <Class typeKind="Class" />
			  </Allowed>
			  <Forbidden>
			    <Namespace contains="Legacy" />
			  </Forbidden>
			</ArchitecturalLevels>
			""");
        var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);

        var result = ArchitectureConfigurationEditService.GetRootDetails(source);

        result.Succeeded.Should().BeTrue(result.Message);
        result.Description.Should().Be("Rules");
        result.RequireRecognizedDependencies.Should().Be("Constructor");
        result.EnforceAcyclic.Should().BeTrue();
        result.EnableReport.Should().BeTrue();
        result.ReportPath.Should().Be("reports/violations.md");
        result.EnableDocumentation.Should().BeTrue();
        result.DocumentationPath.Should().Be("docs/architecture.md");
        result.EnableExceptionPolicy.Should().BeTrue();
        result.RequireExceptionReason.Should().BeTrue();
        result.RequireExceptionOwner.Should().BeTrue();
        result.RequireExceptionExpiresOn.Should().BeTrue();
        result.ExceptionWarnBeforeDays.Should().Be(21);
        result.ExceptionPolicyDescription.Should().Be("Every exception must expire.");
        result.Includes.Single().Summary.Should().Be("<Include path=\"Shared.anl\" />");
        result.AllowedPolicies.Single().Summary.Should().Be("<Class typeKind=\"Class\" />");
        result.ForbiddenPolicies.Single().Summary.Should().Be("<Namespace contains=\"Legacy\" />");
    }

    [Fact]
    public void OperationContracts_CanBeInspectedAddedAndEditedAtTheRoot()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile(
            "Architecture.anl",
            """
			<ArchitecturalLevels>
			  <Operations description="Existing operations.">
			    <Operation name="PlacePizzaOrder">
			      <Owner><DeclarationMatcher><Member exactName="PlacePizzaOrder" memberKind="Method" /></DeclarationMatcher></Owner>
			    </Operation>
			  </Operations>
			</ArchitecturalLevels>
			""");
        var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);

        var details = ArchitectureConfigurationEditService.GetRootDetails(source);

        details.Succeeded.Should().BeTrue(details.Message);
        var existing = details.OperationContracts.Should().ContainSingle().Subject;
        existing.Attributes["description"].Should().Be("Existing operations.");
        ArchitectureConfigurationEditService.SetConfigurationElementChildren(
            existing.Handle,
            """
			<Operation name="PlacePizzaOrder">
			  <Owner><DeclarationMatcher><Member exactName="PlacePizzaOrder" memberKind="Method" /></DeclarationMatcher></Owner>
			  <Request><Class exactName="PlacePizzaOrderRequest" /></Request>
			</Operation>
			""").Succeeded.Should().BeTrue();
        ArchitectureConfigurationEditService.AddOperationContracts(
            source,
            Attributes(("description", "Another explicit operation.")),
            """
			<Operation name="CancelPizzaOrder">
			  <Owner><DeclarationMatcher><Member exactName="CancelPizzaOrder" memberKind="Method" /></DeclarationMatcher></Owner>
			</Operation>
			""").Succeeded.Should().BeTrue();

        var content = File.ReadAllText(path);
        content.Should().Contain("PlacePizzaOrderRequest");
        content.Should().Contain("CancelPizzaOrder");
        content.Should().Contain("Another explicit operation.");
    }

    [Fact]
    public void AssemblyAttributePolicies_CanBeInspectedAddedAndEditedAtTheRoot()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile(
            "Architecture.anl",
            """
			<ArchitecturalLevels>
			  <AssemblyAttributePolicy description="Existing friend access rule.">
			    <Forbidden>
			      <Attribute exactFullName="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
			        <Argument index="0" exactName="NotAllowedExample" />
			      </Attribute>
			    </Forbidden>
			  </AssemblyAttributePolicy>
			</ArchitecturalLevels>
			""");
        var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);

        var details = ArchitectureConfigurationEditService.GetRootDetails(source);

        details.Succeeded.Should().BeTrue(details.Message);
        var existing = details.AssemblyAttributePolicies.Should().ContainSingle().Subject;
        existing.Attributes["description"].Should().Be("Existing friend access rule.");
        ArchitectureConfigurationEditService.SetConfigurationElementChildren(
            existing.Handle,
            """
			<Forbidden>
			  <Attribute exactFullName="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
			    <Argument index="0" exactName="NotAllowedExample" />
			    <Argument name="AllInternalsVisible" exactName="false" />
			  </Attribute>
			</Forbidden>
			""").Succeeded.Should().BeTrue();
        ArchitectureConfigurationEditService.AddAssemblyAttributePolicy(
            source,
            Attributes(("description", "Only named pastry teams may receive internals.")),
            """
			<Allowed>
			  <Attribute exactFullName="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
			    <Argument index="0" exactName="AllowedExample" />
			  </Attribute>
			</Allowed>
			""").Succeeded.Should().BeTrue();

        var content = File.ReadAllText(path);
        content.Should().Contain("AllInternalsVisible");
        content.Should().Contain("Only named pastry teams may receive internals.");
        content.Should().Contain("AllowedExample");
    }

    [Fact]
    public void AddAssemblyAttributePolicy_PreservesInlineAssemblyMetadataAndNameofInterpolation()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile(
            "AnaalIJzerSettings.cs",
            """"
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", $"""
			<ArchitecturalLevels>
			  <Layer name="{nameof(PizzaRecipeBook)}">
			    <Class typeName="{nameof(PizzaRecipeBook)}" />
			  </Layer>
			</ArchitecturalLevels>
			""")]

			public sealed class PizzaRecipeBook { }
			"""");
        var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, path);

        var result = ArchitectureConfigurationEditService.AddAssemblyAttributePolicy(
            source,
            Attributes(("description", "Only approved pastry teams may receive internals.")),
            """
			<Forbidden>
			  <Attribute exactFullName="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
			    <Argument index="0" exactName="NotAllowedExample" />
			  </Attribute>
			</Forbidden>
			""");

        result.Succeeded.Should().BeTrue(result.Message);
        var content = File.ReadAllText(path);
        content.Should().Contain("{nameof(PizzaRecipeBook)}");
        content.Should().Contain("<AssemblyAttributePolicy description=\"Only approved pastry teams may receive internals.\"");
        content.Should().Contain("<Argument index=\"0\" exactName=\"NotAllowedExample\" />");
    }

    [Fact]
    public void SetRootSettings_UpdatesRootAttributes()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile(
            "Architecture.anl",
            """
			<ArchitecturalLevels>
			  <Layer name="Kitchen"><Class endsWith="Kitchen" /></Layer>
			</ArchitecturalLevels>
			""");
        var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);

        var result = ArchitectureConfigurationEditService.SetRootSettings(
            source,
            "Restaurant rules",
            "Constructor, Local",
            true,
            true,
            "reports/violations.md",
            true,
            "docs/architecture.md",
            true,
            true,
            true,
            true,
            30,
            "Temporary exceptions need ownership.");

        var content = File.ReadAllText(path);
        result.Succeeded.Should().BeTrue(result.Message);
        content.Should().Contain("description=\"Restaurant rules\"");
        content.Should().Contain("requireRecognizedDependencies=\"Constructor, Local\"");
        content.Should().Contain("enforceAcyclic=\"true\"");
        content.Should().Contain("enableReport=\"true\"");
        content.Should().Contain("reportPath=\"reports/violations.md\"");
        content.Should().Contain("enableDocumentation=\"true\"");
        content.Should().Contain("documentationPath=\"docs/architecture.md\"");
        content.Should().Contain("<ExceptionPolicy");
        content.Should().Contain("requireReason=\"true\"");
        content.Should().Contain("requireOwner=\"true\"");
        content.Should().Contain("requireExpiresOn=\"true\"");
        content.Should().Contain("warnBeforeDays=\"30\"");
        content.Should().Contain("description=\"Temporary exceptions need ownership.\"");
    }

    [Fact]
    public void SetRootSettings_WhenExceptionPolicyDisabled_RemovesExceptionPolicy()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile(
            "Architecture.anl",
            """
			<ArchitecturalLevels>
			  <ExceptionPolicy requireReason="true" requireOwner="true" />
			</ArchitecturalLevels>
			""");
        var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);

        var result = ArchitectureConfigurationEditService.SetRootSettings(
            source,
            null,
            null,
            false,
            false,
            null,
            false,
            null,
            false,
            false,
            false,
            false,
            14,
            null);

        var content = File.ReadAllText(path);
        result.Succeeded.Should().BeTrue(result.Message);
        content.Should().NotContain("ExceptionPolicy");
    }

    [Fact]
    public void AddIncludeAndGlobalTypePolicyMatcher_EditRootElements()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile(
            "Architecture.anl",
            """
			<ArchitecturalLevels>
			  <Layer name="Kitchen"><Class endsWith="Kitchen" /></Layer>
			</ArchitecturalLevels>
			""");
        var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);

        ArchitectureConfigurationEditService.AddInclude(source, "Shared.anl", false).Succeeded.Should().BeTrue();
        ArchitectureConfigurationEditService.AddInclude(source, "OptionalRules/*.anl", true).Succeeded.Should().BeTrue();
        ArchitectureConfigurationEditService.AddGlobalTypePolicyMatcher(source, "Forbidden", "Class", Attributes(("endsWith", "Store"))).Succeeded.Should().BeTrue();

        var content = File.ReadAllText(path);
        content.Should().Contain("<Include path=\"Shared.anl\" />");
        content.Should().Contain("<Include path=\"OptionalRules/*.anl\" allowNoMatches=\"true\" />");
        content.Should().Contain("<Forbidden>");
        content.Should().Contain("<Class endsWith=\"Store\" />");
    }

}