using System.Text;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using AwesomeAssertions;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using Xunit;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Tests.Document;

public sealed class ArchitectureConfigurationFileServiceTests
{
	[Fact]
	public async Task FormatAsync_RewritesConfigurationToUtf8WithoutBom()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		using var directory = new TemporaryDirectory();
		var inputPath = directory.WriteFile(
			"Architecture.anl",
			"""
			<?xml version="1.0" encoding="utf-16"?>
			<ArchitecturalLevels><Layer name="Customer"><Class endsWith="Customer" /></Layer></ArchitecturalLevels>
			""",
			new UTF8Encoding(true));
		var outputPath = directory.GetPath("Formatted.anl");

		await ArchitectureConfigurationFileService.FormatAsync(inputPath, outputPath, force: false, cancellationToken);

		var bytes = await File.ReadAllBytesAsync(outputPath, cancellationToken);
		bytes.Take(3).Should().NotEqual([0xEF, 0xBB, 0xBF]);
		(await File.ReadAllTextAsync(outputPath, cancellationToken)).Should().Contain("encoding=\"utf-8\"");
		(await File.ReadAllTextAsync(outputPath, cancellationToken)).Should().Contain("<Layer name=\"Customer\">");
	}

	[Fact]
	public async Task MergeAsync_WritesSingleConfigurationContainingAllLayers()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
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

		await ArchitectureConfigurationFileService.MergeAsync([customerPath, waiterPath], outputPath, force: false, cancellationToken);

		var content = await File.ReadAllTextAsync(outputPath, cancellationToken);
		content.Should().Contain("<ArchitecturalLevels");
		content.Should().Contain("description=\"Restaurant rules\"");
		content.Should().Contain("requireRecognizedDependencies=\"Constructor\"");
		content.Should().Contain("<Layer name=\"Customer\">");
		content.Should().Contain("<Layer name=\"Waiter\">");
	}

	[Fact]
	public async Task MergeAsync_FlattensWildcardIncludes()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		using var directory = new TemporaryDirectory();
		_ = directory.WriteFile(
			"Architecture.anl",
			"""
			<ArchitecturalLevels>
			  <Include path="*.anl" />
			</ArchitecturalLevels>
			""");
		_ = directory.WriteFile(
			Path.Combine("RulePlugins", "RestaurantLayers.anl"),
			"""
			<ArchitecturalLevels>
			  <Layer name="Waiter"><Class endsWith="Waiter" /></Layer>
			  <Layer name="Chef"><Class endsWith="Chef" /></Layer>
			</ArchitecturalLevels>
			""");
		_ = directory.WriteFile(
			Path.Combine("RulePlugins", "RestaurantFlow.anl"),
			"""
			<ArchitecturalLevels>
			  <AllowedDependency from="Waiter" to="Chef" />
			</ArchitecturalLevels>
			""");
		var outputPath = directory.GetPath("Merged.anl");

		await ArchitectureConfigurationFileService.MergeAsync([directory.GetPath("Architecture.anl")], outputPath, force: false, cancellationToken);

		var content = await File.ReadAllTextAsync(outputPath, cancellationToken);
		content.Should().Contain("<Layer name=\"Waiter\">");
		content.Should().Contain("<Layer name=\"Chef\">");
		content.Should().Contain("<AllowedDependency from=\"Waiter\" to=\"Chef\" />");
		content.Should().NotContain("<Include path=\"*.anl\" />");
	}

	[Fact]
	public async Task SplitAsync_WritesManifestSharedRulesAndGraphFiles()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
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

		var graphCount = await ArchitectureConfigurationFileService.SplitAsync(inputPath, outputDirectory, force: false, cancellationToken);

		graphCount.Should().Be(2);

		var manifestPath = Path.Combine(outputDirectory, ArchitectureConfigurationDocumentLoader.ConfigFileName);
		File.Exists(manifestPath).Should().BeTrue();
		File.Exists(Path.Combine(outputDirectory, "Shared.anl")).Should().BeTrue();

		var graphFiles = Directory.GetFiles(outputDirectory, "Graph.*.anl");
		graphFiles.Should().HaveCount(2);

		var manifestContent = await File.ReadAllTextAsync(manifestPath, cancellationToken);
		manifestContent.Should().Contain("<Include path=\"Shared.anl\" />");
		manifestContent.Should().Contain("Graph.01");
		manifestContent.Should().Contain("Graph.02");

		var graphContents = await Task.WhenAll(graphFiles.Select(path => File.ReadAllTextAsync(path, cancellationToken)));
		var combinedGraphContent = string.Join(Environment.NewLine, graphContents);
		combinedGraphContent.Should().Contain("<Layer name=\"Customer\">");
		combinedGraphContent.Should().Contain("<Layer name=\"Waiter\">");
		combinedGraphContent.Should().Contain("<Layer name=\"Chef\">");
		combinedGraphContent.Should().Contain("<Layer name=\"Pantry\">");
	}

	private sealed class TemporaryDirectory : IDisposable
	{
		private readonly string _path = Path.Combine(Path.GetTempPath(), "AnaalIJzerConfigFileServiceTests", Guid.NewGuid().ToString("N"));

		public string WriteFile(string fileName, string content, Encoding? encoding = null)
		{
			Directory.CreateDirectory(_path);
			var filePath = Path.Combine(_path, fileName);
			Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
			File.WriteAllText(filePath, content, encoding ?? new UTF8Encoding(false));

			return filePath;
		}

		public string GetPath(string fileNameOrDirectoryName)
		{
			Directory.CreateDirectory(_path);
			var result = Path.Combine(_path, fileNameOrDirectoryName);

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
