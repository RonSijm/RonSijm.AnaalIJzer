using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Matching;

namespace RonSijm.AnaalIJzer.UnitTests;

public sealed class SemanticMatcherTests
{
	// ---- exactName / exactFullName ----

	[Fact]
	public async Task ExactName_OnClass_IsSynonymOfTypeName()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class exactName="IIdentityContext"
		                                     comment="Use the DTO." />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IIdentityContext { }
		                      public class PatientManager(IIdentityContext id) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task ExactFullName_MatchesNamespaceQualifiedType()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class exactFullName="App.Legacy.LegacyHelper"
		                                     comment="Migrate away." />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      namespace App.Legacy { public class LegacyHelper { } }
		                      namespace App
		                      {
		                          using App.Legacy;
		                          public class OrderManager(LegacyHelper helper) { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task ExactFullName_DoesNotMatchSimpleName()
	{
		// 'LegacyHelper' is in App.Core, but the rule requires App.Legacy.LegacyHelper.
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class exactFullName="App.Legacy.LegacyHelper" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      namespace App.Core { public class LegacyHelper { } }
		                      namespace App
		                      {
		                          using App.Core;
		                          public class OrderManager(LegacyHelper helper) { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().BeEmpty();
	}

	[Fact]
	public async Task ExactName_OnNamespace_MatchesExactNamespaceString()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Namespace exactName="App.Legacy" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      namespace App.Legacy { public class Helper { } }
		                      namespace App.LegacyShared { public class OtherHelper { } }
		                      namespace App
		                      {
		                          using App.Legacy;
		                          using App.LegacyShared;
		                          public class OrderManager(Helper bad, OtherHelper allowed) { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var forbidden = diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.ToList();

		forbidden.Should().ContainSingle();
		forbidden[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain("Helper");
		forbidden[0].GetMessage(CultureInfo.InvariantCulture).Should().NotContain("OtherHelper");
	}

	// ---- inherits ----

	[Fact]
	public async Task Inherits_MatchesDirectBaseType()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class inherits="LegacyBase" comment="Drop the LegacyBase hierarchy." />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class LegacyBase { }
		                      public class OldThing : LegacyBase { }
		                      public class OrderManager(OldThing thing) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task Inherits_MatchesTransitiveBaseType()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class inherits="LegacyBase" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class LegacyBase { }
		                      public class Intermediate : LegacyBase { }
		                      public class GrandChild : Intermediate { }
		                      public class OrderManager(GrandChild child) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task Inherits_DoesNotMatchUnrelatedType()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class inherits="LegacyBase" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class LegacyBase { }
		                      public class CleanThing { }
		                      public class OrderManager(CleanThing thing) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	// ---- implements ----

	[Fact]
	public async Task Implements_MatchesDirectInterface()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class implements="IDomainEvent"
		                                     comment="Managers must not depend on raw domain events." />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IDomainEvent { }
		                      public class PatientAdmitted : IDomainEvent { }
		                      public class OrderManager(PatientAdmitted ev) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task Implements_MatchesTransitiveInterface()
	{
		// IFoo : IBase, ConcreteFoo : IFoo -> ConcreteFoo transitively implements IBase.
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class implements="IBase" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IBase { }
		                      public interface IFoo : IBase { }
		                      public class ConcreteFoo : IFoo { }
		                      public class OrderManager(ConcreteFoo foo) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task Implements_DoesNotMatchUnrelatedInterface()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class implements="IDomainEvent" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IDomainEvent { }
		                      public interface IQueryModel { }
		                      public class ReadModel : IQueryModel { }
		                      public class OrderManager(ReadModel model) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().BeEmpty();
	}

	// ---- withAttribute ----

	[Fact]
	public async Task WithAttribute_MatchesWithoutAttributeSuffix()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class withAttribute="Obsolete"
		                                     comment="Remove the obsolete dependency." />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      using System;
		                      [Obsolete] public class OldRegistry { }
		                      public class OrderManager(OldRegistry reg) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task WithAttribute_MatchesWithAttributeSuffix()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class withAttribute="ObsoleteAttribute" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      using System;
		                      [Obsolete] public class OldRegistry { }
		                      public class OrderManager(OldRegistry reg) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task WithAttribute_MatchesFullyQualifiedAttributeName()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class withAttribute="App.Markers.LegacyIngredientAttribute" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      using System;
		                      namespace App.Markers
		                      {
		                          [AttributeUsage(AttributeTargets.Class)]
		                          public sealed class LegacyIngredientAttribute : Attribute { }
		                      }

		                      namespace App
		                      {
		                          using App.Markers;
		                          [LegacyIngredient] public class OldTopping { }
		                          public class OrderManager(OldTopping topping) { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task WithAttribute_DoesNotMatchUndecoratedType()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class withAttribute="Obsolete" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class CleanRegistry { }
		                      public class OrderManager(CleanRegistry reg) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	// ---- withAccessModifier ----

	[Fact]
	public async Task WithAccessModifier_PublicMatchesPublicType()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class withAccessModifier="public"
		                                     comment="Internal-only zone." />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class PublicTool { }
		                      public class OrderManager(PublicTool tool) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task WithAccessModifier_MultipleTokensRequireAll()
	{
		// "public sealed" should match only types that are both public AND sealed.
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class withAccessModifier="public sealed" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public sealed class SealedTool { }
		                      public class NormalTool { }
		                      public class OrderManager(SealedTool s, NormalTool n) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var forbidden = diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.ToList();

		forbidden.Should().ContainSingle();
		forbidden[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain("SealedTool");
	}

	[Theory]
	[InlineData("public", "PublicTool")]
	[InlineData("internal", "InternalTool")]
	[InlineData("private", "PrivateTool")]
	[InlineData("protected", "ProtectedTool")]
	[InlineData("sealed", "SealedTool")]
	[InlineData("abstract", "AbstractTool")]
	[InlineData("static", "StaticTool")]
	[InlineData("record", "RecordTool")]
	public void WithAccessModifier_MatchesEverySupportedModifierToken(string modifier, string typeName)
	{
		const string source = """
		                      namespace ModifierSamples
		                      {
		                          public class PublicTool { }
		                          internal class InternalTool { }
		                          public sealed class SealedTool { }
		                          public abstract class AbstractTool { }
		                          public static class StaticTool { }
		                          public record RecordTool;

		                          public class Host
		                          {
		                              private class PrivateTool { }
		                              protected class ProtectedTool { }
		                          }
		                      }
		                      """;

		var symbol = GetDeclaredTypeSymbol(source, typeName);
		var matcher = new PatternMatcher(MatchTarget.TypeName, MatchKind.HasAccessModifier, modifier);

		matcher.TryMatch(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol)
			.Should().Be(string.Empty);
	}

	[Fact]
	public void WithAccessModifier_UnsupportedTokenDoesNotMatch()
	{
		const string source = """
		                      namespace ModifierSamples
		                      {
		                          public class PublicTool { }
		                      }
		                      """;

		var symbol = GetDeclaredTypeSymbol(source, "PublicTool");
		var matcher = new PatternMatcher(MatchTarget.TypeName, MatchKind.HasAccessModifier, "friend");

		matcher.TryMatch(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol)
			.Should().BeNull();
	}

	// ---- Exceptions interop ----

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
