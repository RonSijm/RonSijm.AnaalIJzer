using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.IJzer.Core.Indicators;
using RonSijm.IJzer.Core.Observations;
using RonSijm.IJzer.Core.SemanticOperations.Analysis;
using RonSijm.IJzer.Core.SemanticOperations.Model;
using RonSijm.IJzer.Core.Violations;
using AnalyzerConfig = RonSijm.IJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.IJzer.Engine.Analysis.BoundaryRules.LayerDependencies;

public static partial class LayerDependencyAnalyzer
{
    internal static void AnalyzeLocalDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ObservedDependencyCollector? observedDependencies)
    {
        var localDecl = (LocalDeclarationStatementSyntax)context.Node;
        var caller = TryGetCallerContext(context, config, localDecl);
        if (caller is null)
        {
            return;
        }

        var typeSyntax = localDecl.Declaration.Type;
        var typeInfo = context.SemanticModel.GetTypeInfo(typeSyntax, context.CancellationToken);
        if (!typeSyntax.IsVar && typeInfo.Type is not null && typeInfo.Type.TypeKind != TypeKind.Error)
        {
            AnalyzeTypeReference(context, config, violations, observedDependencies, caller.Value, typeSyntax.GetLocation(), typeInfo.Type, DependencySites.Local);
            NamingRules.LayerDependencyAnalyzer.AnalyzeLocalInitializerNameRules(context, config, violations, localDecl);
            NamingRules.LayerDependencyAnalyzer.AnalyzeLocalDeclarationNameRules(context, config, violations, localDecl);
            return;
        }

        foreach (var variable in localDecl.Declaration.Variables)
        {
            if (context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken) is not ILocalSymbol localSymbol || localSymbol.Type.TypeKind == TypeKind.Error)
            {
                continue;
            }

            AnalyzeTypeReference(context, config, violations, observedDependencies, caller.Value, variable.Identifier.GetLocation(), localSymbol.Type, DependencySites.Local);
        }

        NamingRules.LayerDependencyAnalyzer.AnalyzeLocalInitializerNameRules(context, config, violations, localDecl);
        NamingRules.LayerDependencyAnalyzer.AnalyzeLocalDeclarationNameRules(context, config, violations, localDecl);
    }

    internal static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ObservedDependencyCollector? observedDependencies)
    {
        var node = (ExpressionSyntax)context.Node;
        var caller = TryGetCallerContext(context, config, node);
        if (caller is null)
        {
            return;
        }

        var typeInfo = context.SemanticModel.GetTypeInfo(node, context.CancellationToken);
        if (typeInfo.Type is null)
        {
            return;
        }

        var location = node is ObjectCreationExpressionSyntax objectCreation
            ? objectCreation.Type.GetLocation()
            : node.GetLocation();

        AnalyzeTypeReference(context, config, violations, observedDependencies, caller.Value, location, typeInfo.Type, DependencySites.New);
        NamingRules.LayerDependencyAnalyzer.AnalyzeObjectCreationNameRules(context, config, violations, node);
    }

    internal static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ObservedDependencyCollector? observedDependencies)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var caller = TryGetCallerContext(context, config, invocation);
        if (caller is null)
        {
            return;
        }

        var operation = context.SemanticModel.GetOperation(invocation, context.CancellationToken);
        if (SemanticOperationFactory.TryCreate(operation, context.SemanticModel, context.CancellationToken, out var semanticOperation)
            && semanticOperation.IsStaticAccess
            && semanticOperation.ContainingType is not null)
        {
            var staticLocation = invocation.Expression is MemberAccessExpressionSyntax memberAccess
                ? memberAccess.Expression.GetLocation()
                : invocation.Expression.GetLocation();
            AnalyzeTypeReference(context, config, violations, observedDependencies, caller.Value, staticLocation, semanticOperation.ContainingType, DependencySites.StaticMember);
        }

        NamingRules.LayerDependencyAnalyzer.AnalyzeInvocationNameRules(context, config, violations, invocation);

        var generic = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name as GenericNameSyntax,
            GenericNameSyntax genericName => genericName,
            _ => null
        };

        if (generic is null || generic.TypeArgumentList.Arguments.Count == 0)
        {
            return;
        }

        foreach (var typeArg in generic.TypeArgumentList.Arguments)
        {
            var typeInfo = context.SemanticModel.GetTypeInfo(typeArg, context.CancellationToken);
            if (typeInfo.Type is null)
            {
                continue;
            }

            AnalyzeTypeReference(context, config, violations, observedDependencies, caller.Value, typeArg.GetLocation(), typeInfo.Type, DependencySites.GenericInvocation);
        }
    }

    internal static void AnalyzeStaticMemberAccess(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ObservedDependencyCollector? observedDependencies)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;
        var operation = context.SemanticModel.GetOperation(memberAccess, context.CancellationToken);
        if (!SemanticOperationFactory.TryCreate(operation, context.SemanticModel, context.CancellationToken, out var semanticOperation)
            || !semanticOperation.IsStaticAccess
            || semanticOperation.ContainingType is null
            || semanticOperation.Kind is not (SemanticOperationKind.PropertyRead or SemanticOperationKind.PropertyWrite or SemanticOperationKind.FieldRead or SemanticOperationKind.FieldWrite or SemanticOperationKind.EventAccess))
        {
            return;
        }

        var caller = TryGetCallerContext(context, config, memberAccess);
        if (caller is null)
        {
            return;
        }

        AnalyzeTypeReference(context, config, violations, observedDependencies, caller.Value, memberAccess.Expression.GetLocation(), semanticOperation.ContainingType, DependencySites.StaticMember);
    }
}
