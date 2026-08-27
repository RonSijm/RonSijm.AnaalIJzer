using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.Matchers.Declarations;
using RonSijm.AnaalIJzer.Core.Matchers.Observations;

namespace RonSijm.AnaalIJzer.Core.Matchers.Tests.Matching;

public sealed class PatternMatcherDeclarationMatcherTests
{
	[Fact]
	public void RequiredDeclarations_SupportEveryDeclarationTarget()
	{
		var symbol = GetDeclaredTypeSymbol(GetDeclarationTargetSource(), "PizzaRequest");
		var matchers = new[]
		{
			CreateDeclarationMatcher(DeclarationMatchTarget.Type,
				new MatchCondition(MatchKind.Equals, "PizzaRequest", MatchOperand.Declaration),
				new MatchCondition(MatchKind.HasTypeKind, "Class", MatchOperand.AssociatedType)),
			CreateDeclarationMatcher(DeclarationMatchTarget.NestedType,
				new MatchCondition(MatchKind.Equals, "PizzaMetadata", MatchOperand.Declaration),
				new MatchCondition(MatchKind.Equals, "PizzaMetadata", MatchOperand.AssociatedType)),
			CreateDeclarationMatcher(DeclarationMatchTarget.Constructor,
				new MatchCondition(MatchKind.Equals, "PizzaRequest", MatchOperand.Declaration),
				new MatchCondition(MatchKind.Equals, "PizzaRequest", MatchOperand.AssociatedType)),
			CreateDeclarationMatcher(DeclarationMatchTarget.Method,
				new MatchCondition(MatchKind.Equals, "Project", MatchOperand.Declaration),
				new MatchCondition(MatchKind.Equals, "PizzaProjection", MatchOperand.AssociatedType)),
			CreateDeclarationMatcher(DeclarationMatchTarget.Property,
				new MatchCondition(MatchKind.Equals, "PizzaId", MatchOperand.Declaration),
				new MatchCondition(MatchKind.Equals, "PizzaId", MatchOperand.AssociatedType)),
			CreateDeclarationMatcher(DeclarationMatchTarget.Field,
				new MatchCondition(MatchKind.Equals, "_tenantId", MatchOperand.Declaration),
				new MatchCondition(MatchKind.Equals, "TenantId", MatchOperand.AssociatedType)),
			CreateDeclarationMatcher(DeclarationMatchTarget.Event,
				new MatchCondition(MatchKind.Equals, "Changed", MatchOperand.Declaration),
				new MatchCondition(MatchKind.Equals, "Action", MatchOperand.AssociatedType)),
			CreateDeclarationMatcher(DeclarationMatchTarget.Operator,
				new MatchCondition(MatchKind.Equals, "PizzaRequest", MatchOperand.AssociatedType)),
			CreateDeclarationMatcher(DeclarationMatchTarget.Conversion,
				new MatchCondition(MatchKind.Equals, "PizzaId", MatchOperand.AssociatedType))
		};

		foreach (var declarationMatcher in matchers)
		{
			var matcher = new PatternMatcher(
				MatchTarget.TypeName,
				[new MatchCondition(MatchKind.Equals, "PizzaRequest")],
				[declarationMatcher]);

			var result = matcher.TryMatch(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol);

			result.Should().Be(string.Empty, $"{declarationMatcher.Target} should be matchable as a required declaration");
		}
	}

	[Fact]
	public void RequiredDeclarations_AreConjunctiveAcrossSiblingDeclarationMatchers()
	{
		var symbol = GetDeclaredTypeSymbol(GetDeclarationTargetSource(), "PizzaRequest");
		var matcher = new PatternMatcher(
			MatchTarget.TypeName,
			[new MatchCondition(MatchKind.Equals, "PizzaRequest")],
			[
				CreateDeclarationMatcher(DeclarationMatchTarget.Property,
					new MatchCondition(MatchKind.Equals, "PizzaId", MatchOperand.Declaration),
					new MatchCondition(MatchKind.Equals, "PizzaId", MatchOperand.AssociatedType)),
				CreateDeclarationMatcher(DeclarationMatchTarget.Field,
					new MatchCondition(MatchKind.Equals, "_tenantId", MatchOperand.Declaration),
					new MatchCondition(MatchKind.Equals, "TenantId", MatchOperand.AssociatedType))
			]);

		var result = matcher.TryMatch(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol);

		result.Should().Be(string.Empty);
	}

	[Fact]
	public void MissingRequiredDeclaration_FailsMatch()
	{
		var symbol = GetDeclaredTypeSymbol(GetDeclarationTargetSource(), "PizzaRequest");
		var matcher = new PatternMatcher(
			MatchTarget.TypeName,
			[new MatchCondition(MatchKind.Equals, "PizzaRequest")],
			[
				CreateDeclarationMatcher(DeclarationMatchTarget.Property,
					new MatchCondition(MatchKind.Equals, "PizzaId", MatchOperand.Declaration),
					new MatchCondition(MatchKind.Equals, "PizzaId", MatchOperand.AssociatedType)),
				CreateDeclarationMatcher(DeclarationMatchTarget.Field,
					new MatchCondition(MatchKind.Equals, "_missingTenantId", MatchOperand.Declaration),
					new MatchCondition(MatchKind.Equals, "TenantId", MatchOperand.AssociatedType))
			]);

		var result = matcher.TryMatch(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol);

		result.Should().BeNull();
	}

