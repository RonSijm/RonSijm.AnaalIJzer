using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Conditions;

namespace RonSijm.AnaalIJzer.Core.Matchers.Tests.Matching;

public sealed class PatternMatcherSemanticTests
{
	public static TheoryData<string, string> DeclaredTypeKinds => new()
	{
		{ "Class", "public class Target { }" },
		{ "Interface", "public interface Target { }" },
		{ "Struct", "public struct Target { }" },
		{ "Record", "public record Target;" },
		{ "RecordStruct", "public record struct Target;" },
		{ "Enum", "public enum Target { Value }" },
		{ "Delegate", "public delegate void Target();" },
		{ "iNtErFaCe", "public interface Target { }" }
	};

	public static TheoryData<string, string> AccessModifiers => new()
	{
		{ "public", "PublicTool" },
		{ "internal", "InternalTool" },
		{ "private", "PrivateTool" },
		{ "protected", "ProtectedTool" },
		{ "sealed", "SealedTool" },
		{ "abstract", "AbstractTool" },
		{ "static", "StaticTool" },
		{ "record", "RecordTool" }
	};

	[Theory]
	[MemberData(nameof(DeclaredTypeKinds))]
	public void HasTypeKind_MatchesEverySupportedKind(string typeKind, string declaration)
	{
		var symbol = GetDeclaredTypeSymbol(declaration, "Target");
		var matcher = new PatternMatcher(MatchTarget.TypeName, MatchKind.HasTypeKind, typeKind);

		var result = matcher.TryMatch(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol);

		result.Should().Be(string.Empty);
	}

	[Theory]
	[InlineData("Class", "public record Target;")]
	[InlineData("Struct", "public record struct Target;")]
	[InlineData("Record", "public class Target { }")]
	[InlineData("RecordStruct", "public struct Target { }")]
	public void HasTypeKind_SeparatesRecordsFromClassesAndStructs(string typeKind, string declaration)
	{
		var symbol = GetDeclaredTypeSymbol(declaration, "Target");
		var matcher = new PatternMatcher(MatchTarget.TypeName, MatchKind.HasTypeKind, typeKind);

		var result = matcher.TryMatch(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol);

		result.Should().BeNull();
	}

	[Theory]
	[MemberData(nameof(AccessModifiers))]
	public void HasAccessModifier_MatchesEverySupportedModifierToken(string modifier, string typeName)
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

		var result = matcher.TryMatch(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol);

		result.Should().Be(string.Empty);
	}

	[Fact]
	public void HasAccessModifier_UnsupportedTokenDoesNotMatch()
	{
		const string source = """
		                      namespace ModifierSamples
		                      {
		                          public class PublicTool { }
		                      }
		                      """;

		var symbol = GetDeclaredTypeSymbol(source, "PublicTool");
		var matcher = new PatternMatcher(MatchTarget.TypeName, MatchKind.HasAccessModifier, "friend");

		var result = matcher.TryMatch(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol);

		result.Should().BeNull();
	}

	[Fact]
	public void HasAttribute_MatchesShortSuffixAndFullyQualifiedNames()
	{
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
		                          [LegacyIngredient]
		                          public class OldTopping { }
		                      }
		                      """;

		var symbol = GetDeclaredTypeSymbol(source, "OldTopping");
		var shortMatcher = new PatternMatcher(MatchTarget.TypeName, MatchKind.HasAttribute, "LegacyIngredient");
		var fullMatcher = new PatternMatcher(MatchTarget.TypeName, MatchKind.HasAttribute, "LegacyIngredientAttribute");
		var qualifiedMatcher = new PatternMatcher(MatchTarget.TypeName, MatchKind.HasAttribute, "App.Markers.LegacyIngredientAttribute");

		shortMatcher.TryMatch(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol).Should().Be(string.Empty);
		fullMatcher.TryMatch(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol).Should().Be(string.Empty);
		qualifiedMatcher.TryMatch(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol).Should().Be(string.Empty);
	}

	[Fact]
	public void Inherits_MatchesTransitiveBaseType()
	{
		const string source = """
		                      public class LegacyBase { }
		                      public class Intermediate : LegacyBase { }
		                      public class GrandChild : Intermediate { }
		                      """;

		var symbol = GetDeclaredTypeSymbol(source, "GrandChild");
		var matcher = new PatternMatcher(MatchTarget.TypeName, MatchKind.Inherits, "LegacyBase");

		var result = matcher.TryMatch(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol);

		result.Should().Be(string.Empty);
	}

	[Fact]
	public void Implements_MatchesTransitiveInterface()
	{
		const string source = """
		                      public interface IBase { }
		                      public interface IFoo : IBase { }
		                      public class ConcreteFoo : IFoo { }
		                      """;

		var symbol = GetDeclaredTypeSymbol(source, "ConcreteFoo");
		var matcher = new PatternMatcher(MatchTarget.TypeName, MatchKind.Implements, "IBase");

		var result = matcher.TryMatch(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol);

		result.Should().Be(string.Empty);
	}

	private static INamedTypeSymbol GetDeclaredTypeSymbol(string source, string typeName)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);
		var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator)
			.Select(path => MetadataReference.CreateFromFile(path));
		var compilation = CSharpCompilation.Create(
			"MatcherTests",
			[syntaxTree],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		var model = compilation.GetSemanticModel(syntaxTree);
		var root = syntaxTree.GetRoot();
		var typeDeclaration = root
			.DescendantNodes()
			.OfType<MemberDeclarationSyntax>()
			.Single(member => member switch
			{
				TypeDeclarationSyntax type => type.Identifier.ValueText == typeName,
				EnumDeclarationSyntax type => type.Identifier.ValueText == typeName,
				DelegateDeclarationSyntax type => type.Identifier.ValueText == typeName,
				_ => false
			});
		var symbol = model.GetDeclaredSymbol(typeDeclaration);
		var result = symbol.Should().BeAssignableTo<INamedTypeSymbol>().Which;

		return result;
	}
}
