using System.Xml.Linq;
using System.Xml.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RonSijm.AnaalIJzer.Application.Tests.ApplicationOperations;
public sealed partial class ApplicationOperationsTests
{
	private static string FindSchemaPath()
	{
		for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
		{
			var candidate = Path.Combine(directory.FullName, "src", "Main", "RonSijm.AnaalIJzer", "Scheme", "AnaalIJzer.xsd");
			if (File.Exists(candidate))
			{
				return candidate;
			}
		}

		throw new InvalidOperationException("Could not locate AnaalIJzer.xsd.");
	}

	private static string FindRepositoryProject(params string[] pathParts)
	{
		var repositoryRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FindSchemaPath())!, "..", "..", "..", ".."));
		var segments = new string[pathParts.Length + 1];
		segments[0] = repositoryRoot;
		Array.Copy(pathParts, 0, segments, 1, pathParts.Length);
		var result = Path.Combine(segments);

		return result;
	}

	private static string CreateRepositoryTempDirectory(string prefix)
	{
		var repositoryRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FindSchemaPath())!, "..", "..", "..", ".."));
		var result = Path.Combine(repositoryRoot, "tmp", prefix + "-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(result);

		return result;
	}

	private static CSharpCompilation CreateInferenceCompilation(int managerCallerCount, int repositoryCallerStart)
	{
		var controllers = Enumerable.Range(1, 10).Select(index =>
		{
			var dependency = index <= managerCallerCount ? "CandyShop.Managers.CandyManager" : "CandyShop.Repositories.CandyRepository";
			if (index >= repositoryCallerStart)
			{
				dependency = index <= managerCallerCount
					? "CandyShop.Managers.CandyManager manager, CandyShop.Repositories.CandyRepository repository"
					: "CandyShop.Repositories.CandyRepository repository";
			}
			else
			{
				dependency += " dependency";
			}

			return $"namespace CandyShop.Controllers {{ public sealed class CandyController{index} {{ public CandyController{index}({dependency}) {{ }} }} }}";
		});
		var source = string.Join(Environment.NewLine, controllers) + """

			namespace CandyShop.Managers { public sealed class CandyManager { } }
			namespace CandyShop.Repositories { public sealed class CandyRepository { } }
			""";
		return CSharpCompilation.Create(
			"CandyShop",
			[CSharpSyntaxTree.ParseText(source)],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}

	private static CSharpCompilation CreateThresholdComparisonCompilation()
	{
		var conventionalCallers = Enumerable.Range(1, 8)
			.Select(index => $"public sealed class Endpoint{index} {{ public Endpoint{index}(Shop.Application.OrderService service) {{ }} }}");
		var source = $$"""
			namespace Shop.Presentation
			{
				{{string.Join(Environment.NewLine, conventionalCallers)}}
				public sealed class LegacyAdminEndpoint { public LegacyAdminEndpoint(Shop.Persistence.OrderRepository repository) { } }
				public sealed class ImportEndpoint { public ImportEndpoint(Shop.Persistence.OrderRepository repository) { } }
			}

			namespace Shop.Application { public sealed class OrderService { } }
			namespace Shop.Persistence { public sealed class OrderRepository { } }
			""";
		return CSharpCompilation.Create(
			"Shop",
			[CSharpSyntaxTree.ParseText(source)],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}

	private static void AssertValid(XDocument document, string schemaPath)
	{
		var schemas = new XmlSchemaSet();
		schemas.Add(null, schemaPath);
		var errors = new List<string>();
		document.Validate(schemas, (_, args) => errors.Add(args.Message));
		errors.Should().BeEmpty();
	}

	private static void CopyDirectory(string sourceDirectory, string targetDirectory)
	{
		Directory.CreateDirectory(targetDirectory);

		foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
		{
			var relativePath = Path.GetRelativePath(sourceDirectory, directory);
			if (IsBuildArtifactPath(relativePath))
			{
				continue;
			}

			Directory.CreateDirectory(Path.Combine(targetDirectory, relativePath));
		}

		foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
		{
			var relativePath = Path.GetRelativePath(sourceDirectory, file);
			if (IsBuildArtifactPath(relativePath))
			{
				continue;
			}

			var targetPath = Path.Combine(targetDirectory, relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
			File.Copy(file, targetPath, overwrite: true);
		}
	}

	private static bool IsBuildArtifactPath(string relativePath)
	{
		var normalized = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
		var segments = normalized.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
		var result = segments.Any(segment => string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase)
		                                    || string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase));

		return result;
	}

	private static string CloneExampleProject(string tempDirectory, params string[] pathParts)
	{
		var sourceProjectSegments = new string[pathParts.Length + 1];
		sourceProjectSegments[0] = "Examples";
		Array.Copy(pathParts, 0, sourceProjectSegments, 1, pathParts.Length);
		var sourceProjectPath = FindRepositoryProject(sourceProjectSegments);
		var sourceDirectory = Path.GetDirectoryName(sourceProjectPath)!;
		var clonedExamplesDirectory = Path.Combine(tempDirectory, "Examples");
		Directory.CreateDirectory(clonedExamplesDirectory);
		File.Copy(
			FindRepositoryProject("Examples", "Directory.Build.props"),
			Path.Combine(clonedExamplesDirectory, "Directory.Build.props"),
			overwrite: true);

		var relativeDirectorySegments = pathParts.Take(pathParts.Length - 1).ToArray();
		var clonedDirectorySegments = new string[relativeDirectorySegments.Length + 1];
		clonedDirectorySegments[0] = clonedExamplesDirectory;
		Array.Copy(relativeDirectorySegments, 0, clonedDirectorySegments, 1, relativeDirectorySegments.Length);
		var clonedDirectory = Path.Combine(clonedDirectorySegments);
		CopyDirectory(sourceDirectory, clonedDirectory);
		var result = Path.Combine(clonedDirectory, Path.GetFileName(sourceProjectPath));

		return result;
	}

	private static string WriteSolutionFile(string directory, params string[] projectPaths)
	{
		var solutionPath = Path.Combine(directory, "ExampleSolution.slnx");
		new XDocument(new XElement("Solution", projectPaths.Select(projectPath => new XElement("Project", new XAttribute("Path", projectPath))))).Save(solutionPath);

		return solutionPath;
	}
}

