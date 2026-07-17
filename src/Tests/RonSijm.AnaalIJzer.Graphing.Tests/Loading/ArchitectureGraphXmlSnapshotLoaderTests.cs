using System.Text;
using AwesomeAssertions;
using Xunit;

namespace RonSijm.AnaalIJzer.Graphing.Tests.Loading;

public sealed class ArchitectureGraphXmlSnapshotLoaderTests
{
	[Fact]
	public void Load_ReadsConnectStyleNestedConfigurationIntoNonEmptyGraph()
	{
		var path = WriteTempFile(
			"""
			<?xml version="1.0" encoding="utf-16"?>
			<ArchitecturalLevels xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
			                      xsi:noNamespaceSchemaLocation="AnaalIJzer.xsd"
			                      description="Connect-style architecture">
			  <Layer name="Crosscutting">
			    <Namespace startsWith="System" />
			  </Layer>
			  <Layer name="Controller">
			    <Class endsWith="Controller" />
			  </Layer>
			  <Layer name="Application">
			    <Class endsWith="Manager" />
			    <Layer name="ApplicationInterfaces">
			      <Class endsWith="Manager" typeKind="Interface" />
			    </Layer>
			    <Layer name="ApplicationImplementation">
			      <Class endsWith="Manager" typeKind="Class" />
			    </Layer>
			    <AllowedDependency from="ApplicationImplementation" to="ApplicationInterfaces" allowedSites="InterfaceImplementation" />
			    <AllowedDependency from="ApplicationImplementation" to="/Ports/PortInterfaces" />
			    <AllowedDependency from="/Controller" to="ApplicationInterfaces" />
			  </Layer>
			  <Layer name="Ports">
			    <Class endsWith="Repository" />
			    <Layer name="PortInterfaces">
			      <Class endsWith="Repository" typeKind="Interface" />
			    </Layer>
			    <Layer name="PortImplementation">
			      <Class endsWith="Repository" typeKind="Class" />
			    </Layer>
			    <AllowedDependency from="PortImplementation" to="PortInterfaces" allowedSites="InterfaceImplementation" />
			  </Layer>
			  <AllowedDependency from="*" to="Crosscutting" appliesToDescendants="true" />
			  <AllowedDependency from="Controller" to="Application" />
			  <AllowedDependency from="Application" to="Ports" />
			</ArchitecturalLevels>
			""");

		var snapshot = ArchitectureGraphXmlSnapshotLoader.Load(path);
		var groups = ArchitectureGraphViewModelBuilder.Build(snapshot, ArchitectureGraphFocusMode.ShowAll);

		ImmutableArrayExtensions.Select(snapshot.Layers, layer => layer.Path).Should().Contain([
			"Application/ApplicationInterfaces",
			"Application/ApplicationImplementation",
			"Ports/PortInterfaces",
			"Ports/PortImplementation"]);
		snapshot.Rules.Should().Contain(rule =>
			rule.ScopePath == "Application"
			&& rule.From == "Application/ApplicationImplementation"
			&& rule.To == "Ports/PortInterfaces");
		snapshot.Rules.Should().Contain(rule => rule.From == "*" && rule.To == "Crosscutting" && rule.AppliesToDescendants);
		groups.Should().NotBeEmpty();
		Enumerable.SelectMany(groups, group => group.Nodes).Should().NotBeEmpty();
	}

	[Fact]
	public void Load_ReadsUtf8BomFileWithMismatchedUtf16Declaration()
	{
		var path = WriteTempFile(
			"""
			<?xml version="1.0" encoding="utf-16"?>
			<ArchitecturalLevels>
			  <Layer name="Controller">
			    <Class endsWith="Controller" />
			  </Layer>
			</ArchitecturalLevels>
			""",
			new UTF8Encoding(true));

		var snapshot = ArchitectureGraphXmlSnapshotLoader.Load(path);

		AssertionExtensions.Should((string)snapshot.Layers.Should().ContainSingle()
        .Which.Path).Be("Controller");
	}

	[Fact]
	public void Load_ExpandsIncludedAnlFilesIntoGraphSnapshot()
	{
		var directory = Path.Combine(Path.GetTempPath(), "AnaalIJzerGraphXmlSnapshotLoaderTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		var includedPath = Path.Combine(directory, "SharedApplicationLayers.anl");
		File.WriteAllText(
			includedPath,
			"""
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			  </Layer>
			  <Layer name="Persistence">
			    <Class endsWith="Repository" />
			  </Layer>
			  <AllowedDependency from="Application" to="Persistence" />
			</ArchitecturalLevels>
			""",
			Encoding.Unicode);
		var rootPath = Path.Combine(directory, "Architecture.anl");
		File.WriteAllText(
			rootPath,
			"""
			<ArchitecturalLevels>
			  <Include path="SharedApplicationLayers.anl" />
			  <Layer name="Presentation">
			    <Class endsWith="Endpoint" />
			  </Layer>
			  <AllowedDependency from="Presentation" to="Application" />
			</ArchitecturalLevels>
			""",
			Encoding.Unicode);

		var snapshot = ArchitectureGraphXmlSnapshotLoader.Load(rootPath);

		ImmutableArrayExtensions.Select(snapshot.Layers, layer => layer.Path).Should().Contain(["Presentation", "Application", "Persistence"]);
		snapshot.Layers.Single(layer => layer.Path == "Application").SourcePath.Should().Be(Path.GetFullPath(includedPath));
		snapshot.Rules.Should().Contain(rule => rule.From == "Application" && rule.To == "Persistence");
		snapshot.Rules.Should().Contain(rule => rule.From == "Presentation" && rule.To == "Application");
	}

	[Fact]
	public void Load_ReadsInlineAssemblyMetadataConfiguration()
	{
		var path = WriteTempFile(
			""""
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", """
			<ArchitecturalLevels>
			  <Layer name="Controller">
			    <Class endsWith="Controller" />
			  </Layer>
			  <Layer name="Application">
			    <Class endsWith="Manager" />
			  </Layer>
			  <AllowedDependency from="Controller" to="Application" />
			</ArchitecturalLevels>
			""")]
			"""",
			Encoding.UTF8,
			"AnaalIJzerSettings.cs");
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, path);

		var snapshot = ArchitectureGraphXmlSnapshotLoader.Load(source);

		snapshot.ConfigurationSource.Kind.Should().Be(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata);
		ImmutableArrayExtensions.Select(snapshot.Layers, layer => layer.Path).Should().Contain(["Controller", "Application"]);
		snapshot.Rules.Should().ContainSingle(rule => rule.From == "Controller" && rule.To == "Application");
	}

	private static string WriteTempFile(string content, Encoding? encoding = null, string fileName = "Architecture.anl")
	{
		var directory = Path.Combine(Path.GetTempPath(), "AnaalIJzerGraphXmlSnapshotLoaderTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		var path = Path.Combine(directory, fileName);
		File.WriteAllText(path, content, encoding ?? Encoding.Unicode);

		return path;
	}
}
