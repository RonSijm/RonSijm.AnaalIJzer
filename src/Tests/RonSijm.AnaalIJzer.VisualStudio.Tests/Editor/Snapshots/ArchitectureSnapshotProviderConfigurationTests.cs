using System.IO;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.VisualStudio.Editor.Snapshots;
using Xunit;

namespace RonSijm.AnaalIJzer.VisualStudio.Tests.Editor.Snapshots;

public sealed class ArchitectureSnapshotProviderConfigurationTests
{
	private static readonly MetadataReference[] BasicReferences = CreateBasicReferences();

	[Fact]
	public async Task ResolveAdditionalFilesAsync_SkipsNearestFallbackWhenProjectUsesInlineSettings()
	{
		var rootDirectory = CreateTemporaryDirectory();
		var rootConfigPath = Path.Combine(rootDirectory, "Architecture.anl");
		var projectDirectory = Path.Combine(rootDirectory, "Examples", "Diagnostics", "Example.Arch001.NoEdge");
		var projectPath = Path.Combine(projectDirectory, "Example.Arch001.NoEdge.csproj");
		var documentPath = Path.Combine(projectDirectory, "Example.cs");
		Directory.CreateDirectory(projectDirectory);
		File.WriteAllText(rootConfigPath, "<ArchitecturalLevels><Layer name=\"Root\"><Class typeName=\"RootType\" /></Layer></ArchitecturalLevels>");

		const string source = """"
		                      using System.Reflection;

		                      [assembly: AssemblyMetadata("AnaalIJzerSettings", """
		                      <ArchitecturalLevels>
		                        <Layer name="Waiter">
		                          <Class endsWith="Waiter" />
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """)]

		                      public class TableWaiter { }
		                      """";

		using var workspace = new AdhocWorkspace();
		var document = CreateDocument(workspace, source, documentPath, projectPath);

		var result = await ArchitectureSnapshotProvider.ResolveAdditionalFilesAsync(document, documentPath, TestContext.Current.CancellationToken);

		result.Should().BeEmpty("inline example settings should take precedence over a repo-root Architecture.anl fallback");
	}

	[Fact]
	public async Task ResolveAdditionalFilesAsync_UsesNearestFallbackWhenProjectHasNoOwnConfiguration()
	{
		var rootDirectory = CreateTemporaryDirectory();
		var rootConfigPath = Path.Combine(rootDirectory, "Architecture.anl");
		var projectDirectory = Path.Combine(rootDirectory, "Examples", "Diagnostics", "Example.Arch001.NoEdge");
		var projectPath = Path.Combine(projectDirectory, "Example.Arch001.NoEdge.csproj");
		var documentPath = Path.Combine(projectDirectory, "Example.cs");
		Directory.CreateDirectory(projectDirectory);
		File.WriteAllText(rootConfigPath, "<ArchitecturalLevels><Layer name=\"Root\"><Class typeName=\"RootType\" /></Layer></ArchitecturalLevels>");

		const string source = "public class TableWaiter { }";

		using var workspace = new AdhocWorkspace();
		var document = CreateDocument(workspace, source, documentPath, projectPath);

		var result = await ArchitectureSnapshotProvider.ResolveAdditionalFilesAsync(document, documentPath, TestContext.Current.CancellationToken);

		result.Should().ContainSingle().Which.Path.Should().Be(rootConfigPath);
	}

	private static Document CreateDocument(AdhocWorkspace workspace, string source, string documentPath, string projectPath)
	{
		var projectId = ProjectId.CreateNewId();
		var documentId = DocumentId.CreateNewId(projectId);
		var projectInfo = ProjectInfo.Create(
			projectId,
			VersionStamp.Create(),
			"TestProject",
			"TestProject",
			LanguageNames.CSharp,
			filePath: projectPath,
			parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
			compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var solution = workspace.CurrentSolution
			.AddProject(projectInfo)
			.AddMetadataReferences(projectId, BasicReferences)
			.AddDocument(documentId, Path.GetFileName(documentPath), SourceText.From(source), filePath: documentPath);

		workspace.TryApplyChanges(solution).Should().BeTrue();
		var result = workspace.CurrentSolution.GetDocument(documentId)!;

		return result;
	}

	private static MetadataReference[] CreateBasicReferences()
	{
		var trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
		if (!string.IsNullOrWhiteSpace(trustedPlatformAssemblies))
		{
			var trustedResult = trustedPlatformAssemblies!
				.Split(Path.PathSeparator)
				.Select(path => MetadataReference.CreateFromFile(path))
				.ToArray<MetadataReference>();

			return trustedResult;
		}

		var frameworkResult = new[]
		{
			typeof(object).Assembly,
			typeof(Enumerable).Assembly,
			typeof(System.Reflection.AssemblyMetadataAttribute).Assembly
		}
			.Distinct()
			.Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
			.ToArray<MetadataReference>();

		return frameworkResult;
	}

	private static string CreateTemporaryDirectory()
	{
		var result = Path.Combine(Path.GetTempPath(), "AnaalIJzerVisualStudioTests", Guid.NewGuid().ToString("N"));

		return result;
	}
}
