using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RonSijm.AnaalIJzer.Core.Matchers.Observations;

public static class CodeObservationMatchTargetClassifier
{
    public static CodeObservationMatchTarget? Classify(SyntaxNode node)
    {
        CodeObservationMatchTarget? result = node switch
        {
            ThrowStatementSyntax or ThrowExpressionSyntax => CodeObservationMatchTarget.Throw,
            InvocationExpressionSyntax => CodeObservationMatchTarget.Invocation,
            ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax => CodeObservationMatchTarget.New,
            IdentifierNameSyntax or GenericNameSyntax => CodeObservationMatchTarget.Identifier,
            MemberAccessExpressionSyntax => CodeObservationMatchTarget.MemberAccess,
            LiteralExpressionSyntax => CodeObservationMatchTarget.Literal,
            PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.UnaryMinusExpression) || prefix.IsKind(SyntaxKind.UnaryPlusExpression) => CodeObservationMatchTarget.Literal,
            _ => null
        };

        return result;
    }

    public static bool Matches(SyntaxNode node, CodeObservationMatchTarget target)
    {
        var result = Classify(node) == target;

        return result;
    }

    public static string GetDisplayName(SyntaxNode node)
    {
        var target = Classify(node);
        var result = target?.ToString() ?? node.Kind().ToString();

        return result;
    }
}