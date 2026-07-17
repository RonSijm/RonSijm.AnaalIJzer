using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.UnitTests.TestSupport;

namespace RonSijm.AnaalIJzer.UnitTests.Matching;

public sealed partial class SemanticMatcherTests
{
	[Fact]
	public async Task Exceptions_SemanticMatcher_BypassesRule()
	{
		// 'implements="IDomainEvent"' forbids everything that implements the marker,
		// but the [LegacyEvent] attribute exempts grandfathered ones.
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class implements="IDomainEvent">
		                                  <Exceptions>
		                                      <Class withAttribute="LegacyEvent" />
		                                  </Exceptions>
		                              </Class>
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      using System;
		                      public interface IDomainEvent { }
		                      [AttributeUsage(AttributeTargets.Class)]
		                      public sealed class LegacyEventAttribute : Attribute { }
		                      [LegacyEvent] public class OldEvent : IDomainEvent { }
		                      public class NewEvent : IDomainEvent { }
		                      public class OrderManager(OldEvent ok, NewEvent bad) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var forbidden = diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.ToList();

		forbidden.Should().ContainSingle();
		forbidden[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain("NewEvent");
	}

	private static INamedTypeSymbol GetDeclaredTypeSymbol(string source, string typeName)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);
		var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator)
			.Select(path => MetadataReference.CreateFromFile(path));

		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			[syntaxTree],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var model = compilation.GetSemanticModel(syntaxTree);
		var typeDeclaration = syntaxTree.GetRoot()
			.DescendantNodes()
			.OfType<TypeDeclarationSyntax>()
			.Single(t => t.Identifier.ValueText == typeName);

		return (INamedTypeSymbol)model.GetDeclaredSymbol(typeDeclaration)!;
	}
}
