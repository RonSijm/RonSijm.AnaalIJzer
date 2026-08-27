using System.Globalization;
using System.Text;
using AwesomeAssertions;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.GraphModel.Loading;
using Xunit;

namespace RonSijm.AnaalIJzer.GraphModel.Tests.Loading;

public sealed class ArchitectureGraphXmlSnapshotLoaderTests
{
	[Fact]
	public void Load_ReadsEmptyConfigurationAsEditableBlankGraph()
	{
		var path = WriteTempFile("<ArchitecturalLevels />");

		var snapshot = ArchitectureGraphXmlSnapshotLoader.Load(path);

		snapshot.HasConfiguration.Should().BeTrue();
		snapshot.HasConfigurationIssues.Should().BeFalse();
		snapshot.Layers.Should().BeEmpty();
		snapshot.Rules.Should().BeEmpty();
		snapshot.ConfigurationSource.Path.Should().Be(Path.GetFullPath(path));
	}

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

		snapshot.Layers.Select(layer => layer.Path).Should().Contain([
			"Application/ApplicationInterfaces",
			"Application/ApplicationImplementation",
			"Ports/PortInterfaces",
			"Ports/PortImplementation"
		]);
		snapshot.Rules.Should().Contain(rule =>
			rule.ScopePath == "Application"
			&& rule.From == "Application/ApplicationImplementation"
			&& rule.To == "Ports/PortInterfaces");
		snapshot.Rules.Should().Contain(rule => rule.From == "*" && rule.To == "Crosscutting" && rule.AppliesToDescendants);
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

		snapshot.Layers.Should().ContainSingle().Which.Path.Should().Be("Controller");
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

		snapshot.Layers.Select(layer => layer.Path).Should().Contain(["Presentation", "Application", "Persistence"]);
		snapshot.Layers.Single(layer => layer.Path == "Application").SourcePath.Should().Be(Path.GetFullPath(includedPath));
		snapshot.Rules.Should().Contain(rule => rule.From == "Application" && rule.To == "Persistence");
		snapshot.Rules.Should().Contain(rule => rule.From == "Presentation" && rule.To == "Application");
	}

	[Fact]
	public void Load_ExpandsWildcardIncludedAnlFilesIntoGraphSnapshot()
	{
		var directory = Path.Combine(Path.GetTempPath(), "AnaalIJzerGraphXmlSnapshotLoaderTests", Guid.NewGuid().ToString("N"));
		var pluginsDirectory = Path.Combine(directory, "RulePlugins");
		Directory.CreateDirectory(pluginsDirectory);

		var layersPath = Path.Combine(pluginsDirectory, "RestaurantLayers.anl");
		File.WriteAllText(
			layersPath,
			"""
			<ArchitecturalLevels>
			  <Layer name="Waiter">
			    <Class endsWith="Waiter" />
			  </Layer>
			  <Layer name="Chef">
			    <Class endsWith="Chef" />
			  </Layer>
			  <Layer name="Pantry">
			    <Class endsWith="Pantry" />
			  </Layer>
			</ArchitecturalLevels>
			""",
			Encoding.Unicode);

		var flowPath = Path.Combine(pluginsDirectory, "RestaurantFlow.anl");
		File.WriteAllText(
			flowPath,
			"""
			<ArchitecturalLevels>
			  <AllowedDependency from="Waiter" to="Chef" />
			  <AllowedDependency from="Chef" to="Pantry" />
			</ArchitecturalLevels>
			""",
			Encoding.Unicode);

		var rootPath = Path.Combine(directory, "Architecture.anl");
		File.WriteAllText(
			rootPath,
			"""
			<ArchitecturalLevels>
			  <Include path="*.anl" />
			</ArchitecturalLevels>
			""",
			Encoding.Unicode);

		var snapshot = ArchitectureGraphXmlSnapshotLoader.Load(rootPath);

		snapshot.Layers.Select(layer => layer.Path).Should().Contain(["Waiter", "Chef", "Pantry"]);
		snapshot.Layers.Single(layer => layer.Path == "Waiter").SourcePath.Should().Be(Path.GetFullPath(layersPath));
		snapshot.Rules.Should().Contain(rule => rule.From == "Waiter" && rule.To == "Chef");
		snapshot.Rules.Should().Contain(rule => rule.From == "Chef" && rule.To == "Pantry");
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
		snapshot.Layers.Select(layer => layer.Path).Should().Contain(["Controller", "Application"]);
		snapshot.Rules.Should().ContainSingle(rule => rule.From == "Controller" && rule.To == "Application");
	}

	[Fact]
	public void Load_ReadsExceptionPolicyReviewsFromXmlConfiguration()
	{
		var expiringSoonDate = DateTime.Today.AddDays(5).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		var path = WriteTempFile(
			$"""
			<ArchitecturalLevels>
			  <ExceptionPolicy requireReason="true" requireOwner="true" requireExpiresOn="true" warnBeforeDays="14" />
			  <Layer name="Kitchen">
			    <Class endsWith="Kitchen">
			      <Exceptions>
			        <Class typeName="OutdoorKitchen" />
			        <Class typeName="SoonKitchen" reason="Temporary exception" owner="Kitchen team" expiresOn="{expiringSoonDate}" />
			      </Exceptions>
			    </Class>
			  </Layer>
			</ArchitecturalLevels>
			""");

		var snapshot = ArchitectureGraphXmlSnapshotLoader.Load(path);

		snapshot.ExceptionReviews.Should().HaveCount(2);
		snapshot.ExceptionReviews.Should().Contain(review =>
			review.OwnerLayerPath == "Kitchen"
			&& review.Status == "Invalid"
			&& review.MatcherKind == "Class"
			&& review.MatcherLabel == "typeName=\"OutdoorKitchen\"");
		snapshot.ExceptionReviews.Should().Contain(review =>
			review.OwnerLayerPath == "Kitchen"
			&& review.Status == "ExpiringSoon"
			&& review.MatcherLabel == "typeName=\"SoonKitchen\"");
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
