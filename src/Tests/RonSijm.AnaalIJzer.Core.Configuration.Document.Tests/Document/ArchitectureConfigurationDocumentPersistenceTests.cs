using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Persistence;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Tests.Document;

public sealed class ArchitectureConfigurationDocumentPersistenceTests : IDisposable
{
	private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "AnaalIJzer-CoreConfigDocTests", Guid.NewGuid().ToString("N"));

	public ArchitectureConfigurationDocumentPersistenceTests()
	{
		Directory.CreateDirectory(_tempDirectory);
	}

	[Fact]
	public void CreateXmlFile_CreatesArchitectureDocument()
	{
		var path = Path.Combine(_tempDirectory, "Architecture.anl");

		var result = ArchitectureConfigurationDocumentPersistence.CreateXmlFile(path);

		result.Succeeded.Should().BeTrue();
		File.Exists(path).Should().BeTrue();

		var document = XDocument.Load(path);
		document.Root?.Name.LocalName.Should().Be("ArchitecturalLevels");
	}

	[Fact]
	public void ReadConfiguration_ReadsInlineAssemblyMetadataDocument()
	{
		var path = Path.Combine(_tempDirectory, "AnaalIJzerSettings.cs");
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
		document.Root?.Element("Layer")?.Attribute("name")?.Value.Should().Be("Chef");
	}

	[Fact]
	public void EditConfiguration_InlineAssemblyMetadata_PreservesNameofInterpolation()
	{
		var path = Path.Combine(_tempDirectory, "AnaalIJzerSettings.cs");
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

	[Fact]
	public void EditConfiguration_InlineAssemblyMetadata_NormalizesWhitespaceAfterAppendingAnElement()
	{
		var path = Path.Combine(_tempDirectory, "AnaalIJzerSettings.cs");
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

		var result = ArchitectureConfigurationDocumentPersistence.EditConfiguration(
			new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, path),
			document =>
			{
				document.Root!.Add(new XElement("AllowedDependency", new XAttribute("from", "Chef"), new XAttribute("to", "Pantry")));
				return ArchitectureConfigurationDocumentOperationResult.Success("Added a dependency.");
			});

		result.Succeeded.Should().BeTrue();

		var updatedSource = File.ReadAllText(path);
		updatedSource.Should().Contain("<AllowedDependency from=\"Chef\" to=\"Pantry\" />" + Environment.NewLine + "</ArchitecturalLevels>");
		InlineAssemblyMetadataSettings.TryFindInlineSettings(updatedSource, out var settings, out var message).Should().BeTrue(message);
		XDocument.Parse(settings.Xml).Root!.Element("AllowedDependency")!.Attribute("to")!.Value.Should().Be("Pantry");
	}

	public void Dispose()
	{
		if (!Directory.Exists(_tempDirectory))
		{
			return;
		}

		Directory.Delete(_tempDirectory, recursive: true);
	}
}
