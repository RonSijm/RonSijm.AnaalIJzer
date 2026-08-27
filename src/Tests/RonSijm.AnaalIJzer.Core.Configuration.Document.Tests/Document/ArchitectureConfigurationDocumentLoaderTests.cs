using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Tests.Document;

public sealed class ArchitectureConfigurationDocumentLoaderTests
{
	[Fact]
	public void FindConfigurationFile_ReturnsArchitectureAnl()
	{
		var additionalFiles = ImmutableArray.Create<AdditionalText>(
			new TestAdditionalText("Other.anl", "<ArchitecturalLevels />"),
			new TestAdditionalText("Architecture.anl", "<ArchitecturalLevels />"));

		var result = ArchitectureConfigurationDocumentLoader.FindConfigurationFile(additionalFiles);

		result.Should().NotBeNull();
		result.Path.Should().EndWith("Architecture.anl");
	}

	[Fact]
	public void FindConfigurationFile_ReturnsArchitectureAnl_ForWindowsStylePath()
	{
		var additionalFiles = ImmutableArray.Create<AdditionalText>(
			new TestAdditionalText(@"D:\repo\config\Other.anl", "<ArchitecturalLevels />"),
			new TestAdditionalText(@"D:\repo\config\Architecture.anl", "<ArchitecturalLevels />"));

		var result = ArchitectureConfigurationDocumentLoader.FindConfigurationFile(additionalFiles);

		result.Should().NotBeNull();
		result.Path.Should().Be(@"D:\repo\config\Architecture.anl");
	}

	[Fact]
	public void TryReadAnalyzerConfigurationText_PrefersAdditionalFileOverInlineMetadata()
	{
		var compilation = CreateCompilation("""
		                                  using System.Reflection;

		                                  [assembly: AssemblyMetadata("AnaalIJzerSettings", "<ArchitecturalLevels><Layer name=\"Inline\" /></ArchitecturalLevels>")]
		                                  public class Example { }
		                                  """);
		var additionalFiles = ImmutableArray.Create<AdditionalText>(
			new TestAdditionalText("Architecture.anl", "<ArchitecturalLevels><Layer name=\"File\" /></ArchitecturalLevels>"));

		var result = ArchitectureConfigurationDocumentLoader.TryReadAnalyzerConfigurationText(additionalFiles, compilation, "Properties\\AnaalIJzerSettings.cs", TestContext.Current.CancellationToken);

		result.Should().NotBeNull();
		result.Path.Should().Be("Architecture.anl");
		result.Content.Should().Contain("Layer name=\"File\"");
	}

	[Fact]
	public void TryReadInlineConfigurationXml_ReadsAssemblyMetadataValue()
	{
		var compilation = CreateCompilation("""
		                                  using System.Reflection;

		                                  [assembly: AssemblyMetadata("AnaalIJzerSettings", "<ArchitecturalLevels><Layer name=\"Inline\" /></ArchitecturalLevels>")]
		                                  public class Example { }
		                                  """);

		var result = ArchitectureConfigurationDocumentLoader.TryReadInlineConfigurationXml(compilation);

		result.Should().Contain("Layer name=\"Inline\"");
	}

	[Fact]
	public void ContainsInlineSettingsMetadata_RecognizesAssemblyMetadataAttribute()
	{
		var tree = CSharpSyntaxTree.ParseText("""
		                                     using System.Reflection;

		                                     [assembly: AssemblyMetadata("AnaalIJzerSettings", "<ArchitecturalLevels />")]
		                                     public class Example { }
		                                     """, cancellationToken: TestContext.Current.CancellationToken);

		var result = ArchitectureConfigurationDocumentLoader.ContainsInlineSettingsMetadata(tree.GetRoot(TestContext.Current.CancellationToken));

		result.Should().BeTrue();
	}

	[Fact]
	public void TryReadConfigurationDocument_ReadsInlineAssemblyMetadataSource()
	{
		var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + ".cs");
		File.WriteAllText(
			path,
			"""
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", "<ArchitecturalLevels><Layer name=\"Inline\" /></ArchitecturalLevels>")]
			public class Example { }
			""");

		try
		{
			var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, path);

			var success = ArchitectureConfigurationDocumentLoader.TryReadConfigurationDocument(source, out var document, out var message);

			success.Should().BeTrue();
			document.Should().NotBeNull();
			document.Root!.Name.LocalName.Should().Be("ArchitecturalLevels");
			message.Should().Contain("Loaded inline architecture configuration.");
		}
		finally
		{
			File.Delete(path);
		}
	}

	private static Compilation CreateCompilation(string source)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source, path: "Example.cs");
		var references = new[]
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(AssemblyMetadataAttribute).Assembly.Location)
		};
		var result = CSharpCompilation.Create("TestAssembly", [syntaxTree], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		return result;
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
