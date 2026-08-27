using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;
using RonSijm.AnaalIJzer.EditorRuntime.Editor.Snapshots;

namespace RonSijm.AnaalIJzer.EditorRuntime.Tests.Editor.Snapshots;
public sealed partial class EditorSnapshotTests
{
	private static async Task<ArchitectureEditorSnapshot> CreateSnapshotAsync(string source, string? config = null, string fileName = "Test.cs", bool includeProjectEvidence = false, string? projectPath = null)
	{
		using var workspace = new AdhocWorkspace();

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
			.AddDocument(documentId, fileName, SourceText.From(source), filePath: fileName);

		workspace.TryApplyChanges(solution).Should().BeTrue();

		var document = workspace.CurrentSolution.GetDocument(documentId)!;
		var additionalFiles = config is null
			? ImmutableArray<AdditionalText>.Empty
			: ImmutableArray.Create<AdditionalText>(new TestAdditionalText("Architecture.anl", config));
		var result = await ArchitectureEditorSnapshotService.CreateSnapshotAsync(document, additionalFiles, includeProjectEvidence);

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
			typeof(Lazy<>).Assembly,
			typeof(System.Reflection.AssemblyMetadataAttribute).Assembly
		}
			.Distinct()
			.Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
			.ToArray<MetadataReference>();

		return frameworkResult;
	}

	private sealed class TestAdditionalText(string path, string content) : AdditionalText
	{
		private readonly SourceText _text = SourceText.From(content);

		public override string Path { get; } = path;

		public override SourceText GetText(CancellationToken cancellationToken = default)
		{
			var result = _text;

			return result;
		}
	}
}
