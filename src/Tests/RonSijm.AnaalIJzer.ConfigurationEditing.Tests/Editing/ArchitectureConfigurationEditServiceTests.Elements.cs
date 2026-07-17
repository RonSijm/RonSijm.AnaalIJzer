using System.Text;
using AwesomeAssertions;
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

		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
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

		AssertionExtensions.Should((bool)details.Succeeded).BeTrue(details.Message);
		AssertionExtensions.Should((bool)edit.Succeeded).BeTrue(edit.Message);
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

		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		AssertionExtensions.Should((string)result.Name).Be("Kitchen");
		AssertionExtensions.Should((string)result.Description).Be("Makes food.");
		AssertionExtensions.Should((string)result.RequireRecognizedDependencies).Be("Constructor");
		ImmutableArrayExtensions.Select(result.Matchers, item => item.Summary).Should().Contain("<Class endsWith=\"Kitchen\" />");
		ImmutableArrayExtensions.Select(result.Matchers, item => item.Summary).Should().Contain("<Namespace contains=\"Restaurant.Kitchen\" />");
		AssertionExtensions.Should((string)ImmutableArrayExtensions.Single(result.AllowedPolicies).Summary).Be("<Class typeKind=\"Class\" />");
		AssertionExtensions.Should((string)ImmutableArrayExtensions.Single(result.ForbiddenPolicies).Summary).Be("<Class endsWith=\"Store\" />");
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

		AssertionExtensions.Should((bool)ArchitectureConfigurationEditService.SetLayerName(handle, "Chef").Succeeded).BeTrue();
		var renamedHandle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Chef", "Chef", string.Empty, null);
		AssertionExtensions.Should((bool)ArchitectureConfigurationEditService.SetLayerRequireRecognizedDependencies(renamedHandle, "Constructor, Local").Succeeded).BeTrue();

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

		AssertionExtensions.Should((bool)ArchitectureConfigurationEditService.AddLayerMatcher(handle, "Namespace", Attributes(("contains", "Restaurant.Kitchen"))).Succeeded).BeTrue();
		var details = ArchitectureConfigurationEditService.GetLayerDetails(handle);
		var namespaceMatcher = ImmutableArrayExtensions.Single(details.Matchers, item => item.ElementKind == "Namespace");

		AssertionExtensions.Should((bool)ArchitectureConfigurationEditService.SetConfigurationElementAttributes(namespaceMatcher.Handle, Attributes(("startsWith", "Restaurant.Kitchen"))).Succeeded).BeTrue();
		File.ReadAllText(path).Should().Contain("<Namespace startsWith=\"Restaurant.Kitchen\" />");

		var updatedDetails = ArchitectureConfigurationEditService.GetLayerDetails(handle);
		var updatedNamespaceMatcher = ImmutableArrayExtensions.Single(updatedDetails.Matchers, item => item.ElementKind == "Namespace");
		AssertionExtensions.Should((bool)ArchitectureConfigurationEditService.RemoveConfigurationElement(updatedNamespaceMatcher.Handle).Succeeded).BeTrue();
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
		var matcher = ImmutableArrayExtensions.Single(ArchitectureConfigurationEditService.GetLayerDetails(handle).Matchers);

		var result = ArchitectureConfigurationEditService.SetConfigurationElementChildren(
			matcher.Handle,
			"""
			<Exceptions>
			  <Class typeName="OutdoorKitchen" />
			</Exceptions>
			""");

		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		File.ReadAllText(path).Should().Contain("<Exceptions>");
		File.ReadAllText(path).Should().Contain("<Class typeName=\"OutdoorKitchen\" />");
		AssertionExtensions.Should((string)ImmutableArrayExtensions.Single(ArchitectureConfigurationEditService.GetLayerDetails(handle).Matchers).ChildXml).Contain("OutdoorKitchen");
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

		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
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

		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		AssertionExtensions.Should((string)result.Description).Be("Rules");
		AssertionExtensions.Should((string)result.RequireRecognizedDependencies).Be("Constructor");
		AssertionExtensions.Should((bool)result.EnforceAcyclic).BeTrue();
		AssertionExtensions.Should((bool)result.EnableReport).BeTrue();
		AssertionExtensions.Should((string)result.ReportPath).Be("reports/violations.md");
		AssertionExtensions.Should((bool)result.EnableDocumentation).BeTrue();
		AssertionExtensions.Should((string)result.DocumentationPath).Be("docs/architecture.md");
		AssertionExtensions.Should((string)ImmutableArrayExtensions.Single(result.Includes).Summary).Be("<Include path=\"Shared.anl\" />");
		AssertionExtensions.Should((string)ImmutableArrayExtensions.Single(result.AllowedPolicies).Summary).Be("<Class typeKind=\"Class\" />");
		AssertionExtensions.Should((string)ImmutableArrayExtensions.Single(result.ForbiddenPolicies).Summary).Be("<Namespace contains=\"Legacy\" />");
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
			"docs/architecture.md");

		var content = File.ReadAllText(path);
		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		content.Should().Contain("description=\"Restaurant rules\"");
		content.Should().Contain("requireRecognizedDependencies=\"Constructor, Local\"");
		content.Should().Contain("enforceAcyclic=\"true\"");
		content.Should().Contain("enableReport=\"true\"");
		content.Should().Contain("reportPath=\"reports/violations.md\"");
		content.Should().Contain("enableDocumentation=\"true\"");
		content.Should().Contain("documentationPath=\"docs/architecture.md\"");
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

		AssertionExtensions.Should((bool)ArchitectureConfigurationEditService.AddInclude(source, "Shared.anl").Succeeded).BeTrue();
		AssertionExtensions.Should((bool)ArchitectureConfigurationEditService.AddGlobalTypePolicyMatcher(source, "Forbidden", "Class", Attributes(("endsWith", "Store"))).Succeeded).BeTrue();

		var content = File.ReadAllText(path);
		content.Should().Contain("<Include path=\"Shared.anl\" />");
		content.Should().Contain("<Forbidden>");
		content.Should().Contain("<Class endsWith=\"Store\" />");
	}

}
