using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RonSijm.AnaalIJzer.Workspace.Analysis;

namespace RonSijm.AnaalIJzer.Application.Tests.ApplicationOperations;

public sealed partial class ApplicationOperationsTests
{
	[Fact]
	public async Task ApplicationRunner_GeneratesCleanConfigurationFromRealProject()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-generate-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var repositoryRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FindSchemaPath())!, "..", "..", "..", ".."));
			var projectPath = Path.Combine(repositoryRoot, "Examples", "Scenarios", "Example.RepositoryQuerySurface", "Example.RepositoryQuerySurface.csproj");
			var outputPath = Path.Combine(tempDirectory, "Architecture.anl");
			var result = await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.GenerateConfig)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				OutputPath = outputPath,
				GenerateDocumentation = true,
				IncludeDocumentationInput = true
			}, cancellationToken);

			result.Message.Should().Contain("Generated configuration");
			result.Message.Should().Contain("Generated code-backed documentation");
			File.Exists(Path.Combine(tempDirectory, "AnaalIJzer.xsd")).Should().BeTrue();
			var documentation = await File.ReadAllTextAsync(Path.Combine(tempDirectory, "architecture-documentation.md"), cancellationToken);
			documentation.Should().Contain("## Code Evidence");
			documentation.Should().Contain("## Input Configuration");
			documentation.Should().Contain("This documentation was generated from the following architecture configuration");
			documentation.Should().Contain("<ArchitecturalLevels");
			documentation.Should().Contain("OrderEndpoint");
			documentation.Should().Contain("The analyzer reports no violations");
			var configuration = XDocument.Load(outputPath);
			AssertValid(configuration, Path.Combine(tempDirectory, "AnaalIJzer.xsd"));
			configuration.Root!.Elements("Layer").Should().NotBeEmpty();
			configuration.Root.Elements("AllowedDependency").Should().NotBeEmpty();
			configuration.Root.Elements("AllowedDependency").Should().OnlyContain(element => element.Attribute("allowedSites") != null);
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public async Task ConfigurationGenerator_GeneratesHelpfulConfigurationFromSolution()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-generate-solution-test-{Guid.NewGuid():N}");
		var solution = CreateSolutionAnalysisResult(tempDirectory);
		var configurationText = ApplicationConfigurationGenerator.Generate(solution, "AnaalIJzer.xsd", new ConfigurationGenerationOptions
		{
			Strategy = ConfigurationGenerationStrategy.Helpful
		}, cancellationToken);
		var configuration = XDocument.Parse(configurationText);

		AssertValid(configuration, FindSchemaPath());
		configuration.Root!.Attribute("description")!.Value.Should().Contain("helpful solution baseline");
		configuration.Root.Elements("Layer").Should().ContainSingle(layer =>
			layer.Attribute("name")!.Value == "Shop.Contracts"
			&& layer.Element("Assembly")!.Attribute("exactName")!.Value == "Shop.Contracts");
		configuration.Root.Elements("Layer").Should().ContainSingle(layer =>
			layer.Attribute("name")!.Value == "Shop.Application"
			&& layer.Element("Assembly")!.Attribute("exactName")!.Value == "Shop.Application");
		configuration.Root.Elements("AllowedDependency").Should().ContainSingle(edge =>
			edge.Attribute("from")!.Value == "Shop.Application"
			&& edge.Attribute("to")!.Value == "Shop.Contracts"
			&& edge.Attribute("allowedSites")!.Value == "Constructor");

		var diagnostics = await ApplicationConfigurationGenerator.ValidateAsync(solution, configurationText, Path.Combine(tempDirectory, "Architecture.anl"), cancellationToken);
		diagnostics.Should().BeEmpty();
	}

	private static SolutionAnalysisResult CreateSolutionAnalysisResult(string solutionDirectory)
	{
		var contractsCompilation = CSharpCompilation.Create(
			"Shop.Contracts",
			[CSharpSyntaxTree.ParseText("""
			                             namespace Shop.Contracts
			                             {
			                                 public sealed class OrderContract { }
			                             }
			                             """)],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		using var contractsAssembly = new MemoryStream();
		var emitResult = contractsCompilation.Emit(contractsAssembly);
		emitResult.Success.Should().BeTrue(string.Join(Environment.NewLine, emitResult.Diagnostics));
		var applicationCompilation = CSharpCompilation.Create(
			"Shop.Application",
			[CSharpSyntaxTree.ParseText("""
			                             namespace Shop.Application
			                             {
			                                 public sealed class OrderService
			                                 {
			                                     public OrderService(Shop.Contracts.OrderContract contract) { }
			                                 }
			                             }
			                             """)],
			[
				MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
				MetadataReference.CreateFromImage(contractsAssembly.ToArray())
			],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		var contractsProject = CreateProjectAnalysisResult(solutionDirectory, "Shop.Contracts", contractsCompilation);
		var contractsSecondTargetProject = CreateProjectAnalysisResult(solutionDirectory, "Shop.Contracts", contractsCompilation);
		var applicationProject = CreateProjectAnalysisResult(solutionDirectory, "Shop.Application", applicationCompilation);
		var result = new SolutionAnalysisResult(
			Path.Combine(solutionDirectory, "ExampleSolution.slnx"),
			solutionDirectory,
			"ExampleSolution",
			[contractsProject, contractsSecondTargetProject, applicationProject],
			ImmutableArray<string>.Empty);

		return result;
	}

	private static ProjectAnalysisResult CreateProjectAnalysisResult(string solutionDirectory, string assemblyName, Compilation compilation)
	{
		var projectDirectory = Path.Combine(solutionDirectory, assemblyName);
		var result = new ProjectAnalysisResult(
			Path.Combine(projectDirectory, assemblyName + ".csproj"),
			projectDirectory,
			assemblyName,
			compilation,
			default,
			null,
			null,
			null,
			null,
			ImmutableArray<Diagnostic>.Empty,
			ImmutableArray<string>.Empty,
			ImmutableArray<string>.Empty);

		return result;
	}
}

