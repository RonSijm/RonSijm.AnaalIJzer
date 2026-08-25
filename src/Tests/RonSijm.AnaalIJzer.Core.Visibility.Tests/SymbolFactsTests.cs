using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Engine.Visibility;
using RonSijm.AnaalIJzer.SymbolFacts;

namespace RonSijm.AnaalIJzer.Core.Visibility.Tests;

public sealed class SymbolFactsTests
{
	[Fact]
	public void TryGetArchitectureAccessibility_MapsSourceDeclarations()
	{
		const string source = """
			public class PublicType
			{
				internal class NestedInternalType { }
				private protected void PrivateProtectedMethod() { }
			}

			internal class InternalType { }
			file class FileLocalType { }
			""";
		var compilation = CreateCompilation(source);
		var model = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
		var root = compilation.SyntaxTrees[0].GetRoot(TestContext.Current.CancellationToken);

		var publicType = GetNamedType(model, root, "PublicType");
		var internalType = GetNamedType(model, root, "InternalType");
		var fileLocalType = GetNamedType(model, root, "FileLocalType");
		var nestedInternalType = GetNamedType(model, root, "NestedInternalType");
		var privateProtectedMethod = GetMethod(model, root, "PrivateProtectedMethod");

		publicType.TryGetArchitectureAccessibility(out var publicAccessibility).Should().BeTrue();
		internalType.TryGetArchitectureAccessibility(out var internalAccessibility).Should().BeTrue();
		fileLocalType.TryGetArchitectureAccessibility(out var fileAccessibility).Should().BeTrue();
		nestedInternalType.TryGetArchitectureAccessibility(out var nestedAccessibility).Should().BeTrue();
		privateProtectedMethod.TryGetArchitectureAccessibility(out var privateProtectedAccessibility).Should().BeTrue();

		publicAccessibility.Should().Be(ArchitectureAccessibility.Public);
		internalAccessibility.Should().Be(ArchitectureAccessibility.Internal);
		fileAccessibility.Should().Be(ArchitectureAccessibility.File);
		nestedAccessibility.Should().Be(ArchitectureAccessibility.Internal);
		privateProtectedAccessibility.Should().Be(ArchitectureAccessibility.PrivateProtected);
	}

	[Fact]
	public void TryGetArchitectureDeclarationTarget_ClassifiesEverySupportedDeclaration()
	{
		const string source = """
			using System;

			public class PolicySubject
			{
				public PolicySubject() { }
				public void Run() { }
				public int Value { get; set; }
				public int Field;
				public event EventHandler? Changed;
				public static PolicySubject operator +(PolicySubject left, PolicySubject right) => left;
				public static explicit operator int(PolicySubject value) => 0;

				public class Nested { }
			}
			""";
		var compilation = CreateCompilation(source);
		var model = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
		var root = compilation.SyntaxTrees[0].GetRoot(TestContext.Current.CancellationToken);
		var policySubject = GetNamedType(model, root, "PolicySubject");
		var nested = GetNamedType(model, root, "Nested");
		var constructor = GetConstructor(model, root, "PolicySubject");
		var method = GetMethod(model, root, "Run");
		var property = GetProperty(model, root, "Value");
		var field = GetField(model, root, "Field");
		var eventSymbol = GetEvent(model, root, "Changed");
		var operatorSymbol = GetOperator(model, root);
		var conversion = GetConversionOperator(model, root);

		policySubject.TryGetArchitectureDeclarationTarget(out var typeTarget).Should().BeTrue();
		nested.TryGetArchitectureDeclarationTarget(out var nestedTarget).Should().BeTrue();
		constructor.TryGetArchitectureDeclarationTarget(out var constructorTarget).Should().BeTrue();
		method.TryGetArchitectureDeclarationTarget(out var methodTarget).Should().BeTrue();
		property.TryGetArchitectureDeclarationTarget(out var propertyTarget).Should().BeTrue();
		field.TryGetArchitectureDeclarationTarget(out var fieldTarget).Should().BeTrue();
		eventSymbol.TryGetArchitectureDeclarationTarget(out var eventTarget).Should().BeTrue();
		operatorSymbol.TryGetArchitectureDeclarationTarget(out var operatorTarget).Should().BeTrue();
		conversion.TryGetArchitectureDeclarationTarget(out var conversionTarget).Should().BeTrue();

		typeTarget.Should().Be(VisibilityPolicyTarget.Type);
		nestedTarget.Should().Be(VisibilityPolicyTarget.NestedType);
		constructorTarget.Should().Be(VisibilityPolicyTarget.Constructor);
		methodTarget.Should().Be(VisibilityPolicyTarget.Method);
		propertyTarget.Should().Be(VisibilityPolicyTarget.Property);
		fieldTarget.Should().Be(VisibilityPolicyTarget.Field);
		eventTarget.Should().Be(VisibilityPolicyTarget.Event);
		operatorTarget.Should().Be(VisibilityPolicyTarget.Operator);
		conversionTarget.Should().Be(VisibilityPolicyTarget.Conversion);
	}

