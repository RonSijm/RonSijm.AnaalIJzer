using System.Xml.Linq;
using AwesomeAssertions;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Tests.Document;

public sealed class ArchitectureConfigurationDocumentPersistenceTests : IDisposable
{
	private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), "AnaalIJzer-CoreConfigDocTests", Guid.NewGuid().ToString("N"));

	public ArchitectureConfigurationDocumentPersistenceTests()
	{
		Directory.CreateDirectory(tempDirectory);
	}

	[Fact]
	public void CreateXmlFile_CreatesArchitectureDocument()
	{
		var path = Path.Combine(tempDirectory, "Architecture.anl");

		var result = ArchitectureConfigurationDocumentPersistence.CreateXmlFile(path);

		result.Succeeded.Should().BeTrue();
		File.Exists(path).Should().BeTrue();

		var document = XDocument.Load(path);
		document.Root?.Name.LocalName.Should().Be("ArchitecturalLevels");
	}

	[Fact]
	public void ReadConfiguration_ReadsInlineAssemblyMetadataDocument()
	{
		var path = Path.Combine(tempDirectory, "AnaalIJzerSettings.cs");
		File.WriteAllText(
			path,
			""""
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", """
			<ArchitecturalLevels>
			  <Layer name="Chef" />
			</ArchitecturalLevels>
			""")]
			"""");

		var result = ArchitectureConfigurationDocumentPersistence.ReadConfiguration(
			new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, path),
			out var document);

		result.Succeeded.Should().BeTrue();
		document.Should().NotBeNull();
		document!.Root?.Element("Layer")?.Attribute("name")?.Value.Should().Be("Chef");
	}

	[Fact]
	public void EditConfiguration_InlineAssemblyMetadata_PreservesNameofInterpolation()
	{
		var path = Path.Combine(tempDirectory, "AnaalIJzerSettings.cs");
		File.WriteAllText(
			path,
			""""
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", $"""
			<ArchitecturalLevels>
			  <Layer name="{nameof(Chef)}" />
			</ArchitecturalLevels>
			""")]
			public class Chef { }
			"""");

		var result = ArchitectureConfigurationDocumentPersistence.EditConfiguration(
			new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, path),
			document =>
			{
				document.Root!.Element("Layer")!.SetAttributeValue("description", "Prepares the food.");
				return ArchitectureConfigurationDocumentOperationResult.Success("Updated inline config.");
			});

		result.Succeeded.Should().BeTrue();

		var updatedSource = File.ReadAllText(path);
		updatedSource.Should().Contain("name=\"{nameof(Chef)}\"");
		updatedSource.Should().Contain("description=\"Prepares the food.\"");
	}

	public void Dispose()
	{
		if (!Directory.Exists(tempDirectory))
		{
			return;
		}

		Directory.Delete(tempDirectory, recursive: true);
	}
}
