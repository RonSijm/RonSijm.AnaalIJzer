using System.Text;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using AwesomeAssertions;
using Xunit;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Tests.Document;

public sealed class ArchitectureConfigurationFileServiceTests
{
	[Fact]
	public async Task FormatAsync_RewritesConfigurationToUtf8WithoutBom()
	{
		using var directory = new TemporaryDirectory();
		var inputPath = directory.WriteFile(
			"Architecture.anl",
			"""
			<?xml version="1.0" encoding="utf-16"?>
			<ArchitecturalLevels><Layer name="Customer"><Class endsWith="Customer" /></Layer></ArchitecturalLevels>
			""",
			new UTF8Encoding(true));
		var outputPath = directory.GetPath("Formatted.anl");

		await ArchitectureConfigurationFileService.FormatAsync(inputPath, outputPath, force: false, CancellationToken.None);

		var bytes = File.ReadAllBytes(outputPath);
		bytes.Take(3).Should().NotEqual([0xEF, 0xBB, 0xBF]);
		File.ReadAllText(outputPath).Should().Contain("encoding=\"utf-8\"");
		File.ReadAllText(outputPath).Should().Contain("<Layer name=\"Customer\">");
	}

	[Fact]
	public async Task MergeAsync_WritesSingleConfigurationContainingAllLayers()
	{
		using var directory = new TemporaryDirectory();
		var customerPath = directory.WriteFile(
			"Customers.anl",
			"""
			<ArchitecturalLevels description="Restaurant rules">
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			</ArchitecturalLevels>
			""");
		var waiterPath = directory.WriteFile(
			"Waiters.anl",
			"""
			<ArchitecturalLevels requireRecognizedDependencies="Constructor">
			  <Layer name="Waiter"><Class endsWith="Waiter" /></Layer>
			</ArchitecturalLevels>
			""");
		var outputPath = directory.GetPath("Merged.anl");

		await ArchitectureConfigurationFileService.MergeAsync([customerPath, waiterPath], outputPath, force: false, CancellationToken.None);

		var content = File.ReadAllText(outputPath);
		content.Should().Contain("<ArchitecturalLevels");
		content.Should().Contain("description=\"Restaurant rules\"");
		content.Should().Contain("requireRecognizedDependencies=\"Constructor\"");
		content.Should().Contain("<Layer name=\"Customer\">");
		content.Should().Contain("<Layer name=\"Waiter\">");
	}

	[Fact]
	public async Task SplitAsync_WritesManifestSharedRulesAndGraphFiles()
	{
		using var directory = new TemporaryDirectory();
		var inputPath = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels description="Restaurant rules">
			  <Forbidden>
			    <Class endsWith="Legacy" />
			  </Forbidden>
			  <Layer name="Customer"><Class endsWith="Customer" /></Layer>
			  <Layer name="Waiter"><Class endsWith="Waiter" /></Layer>
			  <AllowedDependency from="Customer" to="Waiter" />
			  <Layer name="Chef"><Class endsWith="Chef" /></Layer>
			  <Layer name="Pantry"><Class endsWith="Pantry" /></Layer>
			  <AllowedDependency from="Chef" to="Pantry" />
			</ArchitecturalLevels>
			""");
		var outputDirectory = directory.GetPath("Split");

		var graphCount = await ArchitectureConfigurationFileService.SplitAsync(inputPath, outputDirectory, force: false, CancellationToken.None);

		graphCount.Should().Be(2);

		var manifestPath = Path.Combine(outputDirectory, ArchitectureConfigurationDocumentLoader.ConfigFileName);
		File.Exists(manifestPath).Should().BeTrue();
		File.Exists(Path.Combine(outputDirectory, "Shared.anl")).Should().BeTrue();

		var graphFiles = Directory.GetFiles(outputDirectory, "Graph.*.anl");
		graphFiles.Should().HaveCount(2);

		var manifestContent = File.ReadAllText(manifestPath);
		manifestContent.Should().Contain("<Include path=\"Shared.anl\" />");
		manifestContent.Should().Contain("Graph.01");
		manifestContent.Should().Contain("Graph.02");

		var combinedGraphContent = string.Join(Environment.NewLine, graphFiles.Select(File.ReadAllText));
		combinedGraphContent.Should().Contain("<Layer name=\"Customer\">");
		combinedGraphContent.Should().Contain("<Layer name=\"Waiter\">");
		combinedGraphContent.Should().Contain("<Layer name=\"Chef\">");
		combinedGraphContent.Should().Contain("<Layer name=\"Pantry\">");
	}

	private sealed class TemporaryDirectory : IDisposable
	{
		private readonly string path = Path.Combine(Path.GetTempPath(), "AnaalIJzerConfigFileServiceTests", Guid.NewGuid().ToString("N"));

		public string WriteFile(string fileName, string content, Encoding? encoding = null)
		{
			Directory.CreateDirectory(path);
			var filePath = Path.Combine(path, fileName);
			File.WriteAllText(filePath, content, encoding ?? new UTF8Encoding(false));

			return filePath;
		}

		public string GetPath(string fileNameOrDirectoryName)
		{
			Directory.CreateDirectory(path);
			var result = Path.Combine(path, fileNameOrDirectoryName);

			return result;
		}

		public void Dispose()
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
			}
		}
	}
}
