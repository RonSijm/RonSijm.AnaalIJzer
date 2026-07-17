using System.Xml.Linq;
using System.Xml.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RonSijm.AnaalIJzer.Tooling.Tests.Tooling;
public sealed partial class ToolingTests
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

	private static CSharpCompilation CreateInferenceCompilation(int managerCallerCount, int repositoryCallerStart)
	{
		var controllers = Enumerable.Range(1, 10).Select(index =>
		{
			var dependency = index <= managerCallerCount ? "CandyShop.Managers.CandyManager" : "CandyShop.Repositories.CandyRepository";
			if (index >= repositoryCallerStart)
			{
				dependency = index <= managerCallerCount
					? $"CandyShop.Managers.CandyManager manager, CandyShop.Repositories.CandyRepository repository"
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
}
