using AwesomeAssertions;
using Xunit;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Tests.Editing;

public sealed partial class ArchitectureConfigurationEditServiceTests
{
	[Fact]
	public void GetLayerDetails_ReturnsRecursiveExceptionMatchersFromMatchersAndPolicies()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Kitchen">
			    <Class endsWith="Kitchen">
			      <Exceptions>
			        <Class typeName="OutdoorKitchen">
			          <Exceptions>
			            <Class typeName="PizzaTruckKitchen" />
			          </Exceptions>
			        </Class>
			      </Exceptions>
			    </Class>
			    <Allowed>
			      <Class endsWith="Chef">
			        <Exceptions>
			          <Class typeName="TempChef" />
			        </Exceptions>
			      </Class>
			    </Allowed>
			    <Forbidden>
			      <Class endsWith="Store">
			        <Exceptions>
			          <Class typeName="LegacyStore" />
			        </Exceptions>
			      </Class>
			    </Forbidden>
			  </Layer>
			</ArchitecturalLevels>
			""");
		var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Kitchen", "Kitchen", string.Empty, null);

		var result = ArchitectureConfigurationEditService.GetLayerDetails(handle);

		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		ImmutableArrayExtensions.Select(result.ExceptionMatchers, item => item.Summary).Should().Contain([
			"<Class typeName=\"OutdoorKitchen\" />",
			"<Class typeName=\"PizzaTruckKitchen\" />",
			"<Class typeName=\"TempChef\" />",
			"<Class typeName=\"LegacyStore\" />"]);
	}

	[Fact]
	public void GetRootDetails_ReturnsRecursiveGlobalExceptionMatchers()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Allowed>
			    <Class endsWith="Contract">
			      <Exceptions>
			        <Class typeName="LegacyContract" />
			      </Exceptions>
			    </Class>
			  </Allowed>
			  <Forbidden>
			    <Namespace contains="Legacy">
			      <Exceptions>
			        <Namespace exactName="Legacy.Allowed">
			          <Exceptions>
			            <Namespace exactName="Legacy.Allowed.Internal" />
			          </Exceptions>
			        </Namespace>
			      </Exceptions>
			    </Namespace>
			  </Forbidden>
			</ArchitecturalLevels>
			""");
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);

		var result = ArchitectureConfigurationEditService.GetRootDetails(source);

		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		ImmutableArrayExtensions.Select(result.ExceptionMatchers, item => item.Summary).Should().Contain([
			"<Class typeName=\"LegacyContract\" />",
			"<Namespace exactName=\"Legacy.Allowed\" />",
			"<Namespace exactName=\"Legacy.Allowed.Internal\" />"]);
	}

	[Fact]
	public void RemoveConfigurationElement_RemovesEmptyExceptionsContainer()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Kitchen">
			    <Class endsWith="Kitchen">
			      <Exceptions>
			        <Class typeName="OutdoorKitchen" />
			      </Exceptions>
			    </Class>
			  </Layer>
			</ArchitecturalLevels>
			""");
		var handle = new ArchitectureLayerEditHandle(ArchitectureConfigurationSourceKind.XmlFile, path, 0, "Kitchen", "Kitchen", string.Empty, null);
		var exceptionMatcher = ArchitectureConfigurationEditService.GetLayerDetails(handle).ExceptionMatchers.Should().ContainSingle().Which;

		var result = ArchitectureConfigurationEditService.RemoveConfigurationElement(exceptionMatcher.Handle);

		AssertionExtensions.Should((bool)result.Succeeded).BeTrue(result.Message);
		var content = File.ReadAllText(path);
		content.Should().NotContain("<Exceptions>");
		ArchitectureConfigurationEditService.GetLayerDetails(handle).ExceptionMatchers.Should().BeEmpty();
	}
}
