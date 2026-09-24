using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RonSijm.AnaalIJzer.Core.Matchers.Observations;

namespace RonSijm.AnaalIJzer.Core.Matchers.Tests.Matching;

public sealed class CodeObservationMatchTargetClassifierTests
{
    [Theory]
    [InlineData("throw new System.Exception();", CodeObservationMatchTarget.Throw)]
    [InlineData("BakePizza()", CodeObservationMatchTarget.Invocation)]
    [InlineData("new Pizza()", CodeObservationMatchTarget.New)]
    [InlineData("result", CodeObservationMatchTarget.Identifier)]
    [InlineData("kitchen.Result", CodeObservationMatchTarget.MemberAccess)]
    [InlineData("42", CodeObservationMatchTarget.Literal)]
    [InlineData("-42", CodeObservationMatchTarget.Literal)]
    public void Classify_RecognizesConfiguredObservationTargets(string source, CodeObservationMatchTarget expected)
    {
        var node = source.StartsWith("throw", StringComparison.Ordinal)
            ? (SyntaxNode)SyntaxFactory.ParseStatement(source)
            : SyntaxFactory.ParseExpression(source);

        var result = CodeObservationMatchTargetClassifier.Classify(node);

        result.Should().Be(expected);
    }

    [Fact]
    public void GetDisplayName_UsesSyntaxKindForAnUnconfiguredExpressionTarget()
    {
        var expression = SyntaxFactory.ParseExpression("ready ? pizza : fallback");

        var result = CodeObservationMatchTargetClassifier.GetDisplayName(expression);

        result.Should().Be("ConditionalExpression");
    }
}