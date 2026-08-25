using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Outputs.GraphExports;

namespace RonSijm.AnaalIJzer.Outputs.Tests.GraphExports;

public sealed class ArchitectureGraphImageExportServiceTests
{
	[Fact]
	public async Task ExportAsync_SingleMode_ExportsLoadedGraph()
	{
		var service = new ArchitectureGraphImageExportService();
		var tempDirectory = Path.Combine(Path.GetTempPath(), "AnaalIJzer-graph-export-single-" + Guid.NewGuid().ToString("N"));
		var outputPath = Path.Combine(tempDirectory, "graph.png");
		var request = new ArchitectureGraphImageExportRequest(ArchitectureGraphImageExportMode.Single, "input.csproj", outputPath, failOnError: true);
		var loader = new FakeLoader(_ => Task.FromResult(CreateSnapshot()));
		var renderer = new FakeRenderer();

		var result = await service.ExportAsync(request, loader, renderer, TestContext.Current.CancellationToken);

		result.ExitCode.Should().Be(0);
		result.SuccessCount.Should().Be(1);
		result.PlaceholderCount.Should().Be(0);
		result.Files.Should().ContainSingle();
		File.Exists(outputPath).Should().BeTrue();
		renderer.ExportedOutputs.Should().Contain(outputPath);
	}

	[Theory]
	[InlineData(false, 0)]
	[InlineData(true, 1)]
	public async Task ExportAsync_ExamplesMode_WritesFlatExampleOutputsAndHonorsFailOnError(bool failOnError, int expectedExitCode)
	{
		var service = new ArchitectureGraphImageExportService();
		var tempDirectory = Path.Combine(Path.GetTempPath(), "AnaalIJzer-graph-export-examples-" + Guid.NewGuid().ToString("N"));
		var examplesRoot = Path.Combine(tempDirectory, "Examples");
		var outputDirectory = Path.Combine(tempDirectory, "Images");
		Directory.CreateDirectory(Path.Combine(examplesRoot, "Feature.One"));
		Directory.CreateDirectory(Path.Combine(examplesRoot, "Feature.Two"));
		var firstProject = Path.Combine(examplesRoot, "Feature.One", "Feature.One.csproj");
		var secondProject = Path.Combine(examplesRoot, "Feature.Two", "Feature.Two.csproj");
		await File.WriteAllTextAsync(firstProject, "<Project />", TestContext.Current.CancellationToken);
		await File.WriteAllTextAsync(secondProject, "<Project />", TestContext.Current.CancellationToken);
		var request = new ArchitectureGraphImageExportRequest(ArchitectureGraphImageExportMode.Examples, examplesRoot, outputDirectory, failOnError);
		var loader = new FakeLoader(path =>
		{
			var task = path.EndsWith("Feature.Two.csproj", StringComparison.OrdinalIgnoreCase)
				? Task.FromException<ArchitectureGraphSnapshot>(new InvalidOperationException("No graph for Feature.Two"))
				: Task.FromResult(CreateSnapshot());

			return task;
		});
		var renderer = new FakeRenderer();

		var result = await service.ExportAsync(request, loader, renderer, TestContext.Current.CancellationToken);

		result.ExitCode.Should().Be(expectedExitCode);
		result.SuccessCount.Should().Be(1);
		result.PlaceholderCount.Should().Be(1);
		result.Files.Should().HaveCount(2);
		File.Exists(Path.Combine(outputDirectory, "Feature.One-Graph.png")).Should().BeTrue();
		File.Exists(Path.Combine(outputDirectory, "Feature.Two-Graph.png")).Should().BeTrue();
		renderer.ExportedOutputs.Should().Contain(Path.Combine(outputDirectory, "Feature.One-Graph.png"));
		renderer.PlaceholderOutputs.Should().Contain(Path.Combine(outputDirectory, "Feature.Two-Graph.png"));
	}

	private static ArchitectureGraphSnapshot CreateSnapshot()
	{
		var snapshot = new ArchitectureGraphSnapshot(
			hasConfiguration: true,
			hasConfigurationIssues: false,
			layers: ImmutableArray<ArchitectureGraphLayer>.Empty,
			rules: ImmutableArray<ArchitectureGraphRule>.Empty,
			activeLayerPaths: ImmutableArray<string>.Empty,
			configurationIssueMessages: ImmutableArray<string>.Empty);

		return snapshot;
	}

	private sealed class FakeLoader(Func<string, Task<ArchitectureGraphSnapshot>> loadAsync) : IArchitectureGraphSnapshotLoader
	{
		public Task<ArchitectureGraphSnapshot> LoadAsync(string inputPath, CancellationToken cancellationToken)
		{
			var result = loadAsync(inputPath);

			return result;
		}
	}

	private sealed class FakeRenderer : IArchitectureGraphImageRenderer
	{
		public List<string> ExportedOutputs { get; } = [];

		public List<string> PlaceholderOutputs { get; } = [];

		public void ExportGraph(ArchitectureGraphSnapshot snapshot, string outputPath)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
			File.WriteAllText(outputPath, "graph");
			ExportedOutputs.Add(outputPath);
		}

		public void ExportPlaceholder(string outputPath, string title, string message)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
			File.WriteAllText(outputPath, "placeholder:" + title + ":" + message);
			PlaceholderOutputs.Add(outputPath);
		}
	}
}
