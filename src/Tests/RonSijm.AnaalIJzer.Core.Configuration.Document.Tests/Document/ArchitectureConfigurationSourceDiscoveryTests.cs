using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Tests.Document;

public sealed class ArchitectureConfigurationSourceDiscoveryTests
{
	[Fact]
	public void TryReadInlineConfigurationTextDocument_UsesAttributeSourcePath()
	{
		var compilation = CreateCompilation("""
		                                  using System.Reflection;

		                                  [assembly: AssemblyMetadata("AnaalIJzerSettings", "<ArchitecturalLevels><Layer name=\"Inline\" /></ArchitecturalLevels>")]
		                                  public class Example { }
		                                  """, "Properties\\AnaalIJzerSettings.cs");

		var result = ArchitectureConfigurationSourceDiscovery.TryReadInlineConfigurationTextDocument(compilation, null, TestContext.Current.CancellationToken);

		result.Should().NotBeNull();
		result!.Path.Should().Be("Properties\\AnaalIJzerSettings.cs");
		result.Content.Should().Contain("Layer name=\"Inline\"");
	}

	[Fact]
	public void FindNearestConfigurationFilePath_FindsParentArchitectureAnl()
	{
		var rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
		var nestedDirectory = Path.Combine(rootDirectory, "Feature", "Child");
		Directory.CreateDirectory(nestedDirectory);
		var configPath = Path.Combine(rootDirectory, "Architecture.anl");
		var documentPath = Path.Combine(nestedDirectory, "Example.cs");
		File.WriteAllText(configPath, "<ArchitecturalLevels />");
		File.WriteAllText(documentPath, "public class Example { }");

		try
		{
			var result = ArchitectureConfigurationSourceDiscovery.FindNearestConfigurationFilePath(documentPath);

			result.Should().Be(configPath);
		}
		finally
		{
			Directory.Delete(rootDirectory, recursive: true);
		}
	}

	[Fact]
	public void FindConfigurationSource_ReturnsInlineAssemblyMetadata_WhenNoAdditionalFileExists()
	{
		var compilation = CreateCompilation("""
		                                  using System.Reflection;

		                                  [assembly: AssemblyMetadata("AnaalIJzerSettings", "<ArchitecturalLevels><Layer name=\"Inline\" /></ArchitecturalLevels>")]
		                                  public class Example { }
		                                  """, "Properties\\AnaalIJzerSettings.cs");

		var result = ArchitectureConfigurationSourceDiscovery.FindConfigurationSource("Feature\\Example.cs", [], compilation, TestContext.Current.CancellationToken);

		result.Kind.Should().Be(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata);
		result.Path.Should().Be("Properties\\AnaalIJzerSettings.cs");
	}

	[Fact]
	public void CreateConfigurationCreationTargets_DeduplicatesSharedDirectoryBuildPropsTarget()
	{
		var projectPath = "D:\\repo\\src\\Example\\Example.csproj";
		var solutionPath = "D:\\repo\\src\\Example\\Example.slnx";

		var result = ArchitectureConfigurationSourceDiscovery.CreateConfigurationCreationTargets(projectPath, solutionPath);

		result.Should().HaveCount(2);
		result.Select(target => target.Title).Should().BeEquivalentTo(["Project file", "Project folder"]);
	}

	[Fact]
	public void TryCreateConfigurationSource_PrefersConfigurationFileOverInlineSource()
	{
		var rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
		Directory.CreateDirectory(rootDirectory);
		var configPath = Path.Combine(rootDirectory, "Architecture.anl");
		var inlinePath = Path.Combine(rootDirectory, "Properties", "AnaalIJzerSettings.cs");
		Directory.CreateDirectory(Path.GetDirectoryName(inlinePath)!);
		File.WriteAllText(configPath, "<ArchitecturalLevels />");
		File.WriteAllText(inlinePath, "[assembly: System.Reflection.AssemblyMetadata(\"AnaalIJzerSettings\", \"<ArchitecturalLevels />\")]");

		try
		{
			var success = ArchitectureConfigurationSourceDiscovery.TryCreateConfigurationSource(configPath, inlinePath, out var result);

			success.Should().BeTrue();
			result.Kind.Should().Be(ArchitectureConfigurationSourceKind.XmlFile);
			result.Path.Should().Be(configPath);
		}
		finally
		{
			Directory.Delete(rootDirectory, recursive: true);
		}
	}

	[Fact]
	public void TryCreateConfigurationSource_UsesInlineSource_WhenConfigurationFileIsMissing()
	{
		var rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
		Directory.CreateDirectory(rootDirectory);
		var inlinePath = Path.Combine(rootDirectory, "Properties", "AnaalIJzerSettings.cs");
		Directory.CreateDirectory(Path.GetDirectoryName(inlinePath)!);
		File.WriteAllText(inlinePath, "[assembly: System.Reflection.AssemblyMetadata(\"AnaalIJzerSettings\", \"<ArchitecturalLevels />\")]");

		try
		{
			var success = ArchitectureConfigurationSourceDiscovery.TryCreateConfigurationSource(null, inlinePath, out var result);

			success.Should().BeTrue();
			result.Kind.Should().Be(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata);
			result.Path.Should().Be(inlinePath);
		}
		finally
		{
			Directory.Delete(rootDirectory, recursive: true);
		}
	}

	private static Compilation CreateCompilation(string source, string path)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source, path: path);
		var references = new[]
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(AssemblyMetadataAttribute).Assembly.Location)
		};
		var result = CSharpCompilation.Create("TestAssembly", [syntaxTree], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		return result;
	}
}
