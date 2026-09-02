using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.Matchers.Observations;

namespace RonSijm.AnaalIJzer.Core.ReturnValues.Tests.Policies;

public sealed class ReturnValuePolicyTests
{
	[Fact]
	public void Evaluate_RejectsConfiguredLiteral()
	{
		var (expression, semanticModel) = GetReturnExpression("public sealed class PizzaKitchen { public object Serve() => \"\"; }");
		var rule = new ReturnValueRule(
			new CodeObservationMatcher(CodeObservationMatchTarget.Literal, [new MatchCondition(MatchKind.Equals, string.Empty, MatchOperand.Declaration)]),
			"literal value=\"\"",
			"Empty plates are not a serving decision.",
			"Architecture.anl",
			9,
			7);
		var policy = new ReturnValuePolicy("Kitchen", [rule], "Kitchens return meaningful dishes.", "Architecture.anl", 8, 5);

		var evaluation = policy.Evaluate(expression, semanticModel, CancellationToken.None);

		evaluation.Should().NotBeNull();
		evaluation.Value.Rule.Should().Be(rule);
		evaluation.Value.Reason.Should().Contain("literal value=\"\"");
	}

	[Fact]
	public void Evaluate_RejectsInvocationMarkedWithConfiguredAttribute()
	{
		var (expression, semanticModel) = GetReturnExpression(
			"""
			using System;

			[AttributeUsage(AttributeTargets.Method)]
			public sealed class CanBeNullAttribute : Attribute { }

			public sealed class PizzaLookup
			{
				[CanBeNull]
				public object FindPizza() => new object();
			}

			public sealed class PizzaKitchen
			{
				private readonly PizzaLookup lookup = new PizzaLookup();

				public object Serve() => lookup.FindPizza();
			}
			""");
		var rule = new ReturnValueRule(
			new CodeObservationMatcher(CodeObservationMatchTarget.Invocation, [new MatchCondition(MatchKind.HasAttribute, "CanBeNull", MatchOperand.Declaration)]),
			"invocation withAttribute=\"CanBeNull\"",
			"Optional lookup results need a fallback.",
			"Architecture.anl",
			9,
			7);
		var policy = new ReturnValuePolicy("Kitchen", [rule], null, "Architecture.anl", 8, 5);

		var evaluation = policy.Evaluate(expression, semanticModel, CancellationToken.None);

		evaluation.Should().NotBeNull();
		evaluation.Value.Rule.Should().Be(rule);
	}

	[Fact]
	public void Evaluate_AllowsUnmatchedExpression()
	{
		var (expression, semanticModel) = GetReturnExpression("public sealed class PizzaKitchen { public int Serve() => 7; }");
		var rule = new ReturnValueRule(
			new CodeObservationMatcher(CodeObservationMatchTarget.Literal, [new MatchCondition(MatchKind.Equals, "42", MatchOperand.Declaration)]),
			"literal value=\"42\"",
			null,
			"Architecture.anl",
			9,
			7);
		var policy = new ReturnValuePolicy("Kitchen", [rule], null, "Architecture.anl", 8, 5);

		var evaluation = policy.Evaluate(expression, semanticModel, CancellationToken.None);

		evaluation.Should().BeNull();
	}

	private static (ExpressionSyntax Expression, SemanticModel SemanticModel) GetReturnExpression(string source)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);
		var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator)
			.Select(path => MetadataReference.CreateFromFile(path))
			.Cast<MetadataReference>()
			.ToArray();
		var compilation = CSharpCompilation.Create(
			"ReturnValuePolicyTests",
			[syntaxTree],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		var diagnostics = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		diagnostics.Should().BeEmpty();
		var servingMethod = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
			.Single(method => method.Identifier.ValueText == "Serve");
		var expression = servingMethod.ExpressionBody!.Expression;
		var semanticModel = compilation.GetSemanticModel(syntaxTree);
		var result = (expression, semanticModel);

		return result;
	}
}
