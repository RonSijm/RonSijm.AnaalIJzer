using System.Text;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.GraphApplication.Tests.Editing;

public sealed class ArchitectureGraphEditServiceTests
{
	[Fact]
	public void CreateConfiguration_WritesArchitectureFile()
	{
		using var directory = new TemporaryDirectory();
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, directory.GetPath("Architecture.anl"));
		var service = new ArchitectureGraphEditService();

		var result = service.CreateConfiguration(source);

		result.Succeeded.Should().BeTrue(result.Message);
		File.Exists(source.Path).Should().BeTrue();
		File.ReadAllText(source.Path).Should().Contain("<ArchitecturalLevels");
	}

	[Fact]
	public void AddAllowedDependency_AppendsRuleToCreatedConfiguration()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			  <Layer name="Waiter"><Class endsWith="Waiter" /></Layer>
			</ArchitecturalLevels>
			""");
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);
		var service = new ArchitectureGraphEditService();

		var result = service.AddAllowedDependency(source, "Customer", "Waiter");

		result.Succeeded.Should().BeTrue(result.Message);
		File.ReadAllText(path).Should().Contain("<AllowedDependency from=\"Customer\" to=\"Waiter\" />");
	}

	[Fact]
	public void SetDependencySites_PersistsAllowedSitesThroughGraphEditService()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			  <Layer name="Waiter"><Class endsWith="Waiter" /></Layer>
			  <AllowedDependency from="Customer" to="Waiter" />
			</ArchitecturalLevels>
			""");
		var handle = new ArchitectureDependencyRuleEditHandle(
			ArchitectureConfigurationSourceKind.XmlFile,
			path,
			0,
			0,
			"AllowedDependency",
			string.Empty,
			"Customer",
			"Waiter",
			"Customer",
			"Waiter",
			false);
		var service = new ArchitectureGraphEditService();

		var result = service.SetDependencySites(
			handle,
			ArchitectureSiteFilterEditMode.AllowedSites,
            [ArchitectureDependencySiteNames.MethodReturn, ArchitectureDependencySiteNames.New]);

		result.Succeeded.Should().BeTrue(result.Message);
		File.ReadAllText(path).Should().Contain("allowedSites=\"MethodReturn, New\"");
	}

	[Fact]
	public void AddReturnValuePolicy_PersistsThroughGraphEditService()
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
		var service = new ArchitectureGraphEditService();

		var result = service.AddReturnValuePolicy(
			handle,
			ImmutableDictionary<string, string>.Empty.Add("description", "No sentinel meals."),
			"""
			<Literal value="null" />
			<Invocation withAttribute="CanBeNullAttribute" />
			""");

		result.Succeeded.Should().BeTrue(result.Message);
		File.ReadAllText(path).Should().Contain("<ReturnValuePolicy description=\"No sentinel meals.\"");
		File.ReadAllText(path).Should().Contain("<Literal value=\"null\" />");
	}

	[Fact]
	public void AddForbiddenOperationPolicy_PersistsThroughGraphEditService()
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
		var service = new ArchitectureGraphEditService();

		var result = service.AddForbiddenOperationPolicy(
			handle,
			ImmutableDictionary<string, string>.Empty.Add("description", "Use a shared kitchen clock."),
			"""
			<ForbiddenOperation allowedSites="StaticMember">
			  <OperationMatcher kind="PropertyRead" staticAccess="true">
			    <ContainingType exactFullName="System.DateTime" />
			    <Member exactName="UtcNow" memberKind="Property" />
			  </OperationMatcher>
			</ForbiddenOperation>
			""");

		result.Succeeded.Should().BeTrue(result.Message);
		File.ReadAllText(path).Should().Contain("<ForbiddenOperations description=\"Use a shared kitchen clock.\"");
		File.ReadAllText(path).Should().Contain("<Member exactName=\"UtcNow\" memberKind=\"Property\" />");
	}

	[Fact]
	public void AddBehavioralOperationPolicy_PersistsThroughGraphEditService()
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
		var service = new ArchitectureGraphEditService();

		var result = service.AddBehavioralOperationPolicy(
			handle,
			ImmutableDictionary<string, string>.Empty.Add("description", "Validate before saving."),
			"""
			<RequiredOperationBefore>
			  <DeclarationMatcher><Member exactName="Submit" memberKind="Method" /></DeclarationMatcher>
			  <OperationMatcher kind="Invocation"><Member exactName="Validate" memberKind="Method" /></OperationMatcher>
			  <BeforeOperation><OperationMatcher kind="Invocation"><Member exactName="Save" memberKind="Method" /></OperationMatcher></BeforeOperation>
			</RequiredOperationBefore>
			""");

		result.Succeeded.Should().BeTrue(result.Message);
		File.ReadAllText(path).Should().Contain("<BehavioralOperations description=\"Validate before saving.\"");
		File.ReadAllText(path).Should().Contain("<RequiredOperationBefore>");
	}

	[Fact]
	public void AddOperationContracts_PersistsThroughGraphEditService()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Controller"><Class endsWith="Controller" /></Layer>
			  <Layer name="Application"><Class endsWith="Kitchen" /></Layer>
			</ArchitecturalLevels>
			""");
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);
		var service = new ArchitectureGraphEditService();

		var result = service.AddOperationContracts(
			source,
			ImmutableDictionary<string, string>.Empty.Add("description", "A waiter directly calls the kitchen owner."),
			"""
			<Operation name="PlacePizzaOrder" allowedOwnerLayers="Application" allowedEntryPointLayers="Controller">
			  <Owner>
			    <DeclarationMatcher>
			      <ContainingType endsWith="Kitchen" />
			      <Member exactName="PlacePizzaOrder" memberKind="Method" />
			    </DeclarationMatcher>
			  </Owner>
			</Operation>
			""");

		result.Succeeded.Should().BeTrue(result.Message);
		File.ReadAllText(path).Should().Contain("<Operations description=\"A waiter directly calls the kitchen owner.\"");
		File.ReadAllText(path).Should().Contain("<Operation name=\"PlacePizzaOrder\" allowedOwnerLayers=\"Application\" allowedEntryPointLayers=\"Controller\">");
	}

	[Fact]
	public void AddAssemblyAttributePolicy_PersistsThroughGraphEditService()
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
		var service = new ArchitectureGraphEditService();

		var result = service.AddAssemblyAttributePolicy(
			source,
			ImmutableDictionary<string, string>.Empty.Add("description", "Only approved pastry teams receive internals."),
			"""
			<Forbidden>
			  <Attribute exactFullName="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
			    <Argument index="0" exactName="NotAllowedExample" />
			  </Attribute>
			</Forbidden>
			""");

		result.Succeeded.Should().BeTrue(result.Message);
		var content = File.ReadAllText(path);
		content.Should().Contain("<AssemblyAttributePolicy description=\"Only approved pastry teams receive internals.\"");
		content.Should().Contain("<Argument index=\"0\" exactName=\"NotAllowedExample\" />");
	}

	[Fact]
	public void AddAssemblyAttributePolicy_PersistsInlineSettingsThroughGraphEditService()
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
		var service = new ArchitectureGraphEditService();

		var result = service.AddAssemblyAttributePolicy(
			source,
			ImmutableDictionary<string, string>.Empty.Add("description", "Only approved pastry teams may receive internals."),
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
	}

	private sealed class TemporaryDirectory : IDisposable
	{
		private readonly string _path = Path.Combine(Path.GetTempPath(), "AnaalIJzerGraphEditingTests", Guid.NewGuid().ToString("N"));

		public string WriteFile(string fileName, string content, Encoding? encoding = null)
		{
			Directory.CreateDirectory(_path);
			var filePath = Path.Combine(_path, fileName);
			File.WriteAllText(filePath, content, encoding ?? Encoding.UTF8);

			return filePath;
		}

		public string GetPath(string fileName)
		{
			Directory.CreateDirectory(_path);
			var result = Path.Combine(_path, fileName);

			return result;
		}

		public void Dispose()
		{
			if (Directory.Exists(_path))
			{
				Directory.Delete(_path, true);
			}
		}
	}
}