	[Fact]
	public void RequiredObservations_CanMatchMethodAndPropertyBodies()
	{
		var symbol = GetDeclaredTypeSymbol(GetObservationSource(), "CrashingPizzaDeliveryService");
		var matcher = new PatternMatcher(
			MatchTarget.TypeName,
			[new MatchCondition(MatchKind.Equals, "CrashingPizzaDeliveryService")],
			[
				new DeclarationMatcher(
					DeclarationMatchTarget.Method,
					[
						new MatchCondition(MatchKind.Equals, "PizzaDelivery", MatchOperand.Declaration)
					],
					[
						new CodeObservationMatcher(CodeObservationMatchTarget.Throw, ImmutableArray<MatchCondition>.Empty)
					]),
				new DeclarationMatcher(
					DeclarationMatchTarget.Property,
					[
						new MatchCondition(MatchKind.Equals, "PizzaId", MatchOperand.Declaration)
					],
					[
						new CodeObservationMatcher(
							CodeObservationMatchTarget.Throw,
							[
								new MatchCondition(MatchKind.Equals, "InvalidOperationException", MatchOperand.AssociatedType)
							])
					])
			]);

		var result = matcher.TryMatch(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol);

		result.Should().Be(string.Empty);
	}

	[Fact]
	public void MissingRequiredObservation_FailsMatch()
	{
		var symbol = GetDeclaredTypeSymbol(GetObservationSource(), "CrashingPizzaDeliveryService");
		var matcher = new PatternMatcher(
			MatchTarget.TypeName,
			[new MatchCondition(MatchKind.Equals, "CrashingPizzaDeliveryService")],
			[
				new DeclarationMatcher(
					DeclarationMatchTarget.Method,
					[
						new MatchCondition(MatchKind.Equals, "PizzaDelivery", MatchOperand.Declaration)
					],
					[
						new CodeObservationMatcher(
							CodeObservationMatchTarget.Invocation,
							[
								new MatchCondition(MatchKind.Equals, "LogFailure", MatchOperand.Declaration)
							])
					])
			]);

		var result = matcher.TryMatch(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol);

		result.Should().BeNull();
	}

	private static DeclarationMatcher CreateDeclarationMatcher(DeclarationMatchTarget target, params MatchCondition[] conditions)
	{
		var result = new DeclarationMatcher(target, ImmutableArray.CreateRange(conditions));

		return result;
	}

	private static string GetDeclarationTargetSource()
	{
		var result = """
		             using System;
		             
		             namespace MatcherSamples;
		             
		             public sealed class PizzaId { }
		             public sealed class TenantId { }
		             public sealed class PizzaProjection { }
		             
		             public sealed class PizzaRequest
		             {
		                 private TenantId _tenantId = new();
		                 public PizzaId PizzaId { get; } = new();
		                 
		                 public PizzaRequest()
		                 {
		                 }
		                 
		                 public PizzaProjection Project()
		                 {
		                     return new PizzaProjection();
		                 }
		                 
		                 public event Action Changed
		                 {
		                     add
		                     {
		                     }
		                     remove
		                     {
		                     }
		                 }
		                 
		                 public static PizzaRequest operator +(PizzaRequest left, PizzaRequest right)
		                 {
		                     return left;
		                 }
		                 
		                 public static explicit operator PizzaId(PizzaRequest request)
		                 {
		                     return new PizzaId();
		                 }
		                 
		                 public sealed class PizzaMetadata
		                 {
		                 }
		             }
		             """;

		return result;
	}

	private static string GetObservationSource()
	{
		var result = """
		             using System;
		             
		             namespace MatcherSamples;
		             
		             public sealed class PizzaId { }
		             
		             public sealed class CrashingPizzaDeliveryService
		             {
		                 public PizzaId PizzaId => throw new InvalidOperationException();
		                 
		                 public void PizzaDelivery()
		                 {
		                     throw new InvalidOperationException();
		                 }
		             }
		             """;

		return result;
	}

	private static INamedTypeSymbol GetDeclaredTypeSymbol(string source, string typeName)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);
		var references = TrustedPlatformReferences.Value;
		var compilation = CSharpCompilation.Create(
			"MatcherDeclarationTests",
			[syntaxTree],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		var model = compilation.GetSemanticModel(syntaxTree);
		var root = syntaxTree.GetRoot();
		var typeDeclaration = root
			.DescendantNodes()
			.OfType<TypeDeclarationSyntax>()
			.Single(type => type.Identifier.ValueText == typeName && type.Parent is NamespaceDeclarationSyntax or FileScopedNamespaceDeclarationSyntax);
		var symbol = model.GetDeclaredSymbol(typeDeclaration);
		var result = symbol.Should().BeAssignableTo<INamedTypeSymbol>().Which;

		return result;
	}

	private static readonly Lazy<MetadataReference[]> TrustedPlatformReferences = new(() =>
	{
		var result = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator)
			.Select(path => MetadataReference.CreateFromFile(path))
			.ToArray();

		return result;
	});
}
