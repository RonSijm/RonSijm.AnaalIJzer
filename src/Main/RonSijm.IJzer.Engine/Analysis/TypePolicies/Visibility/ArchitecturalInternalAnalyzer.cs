using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using RonSijm.IJzer.Core.Findings;
using RonSijm.IJzer.Core.Indicators;
using RonSijm.IJzer.Core.Visibility;
using RonSijm.IJzer.Core.Visibility.Annotations;
using RonSijm.IJzer.Diagnostics;
using AnalyzerConfig = RonSijm.IJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.IJzer.Engine.Analysis.TypePolicies.Visibility;

internal static class ArchitecturalInternalAnalyzer
{
    internal static void AnalyzeSimpleName(SyntaxNodeAnalysisContext context, AnalyzerConfig config)
    {
        var name = (SimpleNameSyntax)context.Node;
        var alias = context.SemanticModel.GetAliasInfo(name, context.CancellationToken);
        var symbol = alias?.Target ?? context.SemanticModel.GetSymbolInfo(name, context.CancellationToken).Symbol;
        if (symbol is null)
        {
            return;
        }

        AnalyzeReference(context, config, symbol, symbol.GetArchitectureInternalRestrictions(), name.GetLocation());
    }

    internal static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context, AnalyzerConfig config)
    {
        var symbol = context.SemanticModel.GetSymbolInfo(context.Node, context.CancellationToken).Symbol;
        if (symbol is not IMethodSymbol constructor)
        {
            return;
        }

        var restrictions = context.Node is ImplicitObjectCreationExpressionSyntax
            ? constructor.GetArchitectureInternalRestrictions()
            : constructor.GetDeclaredArchitectureInternalRestrictions();
        AnalyzeReference(context, config, constructor, restrictions, context.Node.GetLocation());
    }

    internal static void AnalyzeElementAccess(SyntaxNodeAnalysisContext context, AnalyzerConfig config)
    {
        var symbol = context.SemanticModel.GetSymbolInfo(context.Node, context.CancellationToken).Symbol;
        if (symbol is null)
        {
            return;
        }

        AnalyzeReference(context, config, symbol, symbol.GetArchitectureInternalRestrictions(), context.Node.GetLocation());
    }

    internal static void AnalyzeOperator(OperationAnalysisContext context, AnalyzerConfig config)
    {
        var symbol = context.Operation switch
        {
            IBinaryOperation binary => binary.OperatorMethod,
            IUnaryOperation unary => unary.OperatorMethod,
            IIncrementOrDecrementOperation increment => increment.OperatorMethod,
            IConversionOperation conversion => conversion.OperatorMethod,
            ICompoundAssignmentOperation compoundAssignment => compoundAssignment.OperatorMethod,
            _ => null
        };
        if (symbol is null)
        {
            return;
        }

        AnalyzeReference(
            context.ReportDiagnostic,
            context.ContainingSymbol,
            symbol,
            symbol.GetDeclaredArchitectureInternalRestrictions(),
            config,
            context.Operation.Syntax.GetLocation(),
            DependencySites.Method);
    }

    internal static void AnalyzePublicExposure(SymbolAnalysisContext context, AnalyzerConfig config)
    {
        var symbol = context.Symbol;
        if (symbol.IsImplicitlyDeclared || !symbol.IsEffectivelyExternallyVisible())
        {
            return;
        }

        var reported = new HashSet<string>(StringComparer.Ordinal);
        foreach (var reference in ArchitecturalInternalSignatureTypeEnumerator.Enumerate(symbol))
        {
            foreach (var referencedType in ArchitecturalInternalSignatureTypeEnumerator.Expand(reference.Type))
            {
                foreach (var restriction in referencedType.GetArchitectureInternalRestrictions())
                {
                    if (!ArchitecturalInternalScopeEvaluator.IsReferenceAllowed(symbol, restriction, config, out _, out _, out _)
                        || ArchitecturalInternalScopeEvaluator.IsExposureProtected(symbol, restriction, config))
                    {
                        continue;
                    }

                    var key = referencedType.ToDisplayString() + "|" + restriction.AttributeTypeName + "|" + reference.Site;
                    if (!reported.Add(key))
                    {
                        continue;
                    }

                    var reason = $"the externally visible declaration is not protected by an equivalent {restriction.Scope.ToString().ToLowerInvariant()}-internal annotation";
                    Report(context.ReportDiagnostic, GetLocation(symbol, context.CancellationToken), symbol, referencedType, restriction, string.Empty, string.Empty, reference.Site, reason);
                }
            }
        }
    }

    private static void AnalyzeReference(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ISymbol referencedSymbol, ImmutableArray<ArchitectureInternalRestriction> restrictions, Location location)
    {
        if (restrictions.IsDefaultOrEmpty)
        {
            return;
        }

        var caller = context.SemanticModel.GetEnclosingSymbol(context.Node.SpanStart, context.CancellationToken);
        if (caller is null)
        {
            return;
        }

        var site = ArchitecturalInternalSiteClassifier.GetSite(context.Node, referencedSymbol);
        AnalyzeReference(context.ReportDiagnostic, caller, referencedSymbol, restrictions, config, location, site);
    }

    private static void AnalyzeReference(Action<Diagnostic> reportDiagnostic, ISymbol caller, ISymbol referencedSymbol, ImmutableArray<ArchitectureInternalRestriction> restrictions, AnalyzerConfig config, Location location, string site)
    {
        foreach (var restriction in restrictions)
        {
            if (ArchitecturalInternalScopeEvaluator.IsReferenceAllowed(caller, restriction, config, out var callerLayer, out var targetLayer, out var reason))
            {
                continue;
            }

            Report(reportDiagnostic, location, caller, referencedSymbol, restriction, callerLayer, targetLayer, site, reason);
            break;
        }
    }

    private static void Report(Action<Diagnostic> reportDiagnostic, Location? location, ISymbol caller, ISymbol target, ArchitectureInternalRestriction restriction, string callerLayer, string targetLayer, string site, string reason)
    {
        if (location is null)
        {
            return;
        }

        var callerName = GetDisplayName(caller);
        var targetName = GetDisplayName(target);
        var properties = ImmutableDictionary<string, string?>.Empty
            .Add(ArchitecturalDiagnostics.PropertyCallerTypeName, caller.GetArchitectureOwnerType()?.Name ?? caller.Name)
            .Add(ArchitecturalDiagnostics.PropertyCallerLayerName, callerLayer)
            .Add(ArchitecturalDiagnostics.PropertyDepTypeName, targetName)
            .Add(ArchitecturalDiagnostics.PropertyDepLayerName, targetLayer)
            .Add(ArchitecturalDiagnostics.PropertySite, site)
            .Add(ArchitecturalDiagnostics.PropertyViolationReason, reason)
            .Add(ArchitecturalDiagnostics.PropertyArchitecturalInternalScope, restriction.Scope.ToString())
            .Add(ArchitecturalDiagnostics.PropertyArchitecturalInternalAttribute, restriction.AttributeTypeName);

        reportDiagnostic(ArchitecturalDiagnostics.CreateDiagnostic(
            ArchitecturalDiagnostics.ArchitecturalInternalBoundary,
            location,
            properties,
            callerName,
            targetName,
            reason));
    }

    private static string GetDisplayName(ISymbol symbol)
    {
        var result = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

        return result;
    }

    private static Location? GetLocation(ISymbol symbol, CancellationToken cancellationToken)
    {
        var result = symbol.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax(cancellationToken).GetLocation())
            .OrderBy(location => location.SourceTree?.FilePath, StringComparer.Ordinal)
            .ThenBy(location => location.SourceSpan.Start)
            .FirstOrDefault();

        return result;
    }
}
