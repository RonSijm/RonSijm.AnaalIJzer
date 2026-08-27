using AwesomeAssertions;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using Xunit;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Tests.Editing;

public sealed partial class ArchitectureConfigurationEditServiceTests
{
	[Fact]
	public void CreateConfiguration_WritesEmptyArchitectureFile()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.GetPath("Architecture.anl");
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);

		var result = ArchitectureConfigurationEditService.CreateConfiguration(source);

		result.Succeeded.Should().BeTrue(result.Message);
		File.Exists(path).Should().BeTrue();
		File.ReadAllText(path).Should().Contain("<ArchitecturalLevels");
	}

	[Fact]
	public void CreateConfiguration_ProjectFileTarget_RegistersAdditionalFiles()
	{
		using var directory = new TemporaryDirectory();
		var projectPath = directory.WriteFile("Demo.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
		var architecturePath = directory.GetPath("Architecture.anl");
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, architecturePath);
		var target = new ArchitectureConfigurationCreationTarget(
			"Project file",
			"Create project settings.",
			source,
			ArchitectureConfigurationRegistrationKind.ProjectFile,
			projectPath);

		var result = ArchitectureConfigurationEditService.CreateConfiguration(target);

		result.Succeeded.Should().BeTrue(result.Message);
		File.ReadAllText(projectPath).Should().Contain("<AdditionalFiles Include=\"Architecture.anl\" />");
	}

	[Fact]
	public void CreateConfiguration_DirectoryBuildPropsTarget_CreatesPropsAndRegistersAdditionalFiles()
	{
		using var directory = new TemporaryDirectory();
		var propsPath = directory.GetPath("Directory.Build.props");
		var architecturePath = directory.GetPath("Architecture.anl");
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, architecturePath);
		var target = new ArchitectureConfigurationCreationTarget(
			"Project folder",
			"Create folder settings.",
			source,
			ArchitectureConfigurationRegistrationKind.DirectoryBuildProps,
			propsPath);

		var result = ArchitectureConfigurationEditService.CreateConfiguration(target);

		result.Succeeded.Should().BeTrue(result.Message);
		File.Exists(propsPath).Should().BeTrue();
		File.ReadAllText(propsPath).Should().Contain("<AdditionalFiles Include=\"Architecture.anl\" />");
	}

	[Fact]
	public void CreateConfiguration_DoesNotOverwriteExistingArchitectureFile()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile("Architecture.anl", "<ArchitecturalLevels />");
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);

		var result = ArchitectureConfigurationEditService.CreateConfiguration(source);

		result.Succeeded.Should().BeFalse();
		result.Message.Should().Contain("already exists");
	}

	[Fact]
	public void RemoveDependency_RemovesXmlRuleUsingEditHandle()
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
		var handle = CreateHandle(path, "AllowedDependency", "Customer", "Waiter");

		var result = ArchitectureConfigurationEditService.RemoveDependency(handle);

		result.Succeeded.Should().BeTrue(result.Message);
		File.ReadAllText(path).Should().NotContain("AllowedDependency");
	}

	[Fact]
	public void SetDependencySites_WritesAllowedSitesAndClearsBlockedSites()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			  <Layer name="Waiter"><Class endsWith="Waiter" /></Layer>
			  <AllowedDependency from="Customer" to="Waiter" blockedSites="Field" />
			</ArchitecturalLevels>
			""");
		var handle = CreateHandle(path, "AllowedDependency", "Customer", "Waiter");

		var result = ArchitectureConfigurationEditService.SetDependencySites(
			handle,
			ArchitectureSiteFilterEditMode.AllowedSites,
			[ArchitectureDependencySiteNames.Constructor, ArchitectureDependencySiteNames.Method]);

		var content = File.ReadAllText(path);
		result.Succeeded.Should().BeTrue(result.Message);
		content.Should().Contain("allowedSites=\"Constructor, Method\"");
		content.Should().NotContain("blockedSites");
	}

	[Fact]
	public void AddAllowedDependency_AppendsRootRuleToXmlConfiguration()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Application"><Class endsWith="Service" /></Layer>
			  <Layer name="DataAbstraction"><Class endsWith="Repository" /></Layer>
			</ArchitecturalLevels>
			""");
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);

		var result = ArchitectureConfigurationEditService.AddAllowedDependency(source, "Application/Implementation", "DataAbstraction/Contracts");

		result.Succeeded.Should().BeTrue(result.Message);
		File.ReadAllText(path).Should().Contain("<AllowedDependency from=\"/Application/Implementation\" to=\"/DataAbstraction/Contracts\" />");
	}

	[Fact]
	public void AddAllowedDependency_AppendsSiblingRuleInsideSharedParentLayer()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Layer name="Implementation"><Class endsWith="Service" /></Layer>
			    <Layer name="Contracts"><Class startsWith="I" /></Layer>
			  </Layer>
			</ArchitecturalLevels>
			""");
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);

		var result = ArchitectureConfigurationEditService.AddAllowedDependency(source, "Application/Implementation", "Application/Contracts");

		var content = File.ReadAllText(path);
		result.Succeeded.Should().BeTrue(result.Message);
		content.Should().Contain("<Layer name=\"Application\">");
		content.Should().Contain("<AllowedDependency from=\"Implementation\" to=\"Contracts\" />");
		content.Should().NotContain("<AllowedDependency from=\"/Application/Implementation\"");
	}

	[Fact]
	public void AddDependency_CanAppendBlockedRule()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			  <Layer name="Pantry"><Class endsWith="Pantry" /></Layer>
			</ArchitecturalLevels>
			""");
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, path);

		var result = ArchitectureConfigurationEditService.AddDependency(source, "Customer", "Pantry", "BlockedDependency");

		result.Succeeded.Should().BeTrue(result.Message);
		File.ReadAllText(path).Should().Contain("<BlockedDependency from=\"Customer\" to=\"Pantry\" />");
	}

	[Fact]
	public void SetDependencyKind_ChangesAllowedRuleToBlockedRule()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			  <Layer name="Pantry"><Class endsWith="Pantry" /></Layer>
			  <AllowedDependency from="Customer" to="Pantry" />
			</ArchitecturalLevels>
			""");
		var handle = CreateHandle(path, "AllowedDependency", "Customer", "Pantry");

		var result = ArchitectureConfigurationEditService.SetDependencyKind(handle, "BlockedDependency");

		var content = File.ReadAllText(path);
		result.Succeeded.Should().BeTrue(result.Message);
		content.Should().Contain("<BlockedDependency from=\"Customer\" to=\"Pantry\" />");
		content.Should().NotContain("<AllowedDependency from=\"Customer\" to=\"Pantry\" />");
	}

	[Fact]
	public void SetDependencyAppliesToDescendants_WritesAndRemovesAttribute()
	{
		using var directory = new TemporaryDirectory();
		var path = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			  <Layer name="Framework"><Class typeName="Task" /></Layer>
			  <AllowedDependency from="Customer" to="Framework" />
			</ArchitecturalLevels>
			""");
		var handle = CreateHandle(path, "AllowedDependency", "Customer", "Framework");

		ArchitectureConfigurationEditService.SetDependencyAppliesToDescendants(handle, true).Succeeded.Should().BeTrue();
		File.ReadAllText(path).Should().Contain("appliesToDescendants=\"true\"");

		ArchitectureConfigurationEditService.SetDependencyAppliesToDescendants(handle, false).Succeeded.Should().BeTrue();
		File.ReadAllText(path).Should().NotContain("appliesToDescendants");
	}

	[Fact]
	public void SetDependencyDescription_WritesDescriptionAttribute()
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
		var handle = CreateHandle(path, "AllowedDependency", "Customer", "Waiter");

		var result = ArchitectureConfigurationEditService.SetDependencyDescription(handle, "Customers talk to waiters.");

		result.Succeeded.Should().BeTrue(result.Message);
		File.ReadAllText(path).Should().Contain("description=\"Customers talk to waiters.\"");
	}

}