	[Fact]
	public void IsEffectivelyExternallyVisible_AccountsForContainingTypes()
	{
		const string source = """
			public class PublicOuter
			{
				public void Visible() { }

				internal class HiddenNested
				{
					public void HiddenByParent() { }
				}
			}
			""";
		var compilation = CreateCompilation(source);
		var model = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
		var root = compilation.SyntaxTrees[0].GetRoot(TestContext.Current.CancellationToken);

		var visibleMethod = GetMethod(model, root, "Visible");
		var hiddenMethod = GetMethod(model, root, "HiddenByParent");

		visibleMethod.IsEffectivelyExternallyVisible().Should().BeTrue();
		hiddenMethod.IsEffectivelyExternallyVisible().Should().BeFalse();
	}

	private static CSharpCompilation CreateCompilation(string source)
	{
		var tree = CSharpSyntaxTree.ParseText(source);
		var references = AppDomain.CurrentDomain
			.GetAssemblies()
			.Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
			.Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
			.Cast<MetadataReference>()
			.ToArray();
		var result = CSharpCompilation.Create(
			"VisibilityTests",
			[tree],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		return result;
	}

	private static INamedTypeSymbol GetNamedType(SemanticModel model, SyntaxNode root, string identifier)
	{
		var declaration = root.DescendantNodes()
			.OfType<TypeDeclarationSyntax>()
			.Single(node => node.Identifier.ValueText == identifier);
		var result = (INamedTypeSymbol)model.GetDeclaredSymbol(declaration)!;

		return result;
	}

	private static IMethodSymbol GetMethod(SemanticModel model, SyntaxNode root, string identifier)
	{
		var declaration = root.DescendantNodes()
			.OfType<MethodDeclarationSyntax>()
			.Single(node => node.Identifier.ValueText == identifier);
		var result = (IMethodSymbol)model.GetDeclaredSymbol(declaration)!;

		return result;
	}

	private static IMethodSymbol GetConstructor(SemanticModel model, SyntaxNode root, string identifier)
	{
		var declaration = root.DescendantNodes()
			.OfType<ConstructorDeclarationSyntax>()
			.Single(node => node.Identifier.ValueText == identifier);
		var result = (IMethodSymbol)model.GetDeclaredSymbol(declaration)!;

		return result;
	}

	private static IPropertySymbol GetProperty(SemanticModel model, SyntaxNode root, string identifier)
	{
		var declaration = root.DescendantNodes()
			.OfType<PropertyDeclarationSyntax>()
			.Single(node => node.Identifier.ValueText == identifier);
		var result = (IPropertySymbol)model.GetDeclaredSymbol(declaration)!;

		return result;
	}

	private static IFieldSymbol GetField(SemanticModel model, SyntaxNode root, string identifier)
	{
		var declaration = root.DescendantNodes()
			.OfType<VariableDeclaratorSyntax>()
			.Single(node => node.Identifier.ValueText == identifier);
		var result = (IFieldSymbol)model.GetDeclaredSymbol(declaration)!;

		return result;
	}

	private static IEventSymbol GetEvent(SemanticModel model, SyntaxNode root, string identifier)
	{
		var declaration = root.DescendantNodes()
			.OfType<EventFieldDeclarationSyntax>()
			.SelectMany(node => node.Declaration.Variables)
			.Single(node => node.Identifier.ValueText == identifier);
		var result = (IEventSymbol)model.GetDeclaredSymbol(declaration)!;

		return result;
	}

	private static IMethodSymbol GetOperator(SemanticModel model, SyntaxNode root)
	{
		var declaration = root.DescendantNodes()
			.OfType<OperatorDeclarationSyntax>()
			.Single();
		var result = (IMethodSymbol)model.GetDeclaredSymbol(declaration)!;

		return result;
	}

	private static IMethodSymbol GetConversionOperator(SemanticModel model, SyntaxNode root)
	{
		var declaration = root.DescendantNodes()
			.OfType<ConversionOperatorDeclarationSyntax>()
			.Single();
		var result = (IMethodSymbol)model.GetDeclaredSymbol(declaration)!;

		return result;
	}
}
